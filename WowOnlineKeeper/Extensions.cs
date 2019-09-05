namespace WowOnlineKeeper
{
	using System;
	using static PInvoke.User32;

	public static class Extensions
	{
		public static string KeyToStr(this VirtualKey key)
		{
			switch (key)
			{
				case VirtualKey.VK_OEM_3: return "`";
				case VirtualKey.VK_KEY_0: return "0";
				case VirtualKey.VK_KEY_1: return "1";
				case VirtualKey.VK_KEY_2: return "2";
				case VirtualKey.VK_KEY_3: return "3";
				case VirtualKey.VK_KEY_4: return "4";
				case VirtualKey.VK_KEY_5: return "5";
				case VirtualKey.VK_KEY_6: return "6";
				case VirtualKey.VK_KEY_7: return "7";
				case VirtualKey.VK_KEY_8: return "8";
				case VirtualKey.VK_KEY_9: return "9";
				case VirtualKey.VK_OEM_MINUS: return "-";
				case VirtualKey.VK_OEM_PLUS: return "+";
				case VirtualKey.VK_OEM_4: return "[";
				case VirtualKey.VK_OEM_6: return "]";
				case VirtualKey.VK_OEM_5: return "\\";
				case VirtualKey.VK_OEM_1: return ";";
				case VirtualKey.VK_OEM_7: return "'";
				case VirtualKey.VK_OEM_COMMA: return ",";
				case VirtualKey.VK_OEM_PERIOD: return ".";
				case VirtualKey.VK_OEM_2: return "/";
			}

			return key.ToString().Substring(3);
		}

		public static VirtualKey StrToKey(this string str)
		{
			if (str.Length == 1)
			{
				switch (str[0])
				{
					case '`': return VirtualKey.VK_OEM_3;
					case '0': return VirtualKey.VK_KEY_0;
					case '1': return VirtualKey.VK_KEY_1;
					case '2': return VirtualKey.VK_KEY_2;
					case '3': return VirtualKey.VK_KEY_3;
					case '4': return VirtualKey.VK_KEY_4;
					case '5': return VirtualKey.VK_KEY_5;
					case '6': return VirtualKey.VK_KEY_6;
					case '7': return VirtualKey.VK_KEY_7;
					case '8': return VirtualKey.VK_KEY_8;
					case '9': return VirtualKey.VK_KEY_9;
					case '-': return VirtualKey.VK_OEM_MINUS;
					case '+': return VirtualKey.VK_OEM_PLUS;
					case '[': return VirtualKey.VK_OEM_4;
					case ']': return VirtualKey.VK_OEM_6;
					case '\\': return VirtualKey.VK_OEM_5;
					case ';': return VirtualKey.VK_OEM_1;
					case '\'': return VirtualKey.VK_OEM_7;
					case ',': return VirtualKey.VK_OEM_COMMA;
					case '.': return VirtualKey.VK_OEM_PERIOD;
					case '/': return VirtualKey.VK_OEM_2;
				}
			}

			return Enum.TryParse<VirtualKey>("VK_" + str, out var key) ? key : VirtualKey.VK_NO_KEY;
		}
	}
}