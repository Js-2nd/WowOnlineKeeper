namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	public sealed class Program
	{
		public const string Version = "0.1.2";

		static async Task Main()
		{
			Console.WriteLine($"WowOnlineKeeper v{Version}");
			await new Program().Run();
		}

		readonly InputSystem m_Input = new InputSystem();
		readonly Random m_Random = new Random();
		Dictionary<int, Game> m_Games = new Dictionary<int, Game>();
		Dictionary<int, Game> m_GamesSwap = new Dictionary<int, Game>();
		DateTime m_SystemLastInputTime = DateTime.Now;
		DateTime m_Now;

		async Task Run()
		{
			m_Input.KeyDown += _ => OnUserInput(true);
			m_Input.Mouse += (type, point) => OnUserInput(type != WindowMessage.WM_MOUSEMOVE);
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
						var keySet = s_KeySets[m_Random.Next(s_KeySets.Length)];
						var key = keySet[m_Random.Next(keySet.Length)];
						Console.WriteLine($"{m_Now}\t{key}");
						await game.Key(key, 500);
						if (m_Now - m_SystemLastInputTime >= TimeSpan.FromSeconds(30))
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

		void OnUserInput(bool updateGame)
		{
			m_SystemLastInputTime = m_Now;
			if (!updateGame) return;
			GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
			if (!m_Games.TryGetValue(processId, out var item)) return;
			item.LastInputTime = m_Now;
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

		static readonly VirtualKey[][] s_KeySets =
		{
			new[]
			{
				VirtualKey.VK_W,
				VirtualKey.VK_A,
				VirtualKey.VK_D,
			},
			new[]
			{
				VirtualKey.VK_OEM_3,
				VirtualKey.VK_Q,
				VirtualKey.VK_E,
				VirtualKey.VK_KEY_1,
				VirtualKey.VK_KEY_2,
				VirtualKey.VK_KEY_3,
				VirtualKey.VK_KEY_4,
			},
		};
	}

	class Game
	{
		public Process Process;
		public DateTime LastInputTime;

		public async Task Key(VirtualKey key, int delay)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
			await Task.Delay(delay);
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