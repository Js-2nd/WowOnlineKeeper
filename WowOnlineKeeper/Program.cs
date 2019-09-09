namespace WowOnlineKeeper
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	public sealed class Program
	{
		public const string Version = "0.3.0";
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
			m_Config = new Config(ConfigPath);
			InitInput();
			while (true)
			{
				m_Now = DateTime.Now;
				UpdateGames();
				bool act = false;
				foreach (var game in m_Games.Values)
				{
					if (m_Now - game.LastInputTime >= m_Config.GameIdleTime)
					{
						if (m_Config.KeySets.Count > 0)
						{
							var keySet = m_Config.KeySets[m_Random.Next(m_Config.KeySets.Count)];
							if (keySet.Count > 0)
							{
								act = true;
								var keyConfig = keySet[m_Random.Next(keySet.Count)];
								Console.WriteLine($"{m_Now}\t{keyConfig}");
								await game.Window.Key(keyConfig);
							}
						}

						if (m_Now - m_LastMouseMoveTime >= m_Config.MouseIdleTime)
						{
							var size = game.Window.Size;
							// click enter game
							await game.Window.Click(size.X * 0.5, size.Y * 0.917);
							// click center popup
							await game.Window.Click(size.X * 0.5, size.Y * 0.513);
							// click quit game
							await game.Window.Click(size.X * 0.5 + size.Y * 0.851, size.Y * 0.935);
						}
					}
				}

				if (!act) await Task.Delay(TimeSpan.FromSeconds(1));
			}
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
				if (!m_Games.TryGetValue(processId, out var item)) item = new Game(process);
				m_GamesSwap[processId] = item;
			}

			(m_Games, m_GamesSwap) = (m_GamesSwap, m_Games);
		}

		async Task<bool> Launch()
		{
			var process = Process.GetProcessesByName("Battle.net")
				.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
			if (process == null) return false;
			var battleNet = new Window(process.MainWindowHandle);
			var size = battleNet.Size;
			await battleNet.Click((320, size.Y - 64), TimeSpan.FromSeconds(0.2));
			await Task.Delay(TimeSpan.FromSeconds(3));
			process = Process.GetProcessesByName("Wow").FirstOrDefault();
			if (process == null) return false;
			var wow = new Window(process.MainWindowHandle);
			await Task.Delay(TimeSpan.FromSeconds(7));
			size = wow.Size;
			// click second region
			await wow.Click(size.X * 0.5 - size.Y * 0.25, size.Y * 0.833);
			await Task.Delay(TimeSpan.FromSeconds(1));
			// click first server
			await wow.Click(size.X * 0.5, size.Y * 0.25);
			await Task.Delay(TimeSpan.FromSeconds(1));
			// click confirm
			await wow.Click(size.X * 0.5 + size.Y * 0.153, size.Y * 0.793);
			await Task.Delay(TimeSpan.FromSeconds(10));
			return true;
		}
	}

	class Game
	{
		public Process Process;
		public Window Window;
		public DateTime LastInputTime;

		public Game(Process process)
		{
			Process = process;
			Window = new Window(process.MainWindowHandle);
			LastInputTime = DateTime.Now;
		}
	}
}