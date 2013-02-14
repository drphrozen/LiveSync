using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LiveSync
{
  public class LiveSync
  {
    private readonly HashSet<Regex> _patterns;
    private readonly string _rootPath;
    private readonly string _targetPath;
    private readonly ConcurrentQueue<FileSystemEventWrapper> _queue;
    private readonly Timer _timer;
    private readonly bool _overwrite;
    private readonly int _activityTimeout;
    private readonly FileSystemWatcher _fileSystemWatcher;

    public LiveSync(string rootPath, string targetPath, IEnumerable<string> patterns, bool? overwrite, int? activityTimeout)
    {
      _overwrite = overwrite ?? false;
      _activityTimeout = activityTimeout ?? 1000;
      _rootPath = Path.GetFullPath(rootPath);
      _targetPath = Path.GetFullPath(targetPath);
      _queue = new ConcurrentQueue<FileSystemEventWrapper>();
      _patterns = new HashSet<Regex>(patterns.Select(x => new Regex(x, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)));
      _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
      _fileSystemWatcher = new FileSystemWatcher(_rootPath)
      {
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
        IncludeSubdirectories = true
      };
      Directory.SetCurrentDirectory(_rootPath);
    }

    public void Start()
    {
      _fileSystemWatcher.Changed += HandleEvent;
      _fileSystemWatcher.Created += HandleEvent;
      _fileSystemWatcher.Deleted += HandleEvent;
      _fileSystemWatcher.Renamed += HandleRenamedEvent;
      _fileSystemWatcher.Error += FileSystemWatcherOnError;
      _fileSystemWatcher.EnableRaisingEvents = true;
      Console.WriteLine("Live syncing {0} to {1}..", _rootPath, _targetPath);
    }

    public void Stop()
    {
      _fileSystemWatcher.EnableRaisingEvents = false;
      _fileSystemWatcher.Changed -= HandleEvent;
      _fileSystemWatcher.Created -= HandleEvent;
      _fileSystemWatcher.Deleted -= HandleEvent;
      _fileSystemWatcher.Renamed -= HandleRenamedEvent;
      _fileSystemWatcher.Error -= FileSystemWatcherOnError;
      Console.WriteLine("Stopped live sync..");
    }

    private void TimerCallback(object state)
    {
      FileSystemEventWrapper e;
      var last = new FileSystemEventWrapper { ChangeType = WatcherChangeTypes.All, Name = "", NewName = "" };
      while (_queue.TryDequeue(out e))
      {
        try
        {
          if (e == last) continue;
          switch (e.ChangeType)
          {
            case WatcherChangeTypes.Created:
              ColorConsole.Write(ConsoleColor.DarkGreen, "Creating");
              Console.Write(" {0}..", e.Name);
              Copy(e);
              Console.WriteLine(" OK!");
              break;
            case WatcherChangeTypes.Deleted:
              ColorConsole.Write(ConsoleColor.DarkRed, "Deleting");
              Console.Write(" {0}..", e.Name);
              Delete(e);
              Console.WriteLine(" OK!");
              break;
            case WatcherChangeTypes.Changed:
              Console.Write("Copying {0}..", e.Name);
              Copy(e);
              Console.WriteLine(" OK!");
              break;
            case WatcherChangeTypes.Renamed:
              ColorConsole.Write(ConsoleColor.DarkYellow, "Moving");
              Console.Write(" {0} to {1}..", e.Name, e.NewName);
              Move(e);
              Console.WriteLine(" OK!");
              break;
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine(" ERROR!");
          Console.WriteLine(ex.Message);
        }
        last = e;
      }
    }

    private void HandleRenamedEvent(object sender, RenamedEventArgs e)
    {
      HandleEvent(new FileSystemEventWrapper { ChangeType = e.ChangeType, Name = e.OldName, NewName = e.Name });
    }

    private void HandleEvent(object sender, FileSystemEventArgs e)
    {
      HandleEvent(new FileSystemEventWrapper { ChangeType = e.ChangeType, Name = e.Name, NewName = null });
    }

    private void HandleEvent(FileSystemEventWrapper e)
    {
      if (!IsMatch(e.Name)) return;
      _timer.Change(_activityTimeout, Timeout.Infinite);
      _queue.Enqueue(e);
    }

    public bool IsMatch(string path)
    {
      return _patterns.Any(x => x.IsMatch(path));
    }

    private static void FileSystemWatcherOnError(object sender, ErrorEventArgs e)
    {
      Console.WriteLine(e.GetException());
    }

    public void Copy(FileSystemEventWrapper e)
    {
      var origin = Path.Combine(_rootPath, e.Name);
      var target = Path.Combine(_targetPath, e.Name);
      var targetDirectory = Path.GetDirectoryName(target);
      if (targetDirectory == null) throw new NullReferenceException();
      if (!Directory.Exists(targetDirectory))
      {
        Directory.CreateDirectory(targetDirectory);
      }
      else
      {
        TryRemoveReadonly(target);
      }
      File.Copy(origin, target, true);
    }

    public void Delete(FileSystemEventWrapper e)
    {
      Delete(e.Name);
    }

    public void Delete(string name)
    {
      var target = Path.Combine(_targetPath, name);
      TryRemoveReadonly(target);
      File.Delete(target);
    }

    public void Move(FileSystemEventWrapper e)
    {
      var oldName = Path.Combine(_targetPath, e.Name);
      var newName = Path.Combine(_targetPath, e.NewName);
      if (File.Exists(oldName))
      {
        if (File.Exists(newName))
          Delete(newName);
        File.Move(oldName, newName);
      }
      else
      {
        Copy(e);
      }
    }

    public void TryRemoveReadonly(string path)
    {
      if (!_overwrite || !File.Exists(path)) return;
      var attributes = File.GetAttributes(path);
      if (attributes.HasFlag(FileAttributes.ReadOnly))
      {
        File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
      }
    }
  }

  public static class ColorConsole
  {
    public static void Write(string value, ConsoleColor color)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.Write(value);
      Console.ForegroundColor = oldColor;
    }

    public static void Write(ConsoleColor color, string format, params object[] arg)
    {
      var oldColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.Write(format, arg);
      Console.ForegroundColor = oldColor;
    }
  }
}