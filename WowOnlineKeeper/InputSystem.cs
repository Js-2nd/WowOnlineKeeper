namespace WowOnlineKeeper
{
	using PInvoke;
	using System;
	using System.Runtime.InteropServices;
	using System.Threading.Channels;
	using System.Threading.Tasks;
	using static PInvoke.User32;

	public sealed class InputSystem : IDisposable
	{
		public event Action<VirtualKey> KeyDown;
		public event Action<VirtualKey> KeyHold;
		public event Action<VirtualKey> KeyUp;
		public bool IsDisposed { get; private set; }

		readonly WindowMessage m_StopMessage;
		readonly int[] m_KeyCounter;
		readonly Channel<int> m_Channel;
		int m_HookThreadId;
		WindowsHookDelegate m_HookProc;
		SafeHookHandle m_Hook;

		public InputSystem(WindowMessage stopMessage = WindowMessage.WM_USER)
		{
			m_StopMessage = stopMessage;
			m_KeyCounter = new int[256];
			m_Channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions {SingleReader = true});
			Task.Run(ReaderLoop);
			Task.Factory.StartNew(HookKeyboard, TaskCreationOptions.LongRunning);
		}

		async Task ReaderLoop()
		{
			try
			{
				while (await m_Channel.Reader.WaitToReadAsync())
				while (m_Channel.Reader.TryRead(out int key))
				{
					if (key < 0)
					{
						key = ~key;
						m_KeyCounter[key] = 0;
						Trigger(KeyUp, key);
					}
					else
					{
						if (++m_KeyCounter[key] == 1) Trigger(KeyDown, key);
						Trigger(KeyHold, key);
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
			}
			finally
			{
				Dispose();
			}
		}

		void Trigger(Action<VirtualKey> action, int key)
		{
			try
			{
				action?.Invoke((VirtualKey) key);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
			}
		}

		void HookKeyboard()
		{
			try
			{
				m_HookThreadId = Kernel32.GetCurrentThreadId();
				m_HookProc = LowLevelKeyboardProc;
				m_Hook = SetWindowsHookEx(WindowsHookType.WH_KEYBOARD_LL, m_HookProc, IntPtr.Zero, 0);
				MessageLoop();
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex);
			}
			finally
			{
				Dispose();
			}
		}

		int LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				var type = (WindowMessage) wParam;
				int key = Marshal.ReadInt32(lParam);
				if (type == WindowMessage.WM_KEYUP || type == WindowMessage.WM_SYSKEYUP) key = ~key;
				m_Channel.Writer.TryWrite(key);
			}

			return CallNextHookEx(m_Hook.DangerousGetHandle(), nCode, wParam, lParam);
		}

		unsafe void MessageLoop()
		{
			MSG msg;
			while (GetMessage(&msg, IntPtr.Zero, 0, 0) > 0)
			{
				if (msg.message == m_StopMessage) break;
				TranslateMessage(&msg);
				DispatchMessage(&msg);
			}
		}

		public void Dispose()
		{
			if (IsDisposed) return;
			IsDisposed = true;
			m_Hook.Dispose();
			PostThreadMessage(m_HookThreadId, m_StopMessage, IntPtr.Zero, IntPtr.Zero);
			m_Channel.Writer.TryComplete();
		}

		public bool IsKeyDown(VirtualKey key) => m_KeyCounter[(int) key] > 0;

		public bool IsCtrlDown() =>
			IsKeyDown(VirtualKey.VK_CONTROL) || IsKeyDown(VirtualKey.VK_LCONTROL) || IsKeyDown(VirtualKey.VK_RCONTROL);

		public bool IsAltDown() =>
			IsKeyDown(VirtualKey.VK_MENU) || IsKeyDown(VirtualKey.VK_LMENU) || IsKeyDown(VirtualKey.VK_RMENU);

		public bool IsShiftDown() =>
			IsKeyDown(VirtualKey.VK_SHIFT) || IsKeyDown(VirtualKey.VK_LSHIFT) || IsKeyDown(VirtualKey.VK_RSHIFT);
	}
}