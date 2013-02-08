using System.Xml.Serialization;

namespace LiveSync
{
	public class Settings
	{
		public string WatchPath { get; set; }
		public string TargetPath { get; set; }
		[XmlArrayItem("Pattern")]
		public string[] Patterns { get; set; }
	}
}