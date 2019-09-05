namespace WowOnlineKeeper
{
	using IniParser;
	using IniParser.Model;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using static PInvoke.User32;

	public class Config
	{
		public static readonly FileIniDataParser Parser = new FileIniDataParser();

		public static IniData Default
		{
			get
			{
				var data = new IniData();
				var set = data["KeySet0"];
				set["W"] = "700";
				set["A"] = "300";
				set["D"] = "300";
				set = data["KeySet1"];
				set["1"] = "100";
				set["2"] = "100";
				set["F1"] = "100";
				set["Ctrl | Tab"] = "100";
				set["Shift | Q"] = "2000";
				set["Alt | E"] = "100";
				set["Ctrl | Shift | Alt | `"] = "100";
				return data;
			}
		}

		public readonly List<List<KeyConfig>> KeySets;

		public Config(string path) : this(Parser.ReadFile(path))
		{
		}

		public Config(IniData data)
		{
			KeySets = data.Sections
				.Where(section => section.Keys.Count > 0 && section.SectionName.StartsWith("KeySet"))
				.Select(section => section.Keys.Select(KeyConfig.From).ToList()).ToList();
		}
	}

	public class KeyConfig
	{
		public VirtualKey Key;
		public int Duration;
		public bool Ctrl;
		public bool Shift;
		public bool Alt;

		public override string ToString()
		{
			s_Builder.Clear();
			if (Ctrl) s_Builder.Append("CTRL").Append(" | ");
			if (Shift) s_Builder.Append("SHIFT").Append(" | ");
			if (Alt) s_Builder.Append("ALT").Append(" | ");
			string str = s_Builder.Append(Key.KeyToStr()).Append(" = ").Append(Duration).ToString();
			s_Builder.Clear();
			return str;
		}

		static readonly StringBuilder s_Builder = new StringBuilder();

		public static KeyConfig From(KeyData data)
		{
			var config = new KeyConfig();
			var array = data.KeyName.ToUpperInvariant().Split('|');
			for (int i = 0, keyIndex = array.Length - 1; i <= keyIndex; i++)
			{
				string str = array[i].Trim();
				if (i == keyIndex)
				{
					config.Key = str.StrToKey();
					if (config.Key == VirtualKey.VK_NO_KEY)
						Console.Error.WriteLine($"Invalid key: {str} at {data.KeyName}={data.Value}");
				}
				else
				{
					switch (str)
					{
						case "CTRL":
							config.Ctrl = true;
							break;
						case "SHIFT":
							config.Shift = true;
							break;
						case "ALT":
							config.Alt = true;
							break;
						default:
							Console.Error.WriteLine($"Invalid modifier: {str} at {data.KeyName}={data.Value}");
							break;
					}
				}
			}

			if (!int.TryParse(data.Value, out config.Duration))
				Console.Error.WriteLine($"Invalid duration: {data.Value} at {data.KeyName}={data.Value}");
			return config;
		}
	}
}