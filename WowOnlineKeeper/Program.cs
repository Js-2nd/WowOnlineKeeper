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
			m_Config = new Config(ConfigPath);
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
						Console.WriteLine($"{m_Now.ToString()}\t{keyConfig}");
						await game.Window.Key(keyConfig);
						if (m_Now - m_LastMouseMoveTime >= TimeSpan.FromSeconds(30))
						{
							GetWindowRect(game.Window.Handle, out var rect);
							Point point = ((rect.right - rect.left) / 2, (int) ((rect.bottom - rect.top) * 0.917));
							await game.Window.Click(point, TimeSpan.FromSeconds(0.2));
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
				if (!m_Games.TryGetValue(processId, out var item))
					item = new Game {Window = new Window(process.MainWindowHandle), LastInputTime = m_Now};
				m_GamesSwap[processId] = item;
			}

			(m_Games, m_GamesSwap) = (m_GamesSwap, m_Games);
		}

		async Task Launch()
		{
			var battleNet = new Window(Process.GetProcessesByName("Battle.net")
				.First(process => process.MainWindowHandle != IntPtr.Zero).MainWindowHandle);
			GetWindowRect(battleNet.Handle, out var rect);
			await battleNet.Click((320, rect.bottom - rect.top - 64), TimeSpan.FromSeconds(200));
		}
	}

	class Game
	{
		public Window Window;
		public DateTime LastInputTime;
	}
}