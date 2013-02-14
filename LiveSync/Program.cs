using System;
using System.IO;

namespace LiveSync
{
  internal class Program
  {
    private const string SettingsPath = "LiveSync.xml";

    static void Main()
    {
      Settings settings;
      var serializer = new SimpleSerializer<Settings>(SettingsPath);
      if (!File.Exists(SettingsPath))
      {
        settings = new Settings { WatchPath = " ", TargetPath = " ", Patterns = new[] { " " }, Overwrite = false, ActivityTimeout = 1000 };
        serializer.Serialize(settings);
      }
      else
      {
        settings = serializer.Deserialize();
      }

      LiveSync liveSync = null;
      if (settings == null
        || string.IsNullOrWhiteSpace(settings.WatchPath)
        || string.IsNullOrWhiteSpace(settings.TargetPath)
        || settings.Patterns == null)
      {
        Console.Title = "LiveSync";
        Console.WriteLine("Please create a {0} file first!", SettingsPath);
      }
      else
      {
        Console.Title = string.Format("LiveSync - {0}",
                                      string.IsNullOrWhiteSpace(settings.Title)
                                        ? settings.WatchPath
                                        : Environment.ExpandEnvironmentVariables(settings.Title));
        liveSync = new LiveSync(
          Environment.ExpandEnvironmentVariables(settings.WatchPath),
          Environment.ExpandEnvironmentVariables(settings.TargetPath),
          settings.Patterns,
          settings.Overwrite,
          settings.ActivityTimeout);
        liveSync.Start();
      }
      Console.WriteLine("Press Q to quit!");
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
      if (liveSync != null)
      {
        liveSync.Stop();
      }
    }
  }
}
