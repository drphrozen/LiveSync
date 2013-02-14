using System.Xml.Serialization;

namespace LiveSync
{
  public class Settings
  {
    public string Title { get; set; }
    public string WatchPath { get; set; }
    public string TargetPath { get; set; }
    [XmlArrayItem("Pattern")]
    public string[] Patterns { get; set; }
    public bool? Overwrite { get; set; }
    public int? ActivityTimeout { get; set; }
  }
}