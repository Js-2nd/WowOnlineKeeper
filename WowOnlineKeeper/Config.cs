namespace WowOnlineKeeper
{
	using IniParser;
	using IniParser.Model;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using static PInvoke.User32;

	public class Config
	{
		public const string ConfigSectionName = nameof(Config);

		public static readonly FileIniDataParser Parser = new FileIniDataParser();

		public static readonly Config Default = new Config
		{
			GameIdleTime = TimeSpan.FromSeconds(30),
			MouseIdleTime = TimeSpan.FromSeconds(60),
		};

		public static void DefaultConfig(IniData data)
		{
			var config = data[ConfigSectionName];
			config[nameof(GameIdleTime)] = Default.GameIdleTime.TotalSeconds.ToString();
			config[nameof(MouseIdleTime)] = Default.MouseIdleTime.TotalSeconds.ToString();
		}

		public static void DefaultKeySets(IniData data)
		{
			var keySet = data["KeySet0"];
			keySet["W"] = "0.7";
			keySet["A"] = "0.3";
			keySet["D"] = "0.3";
			keySet = data["KeySet1"];
			keySet["1"] = "0.2";
			keySet["2"] = "0.2";
			keySet["F1"] = "0.2";
			keySet["Ctrl | Tab"] = "0.2";
			keySet["Shift | Q"] = "0.2";
			keySet["Alt | E"] = "0.2";
			keySet["Ctrl | Shift | Alt | `"] = "0.2";
		}

		public TimeSpan GameIdleTime { get; private set; }
		public TimeSpan MouseIdleTime { get; private set; }
		public List<List<KeyConfig>> KeySets { get; private set; }

		Config()
		{
		}

		public Config(string path)
		{
			var data = new IniData();
			DefaultConfig(data);
			if (File.Exists(path)) data.Merge(Parser.ReadFile(path));
			else DefaultKeySets(data);
			Parser.WriteFile(path, data);
			var config = data[ConfigSectionName];
			GameIdleTime = TimeSpan.FromSeconds(
				config.GetKeyData(nameof(GameIdleTime), ConfigSectionName)?.ParseNum(ConfigSectionName) ??
				Default.GameIdleTime.TotalSeconds);
			MouseIdleTime = TimeSpan.FromSeconds(
				config.GetKeyData(nameof(MouseIdleTime), ConfigSectionName)?.ParseNum(ConfigSectionName) ??
				Default.MouseIdleTime.TotalSeconds);
			KeySets = data.Sections.Where(s => s.Keys.Count > 0 && s.SectionName.StartsWith("KeySet"))
				.Select(s => s.Keys.Select(keyData => KeyConfig.From(keyData, s.SectionName)).ToList()).ToList();
		}
	}

	public class KeyConfig
	{
		public VirtualKey Key;
		public TimeSpan Duration;
		public bool Ctrl;
		public bool Shift;
		public bool Alt;

		public override string ToString()
		{
			s_Builder.Clear();
			if (Ctrl) s_Builder.Append("CTRL").Append(" | ");
			if (Shift) s_Builder.Append("SHIFT").Append(" | ");
			if (Alt) s_Builder.Append("ALT").Append(" | ");
			string str = s_Builder.Append(Key.KeyToStr()).Append(" = ").Append(Duration.TotalSeconds).ToString();
			s_Builder.Clear();
			return str;
		}

		static readonly StringBuilder s_Builder = new StringBuilder();

		public static KeyConfig From(KeyData data, string section)
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
						Console.Error.WriteLine($"Invalid key: {str} at [{section}] {data.KeyName} = {data.Value}");
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
							string message = $"Invalid modifier: {str} at [{section}] {data.KeyName} = {data.Value}";
							Console.Error.WriteLine(message);
							break;
					}
				}
			}

			config.Duration = TimeSpan.FromSeconds(data.ParseNum(section) ?? 0.2);
			return config;
		}
	}
}