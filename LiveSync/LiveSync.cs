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
		private readonly ConcurrentQueue<FileSystemEventArgs> _queue;
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
			_queue = new ConcurrentQueue<FileSystemEventArgs>();
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

		private static bool Equals(FileSystemEventArgs left, FileSystemEventArgs right)
		{
			return left.ChangeType == right.ChangeType && left.Name == right.Name;
		}

		private static bool Equals(RenamedEventArgs left, FileSystemEventArgs right)
		{
			return left.ChangeType == right.ChangeType && left.Name == right.Name && left.OldName == ((RenamedEventArgs)right).OldName;
		}

		private void TimerCallback(object state)
		{
			FileSystemEventArgs e;
			FileSystemEventArgs last = new RenamedEventArgs(WatcherChangeTypes.Changed, "", "", "");
			while (_queue.TryDequeue(out e))
			{
				try
				{
					switch (e.ChangeType)
					{
						case WatcherChangeTypes.Created:
							if (Equals(e, last)) return;
							Console.Write("Copying {0}..", e.Name);
							Copy(e);
							Console.WriteLine(" OK!");
							break;
						case WatcherChangeTypes.Deleted:
							if (Equals(e, last)) return;
							Console.Write("Deleting {0}..", e.Name);
							Delete(e);
							Console.WriteLine(" OK!");
							break;
						case WatcherChangeTypes.Changed:
							if (Equals(e, last)) return;
							Console.Write("Copying {0}..", e.Name);
							Copy(e);
							Console.WriteLine(" OK!");
							break;
						case WatcherChangeTypes.Renamed:
							var renamedEventArgs = (RenamedEventArgs)e;
							if (Equals(renamedEventArgs, last)) return;
							Console.Write("Moving {0} to {1}..", e.Name, renamedEventArgs.OldName);
							Move(renamedEventArgs);
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
			HandleEvent(e, e.OldName);
		}

		private void HandleEvent(object sender, FileSystemEventArgs e)
		{
			HandleEvent(e, e.Name);
		}

		private void HandleEvent(FileSystemEventArgs e, string name)
		{
			if (!IsMatch(name)) return;
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

		public void Copy(FileSystemEventArgs e)
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

		public void Delete(FileSystemEventArgs e)
		{
			var target = Path.Combine(_targetPath, e.Name);
			TryRemoveReadonly(target);
			File.Delete(target);
		}

		public void Move(RenamedEventArgs e)
		{
			File.Move(Path.Combine(_targetPath, e.OldName), Path.Combine(_targetPath, e.Name));
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
}