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
				settings = new Settings {WatchPath = " ", TargetPath = " ", Patterns = new[] {" "}, Overwrite = false, ActivityTimeout = 1000};
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
			var liveSync = new LiveSync(settings.WatchPath, settings.TargetPath, settings.Patterns, settings.Overwrite, settings.ActivityTimeout);
			liveSync.Start();
			var isRunning = true;
			while (isRunning)
			{
				var key = Console.ReadKey(true);
				switch (key.Key)
				{
					case ConsoleKey.Q:
						isRunning = false;
						break;
				}
			}
			liveSync.Stop();
		}
	}
}
