namespace DocumentationAgent.Models;

public class Node
{
  public string Id { get; set; }
  public string Name { get; set; }
  public string Type { get; set; }

  public List<Edge> Edges { get; set; } = new();
}

public class FlowPath
{
  public List<Node> Nodes { get; set; } = new List<Node>();
}

public class CodeEntity
{
  public string Id { get; set; } = Guid.NewGuid().ToString();
  public string Name { get; set; }
  public string Type { get; set; } // Class, Method
  public string Namespace { get; set; }
  public string FilePath { get; set; }

  public List<string> Dependencies { get; set; } = new();
  public List<string> MethodCalls { get; set; } = new();

  public string SourceCode { get; set; }
}
