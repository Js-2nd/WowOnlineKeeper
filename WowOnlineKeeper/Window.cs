namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	public readonly struct Window
	{
		public readonly IntPtr Handle;
		public Window(IntPtr handle) => Handle = handle;

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

		public async Task Key(VirtualKey key, TimeSpan duration)
		{
			if (key == VirtualKey.VK_NO_KEY) return;
			KeyDown(key);
			await Task.Delay(duration);
			KeyUp(key);
		}

		public void KeyDown(VirtualKey key)
		{
			PostMessage(Handle, WindowMessage.WM_IME_KEYDOWN, (IntPtr) key, IntPtr.Zero);
		}

		public void KeyUp(VirtualKey key)
		{
			PostMessage(Handle, WindowMessage.WM_IME_KEYUP, (IntPtr) key, IntPtr.Zero);
		}

		public async Task Click(Point point, TimeSpan delay)
		{
			GetCursorPos(out var pos);
			POINT pt = point;
			ClientToScreen(Handle, ref pt);
			SetCursorPos(pt.x, pt.y);
			var lParam = point.ToLParam();
			PostMessage(Handle, WindowMessage.WM_MOUSEMOVE, IntPtr.Zero, lParam);
			PostMessage(Handle, WindowMessage.WM_LBUTTONDOWN, (IntPtr) 1, lParam);
			PostMessage(Handle, WindowMessage.WM_LBUTTONUP, IntPtr.Zero, lParam);
			await Task.Delay(delay);
			SetCursorPos(pos.x, pos.y);
		}
	}
}