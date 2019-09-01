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
		Dictionary<int, Item> m_Items = new Dictionary<int, Item>();
		Dictionary<int, Item> m_ItemsAlt = new Dictionary<int, Item>();
		DateTime m_LastInputTime = DateTime.Now;
		DateTime m_Now;

		async Task Run()
		{
			m_Input.KeyDown += _ => OnUserInput(true);
			m_Input.Mouse += (type, point) => OnUserInput(type != WindowMessage.WM_MOUSEMOVE);
			while (true)
			{
				m_Now = DateTime.Now;
				UpdateItems();
				foreach (var item in m_Items.Values)
				{
					bool afk = m_Now - item.LastInputTime >= TimeSpan.FromMinutes(2);
					if (m_Now - item.LastActionTime >= TimeSpan.FromSeconds(afk ? 2 : 30))
					{
						item.LastActionTime = m_Now;
						var key = afk ? s_Keys[m_Random.Next(s_Keys.Length)] : VirtualKey.VK_SPACE;
						Console.WriteLine($"{m_Now}\t{key}");
						await item.Key(key, afk ? 500 : 50);
						if (m_Now - m_LastInputTime >= TimeSpan.FromMinutes(2))
						{
							GetWindowRect(item.Process.MainWindowHandle, out var rect);
							Point point = ((rect.right - rect.left) / 2, (int) ((rect.bottom - rect.top) * 0.917));
							await item.Click(point, 200);
						}
					}
				}

				await Task.Delay(TimeSpan.FromSeconds(1));
			}
		}

		void OnUserInput(bool updateItem)
		{
			m_LastInputTime = m_Now;
			if (!updateItem) return;
			GetWindowThreadProcessId(GetForegroundWindow(), out int processId);
			if (!m_Items.TryGetValue(processId, out var item)) return;
			item.LastInputTime = m_Now;
			item.LastActionTime = m_Now;
		}

		void UpdateItems()
		{
			m_ItemsAlt.Clear();
			foreach (var process in Process.GetProcessesByName("Wow"))
			{
				int processId = process.Id;
				if (!m_Items.TryGetValue(processId, out var item))
					item = new Item {Process = process, LastInputTime = m_Now, LastActionTime = m_Now};
				m_ItemsAlt[processId] = item;
			}

			(m_Items, m_ItemsAlt) = (m_ItemsAlt, m_Items);
		}

		static readonly VirtualKey[] s_Keys =
		{
			VirtualKey.VK_SPACE,
			VirtualKey.VK_W,
			VirtualKey.VK_S,
			VirtualKey.VK_A,
			VirtualKey.VK_D,
			VirtualKey.VK_Q,
			VirtualKey.VK_E,
			VirtualKey.VK_OEM_3,
			VirtualKey.VK_KEY_1,
			VirtualKey.VK_KEY_2,
			VirtualKey.VK_KEY_3,
			VirtualKey.VK_KEY_4,
		};
	}

	class Item
	{
		public Process Process;
		public DateTime LastInputTime;
		public DateTime LastActionTime;

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