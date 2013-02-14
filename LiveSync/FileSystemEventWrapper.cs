using System.IO;

namespace LiveSync
{
    public class FileSystemEventWrapper
    {
        public string Name { get; set; }
        public string NewName { get; set; }
        public WatcherChangeTypes ChangeType { get; set; }

        protected bool Equals(FileSystemEventWrapper other)
        {
            return string.Equals(Name, other.Name) && string.Equals(NewName, other.NewName) && ChangeType == other.ChangeType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (NewName != null ? NewName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) ChangeType;
                return hashCode;
            }
        }

        public static bool operator ==(FileSystemEventWrapper left, FileSystemEventWrapper right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FileSystemEventWrapper left, FileSystemEventWrapper right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((FileSystemEventWrapper) obj);
        }
    }
}