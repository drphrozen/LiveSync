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

		public LiveSync(string rootPath, string targetPath, IEnumerable<string> patterns)
		{
			_rootPath = Path.GetFullPath(rootPath);
			_targetPath = Path.GetFullPath(targetPath);
			_queue = new ConcurrentQueue<FileSystemEventArgs>();
			Directory.SetCurrentDirectory(_rootPath);
			_patterns = new HashSet<Regex>(patterns.Select(x => new Regex(x, RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled)));
			var fileSystemWatcher = new FileSystemWatcher(_rootPath)
				                        {
					                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
					                        IncludeSubdirectories = true
				                        };
			fileSystemWatcher.Changed += HandleEvent;
			fileSystemWatcher.Created += HandleEvent;
			fileSystemWatcher.Deleted += HandleEvent;
			fileSystemWatcher.Renamed += HandleRenamedEvent;
			fileSystemWatcher.Error += FileSystemWatcherOnError;
			fileSystemWatcher.EnableRaisingEvents = true;
			_timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
			
			while (true)
			{
				Thread.Sleep(1);
			}
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
				
				switch (e.ChangeType)
				{
					case WatcherChangeTypes.Created:
						if (Equals(e, last)) return;
						Copy(e);
						break;
					case WatcherChangeTypes.Deleted:
						if (Equals(e, last)) return;
						Delete(e);
						break;
					case WatcherChangeTypes.Changed:
						if (Equals(e, last)) return;
						Copy(e);
						break;
					case WatcherChangeTypes.Renamed:
						var renamedEventArgs = (RenamedEventArgs) e;
						if (Equals(renamedEventArgs, last)) return;
						Move(renamedEventArgs);
						break;
				}
				last = e;
			}
		}

		private void HandleRenamedEvent(object sender, RenamedEventArgs e)
		{
			if (!IsMatch(e.OldName)) return;
			_timer.Change(1000, Timeout.Infinite);
			_queue.Enqueue(e);
		}

		private void HandleEvent(object sender, FileSystemEventArgs e)
		{
			if (!IsMatch(e.Name)) return;
			_timer.Change(1000, Timeout.Infinite);
			_queue.Enqueue(e);
		}

		public bool IsMatch(string path)
		{
			return _patterns.Any(x => x.IsMatch(path));
		}

		private static void FileSystemWatcherOnError(object sender, ErrorEventArgs e)
		{
			Console.WriteLine("ERROR {0}", e.GetException().Message);
		}

		public void Copy(FileSystemEventArgs e)
		{
			Console.WriteLine("Copy {0}", e.Name);
			var target = Path.Combine(_targetPath, e.Name);
			var targetDirectory = Path.GetDirectoryName(target);
			if(targetDirectory == null) throw new NullReferenceException();
			if (Directory.Exists(targetDirectory) == false)
			{
				Directory.CreateDirectory(targetDirectory);
			}
			File.Copy(Path.Combine(_rootPath, e.Name), Path.Combine(_targetPath, e.Name), true);
		}

		public void Delete(FileSystemEventArgs e)
		{
			Console.WriteLine("Delete {0}", e.Name);
			File.Delete(Path.Combine(_targetPath, e.Name));
		}

		public void Move(RenamedEventArgs e)
		{
			Console.WriteLine("Move {0} to {1}", e.OldName, e.Name);
			File.Move(Path.Combine(_targetPath, e.OldName), Path.Combine(_targetPath, e.Name));
		}
	}
}