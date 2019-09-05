namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	public sealed class Program
	{
		public const string Version = "0.2.0";
		const string ConfigPath = nameof(WowOnlineKeeper) + ".ini";

		static async Task Main()
		{
			Console.WriteLine($"WowOnlineKeeper v{Version}");
			await new Program().Run();
		}

		readonly Random m_Random = new Random();
		Dictionary<int, Game> m_Games = new Dictionary<int, Game>();
		Dictionary<int, Game> m_GamesSwap = new Dictionary<int, Game>();
		Config m_Config;
		InputSystem m_Input;
		DateTime m_LastMouseMoveTime = DateTime.Now;
		DateTime m_Now;

		async Task Run()
		{
			LoadConfig();
			InitInput();
			while (true)
			{
				m_Now = DateTime.Now;
				UpdateGames();
				bool act = false;
				foreach (var game in m_Games.Values)
				{
					if (m_Now - game.LastInputTime >= TimeSpan.FromSeconds(30))
					{
						act = true;
						var keySet = m_Config.KeySets[m_Random.Next(m_Config.KeySets.Count)];
						var keyConfig = keySet[m_Random.Next(keySet.Count)];
						Console.WriteLine($"{m_Now.ToString(DateTimeFormatInfo.CurrentInfo)}\t{keyConfig}");
						await game.Key(keyConfig);
						if (m_Now - m_LastMouseMoveTime >= TimeSpan.FromSeconds(30))
						{
							GetWindowRect(game.Process.MainWindowHandle, out var rect);
							Point point = ((rect.right - rect.left) / 2, (int) ((rect.bottom - rect.top) * 0.917));
							await game.Click(point, 200);
						}
					}
				}

				if (!act) await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		void LoadConfig()
		{
			if (!File.Exists(ConfigPath)) Config.Parser.WriteFile(ConfigPath, Config.Default);
			m_Config = new Config(ConfigPath);
		}

		void InitInput()
		{
			try
			{
				m_Input = new InputSystem();
				m_Input.KeyDown += _ => OnUserInput(false);
				m_Input.Mouse += (type, point) => OnUserInput(type == WindowMessage.WM_MOUSEMOVE);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
			}
		}

		void OnUserInput(bool isMouseMove)
		{
			if (isMouseMove) m_LastMouseMoveTime = m_Now;
			else
			{
				GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
				if (m_Games.TryGetValue(processId, out var item)) item.LastInputTime = m_Now;
			}
		}

		void UpdateGames()
		{
			m_GamesSwap.Clear();
			foreach (var process in Process.GetProcessesByName("Wow"))
			{
				int processId = process.Id;
				if (!m_Games.TryGetValue(processId, out var item))
					item = new Game {Process = process, LastInputTime = m_Now};
				m_GamesSwap[processId] = item;
			}

			(m_Games, m_GamesSwap) = (m_GamesSwap, m_Games);
		}
	}

	class Game
	{
		public Process Process;
		public DateTime LastInputTime;

		public async Task Key(KeyConfig config)
		{
			if (config.Ctrl) KeyDown(VirtualKey.VK_LCONTROL);
			if (config.Shift) KeyDown(VirtualKey.VK_LSHIFT);
			if (config.Alt) KeyDown(VirtualKey.VK_LMENU);
			await Key(config.Key, config.Duration);
			if (config.Ctrl) KeyUp(VirtualKey.VK_LCONTROL);
			if (config.Shift) KeyUp(VirtualKey.VK_LSHIFT);
			if (config.Alt) KeyUp(VirtualKey.VK_LMENU);
		}

		public async Task Key(VirtualKey key, int duration)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			KeyDown(key);
			await Task.Delay(duration);
			KeyUp(key);
		}

		public void KeyDown(VirtualKey key)
		{
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
		}

		public void KeyUp(VirtualKey key)
		{
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}

		public async Task Click(Point point, int delay)
		{
			GetCursorPos(out var pos);
			POINT pt = point;
			ClientToScreen(Process.MainWindowHandle, ref pt);
			SetCursorPos(pt.x, pt.y);
			var lParam = point.ToLParam();
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, lParam);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_LBUTTONDOWN, (IntPtr) 1, lParam);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, lParam);
			await Task.Delay(delay);
			SetCursorPos(pos.x, pos.y);
		}
	}
}