namespace WowOnlineKeeper
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	static class Program
	{
		static InputSystem m_Input;
		static Dictionary<int, Item> m_Items = new Dictionary<int, Item>();
		static Dictionary<int, Item> m_ItemsBuf = new Dictionary<int, Item>();
		static DateTime m_Now;

		public static async Task Main()
		{
			m_Input = new InputSystem();
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
						await item.Key(VirtualKey.VK_SPACE);
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		static void OnKeyDown(VirtualKey key)
		{
			GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
			if (m_Items.TryGetValue(processId, out var item)) item.LastInputTime = m_Now;
		}

		static void UpdateItems()
		{
			m_ItemsBuf.Clear();
			foreach (var process in Process.GetProcessesByName("Wow"))
			{
				int processId = process.Id;
				if (!m_Items.TryGetValue(processId, out var item)) item = new Item {LastInputTime = m_Now};
				m_ItemsBuf[processId] = item;
			}

			(m_Items, m_ItemsBuf) = (m_ItemsBuf, m_Items);
		}
	}

	class Item
	{
		public Process Process;
		public DateTime LastInputTime;

		public async Task Key(VirtualKey key, int duration = 100)
		{
			PostMessage(Process.Handle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
			await Task.Delay(duration);
			PostMessage(Process.Handle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}
	}
}