namespace DocumentationAgent.Models;

public class VectorRecord
{
  public string Id { get; set; }
  public string Type { get; set; }
  public string Content { get; set; }
  public float[] Vector { get; set; }
  public Dictionary<string, string> Metadata { get; set; }
}
