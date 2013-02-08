using System;
using System.IO;

namespace LiveSync
{
	internal class Program
	{
		private const string SettingsPath = "settings.xml";

		static void Main()
		{
			Settings settings;
			var serializer = new SimpleSerializer<Settings>(SettingsPath);
			if (!File.Exists(SettingsPath))
			{
				settings = new Settings {WatchPath = " ", TargetPath = " ", Patterns = new[] {" "}};
				serializer.Serialize(settings);
			}
			else
			{
				settings = serializer.Deserialize();
			}
			
			if (settings == null || string.IsNullOrWhiteSpace(settings.WatchPath) || string.IsNullOrWhiteSpace(settings.TargetPath) || settings.Patterns == null)
			{
				Console.WriteLine("Please create a {0} file first!", SettingsPath);
				return;
			}
			new LiveSync(settings.WatchPath, settings.TargetPath, settings.Patterns);
		}
	}
}
