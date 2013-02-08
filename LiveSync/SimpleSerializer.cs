using System.IO;
using System.Xml.Serialization;

namespace LiveSync
{
	public class SimpleSerializer<T>
	{
		private readonly string _path;
		private readonly XmlSerializer _serializer;
		public SimpleSerializer(string path)
		{
			_path = path;
			_serializer = new XmlSerializer(typeof (T));
		}

		public void Serialize(T t)
		{
			using (var stream = new FileStream(_path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				_serializer.Serialize(stream, t);				
			}
		}

		public T Deserialize()
		{
			using (var stream = File.OpenRead(_path))
			{
				return (T) _serializer.Deserialize(stream);
			}
		}
	}
}