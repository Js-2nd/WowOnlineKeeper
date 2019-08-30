namespace WowOnlineKeeper
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	class Program
	{
		static async Task Main() => await new Program().Run();

		readonly InputSystem m_Input = new InputSystem();
		readonly Random m_Random = new Random();
		Dictionary<int, Item> m_Items = new Dictionary<int, Item>();
		Dictionary<int, Item> m_ItemsBuf = new Dictionary<int, Item>();
		DateTime m_Now;

		async Task Run()
		{
			m_Input.KeyDown += OnKeyDown;
			while (true)
			{
				m_Now = DateTime.UtcNow;
				UpdateItems();
				foreach (var item in m_Items.Values)
				{
					if (m_Now - item.LastInputTime >= TimeSpan.FromSeconds(5))
					{
						item.LastInputTime = m_Now;
						GetWindowRect(item.Process.MainWindowHandle, out var rect);
						await item.Click(((rect.right - rect.left) / 2, rect.bottom - rect.top - 90));
						await item.Key(RandomKey());
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		void OnKeyDown(VirtualKey key)
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
			switch (m_Random.Next(3))
			{
				case 0: return VirtualKey.VK_SPACE;
				case 1: return VirtualKey.VK_S;
				case 2: return VirtualKey.VK_W;
				default: return VirtualKey.VK_NO_KEY;
			}
		}
	}

	class Item
	{
		public Process Process;
		public DateTime LastInputTime;

		public async Task Key(VirtualKey key, int duration = 100)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			PostMessage(Process.Handle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
			await Task.Delay(duration);
			PostMessage(Process.Handle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}

		public async Task Click(Point point, int duration = 100)
		{
			var lParam = point.ToLParam();
			PostMessage(Process.Handle, WindowMessage.WM_LBUTTONDOWN, (IntPtr) 1, lParam);
			await Task.Delay(duration);
			PostMessage(Process.Handle, WindowMessage.WM_IME_KEYUP, IntPtr.Zero, lParam);
		}
	}
}