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
		public const string Version = "0.1.0";

		static async Task Main()
		{
			Console.WriteLine($"WowOnlineKeeper v{Version}");
			await new Program().Run();
		}

		readonly InputSystem m_Input = new InputSystem();
		readonly Random m_Random = new Random();
		Dictionary<int, Item> m_Items = new Dictionary<int, Item>();
		Dictionary<int, Item> m_ItemsBuf = new Dictionary<int, Item>();
		DateTime m_Now;

		async Task Run()
		{
			m_Input.KeyDown += _ => OnUserInput();
			m_Input.Mouse += (type, point) =>
			{
				if (type != WindowMessage.WM_MOUSEMOVE) OnUserInput();
			};
			while (true)
			{
				m_Now = DateTime.UtcNow;
				UpdateItems();
				foreach (var item in m_Items.Values)
				{
					if (m_Now - item.LastInputTime >= TimeSpan.FromSeconds(5))
					{
						Console.WriteLine(DateTime.Now);
						item.LastInputTime = m_Now;
						GetWindowRect(item.Process.MainWindowHandle, out var rect);
						await item.Click(((rect.right - rect.left) / 2, (int) ((rect.bottom - rect.top) * 0.917)));
						await item.Key(RandomKey());
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		void OnUserInput()
		{
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

		public async Task Key(VirtualKey key)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
			await Task.Delay(50);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}

		public async Task Click(Point point)
		{
			GetCursorPos(out var pos);
			await Task.Delay(25);
			POINT pt = point;
			ClientToScreen(Process.MainWindowHandle, ref pt);
			SetCursorPos(pt.x, pt.y);
			await Task.Delay(25);
			var lParam = point.ToLParam();
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, lParam);
			await Task.Delay(25);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_LBUTTONDOWN, (IntPtr) 1, lParam);
			PostMessage(Process.MainWindowHandle, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, lParam);
			await Task.Delay(25);
			SetCursorPos(pos.x, pos.y);
		}
	}
}