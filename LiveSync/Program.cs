using System;
using System.IO;
using Newtonsoft.Json;

namespace LiveSync
{
	internal class Program
	{
		private const string SettingsPath = "settings.json";

		static void Main()
		{
			Settings settings;
			if (!File.Exists(SettingsPath))
			{
				settings = new Settings {WatchPath = "", TargetPath = "", Patterns = new[] {""}};
				var json = JsonConvert.SerializeObject(settings);
				File.WriteAllText(SettingsPath, json);
			}
			else
			{
				settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingsPath));
			}
			
			if (settings == null || string.IsNullOrEmpty(settings.WatchPath) || string.IsNullOrEmpty(settings.TargetPath) || settings.Patterns == null)
			{
				Console.WriteLine("Please create a {0} file first!", SettingsPath);
				return;
			}
			new LiveSync(settings.WatchPath, settings.TargetPath, settings.Patterns);
		}
	}
}
