namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	class Program
	{
		public const string Version = "0.1.1";

		static async Task Main()
		{
			Console.WriteLine($"WowOnlineKeeper v{Version}");
			await new Program().Run();
		}

		readonly InputSystem m_Input = new InputSystem();
		readonly Random m_Random = new Random();
		Dictionary<int, Item> m_Items = new Dictionary<int, Item>();
		Dictionary<int, Item> m_ItemsBuf = new Dictionary<int, Item>();
		DateTime m_LastInputTime = DateTime.Now;
		DateTime m_Now;

		bool AwayFromKeyboard => m_Now - m_LastInputTime >= TimeSpan.FromMinutes(5);

		async Task Run()
		{
			m_Input.KeyDown += _ => OnUserInput();
			m_Input.Mouse += (type, point) =>
			{
				if (type != WindowMessage.WM_MOUSEMOVE) OnUserInput();
			};
			while (true)
			{
				m_Now = DateTime.Now;
				UpdateItems();
				foreach (var item in m_Items.Values)
				{
					if (m_Now - item.LastInputTime >= TimeSpan.FromSeconds(30))
					{
						Console.WriteLine(m_Now);
						item.LastInputTime = m_Now;
						GetWindowRect(item.Process.MainWindowHandle, out var rect);
						Point point = ((rect.right - rect.left) / 2, (int) ((rect.bottom - rect.top) * 0.917));
						bool awayFromKeyboard = AwayFromKeyboard;
						await item.Click(point, awayFromKeyboard ? 100 : 10);
						await item.Key(awayFromKeyboard ? RandomKey() : VirtualKey.VK_SPACE);
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		void OnUserInput()
		{
			m_LastInputTime = m_Now;
			GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
			if (m_Items.TryGetValue(processId, out var item)) item.LastInputTime = m_Now;
		}

		void UpdateItems()
		{
			m_ItemsBuf.Clear();
			foreach (var process in Process.GetProcessesByName("Wow"))
			{
				int processId = process.Id;
				if (!m_Items.TryGetValue(processId, out var item))
					item = new Item {Process = process, LastInputTime = m_Now};
				m_ItemsBuf[processId] = item;
			}

			(m_Items, m_ItemsBuf) = (m_ItemsBuf, m_Items);
		}

		VirtualKey RandomKey()
		{
			switch (m_Random.Next(5))
			{
				case 0:
				case 1: return VirtualKey.VK_SPACE;
				case 2:
				case 3: return VirtualKey.VK_S;
				case 4: return VirtualKey.VK_W;
				default: return VirtualKey.VK_NO_KEY;
			}
		}
	}

	class Item
	{
		public Process Process;
		public DateTime LastInputTime;

		public async Task Key(VirtualKey key, int delay = 50)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
			await Task.Delay(delay);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}

		public async Task Click(Point point, int delay = 50)
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