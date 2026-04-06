using DocumentationAgent.Models;
using System.Text;

namespace DocumentationAgent.AI;

public class DocGenerator
{
  private readonly OllamaClient _ollama;

  public DocGenerator(OllamaClient ollama)
  {
    _ollama = ollama;
  }

  public async Task<string> GenerateMarkdownAsync(string title, List<FlowPath> flows, List<VectorRecord> context)
  {
    var sb = new StringBuilder();
    sb.AppendLine($"# {title}");
    sb.AppendLine();

    // Combine all relevant chunk contents for a single model call
    var contextText = new StringBuilder();
    foreach (var record in context)
    {
      var name = record.Metadata != null && record.Metadata.TryGetValue("name", out var n) ? n : "(unknown)";
      contextText.AppendLine($"## {name}\n");
      contextText.AppendLine(record.Content);
      contextText.AppendLine();
    }

    // Build a single prompt
    var prompt = $"You are an expert code assistant. Use the following code context to answer the user's question as concisely as possible.\n\nQuestion: {title}\n\nRelevant code context:\n{contextText}\n\nAnswer:";

    var summary = await _ollama.Generate(prompt);
    if (!string.IsNullOrWhiteSpace(summary))
    {
      var trimmed = summary.Trim();
      if (trimmed.Length > 2000)
        trimmed = trimmed.Substring(0, 2000) + "...";
      sb.AppendLine(trimmed);
    }
    else
    {
      var excerpt = contextText.ToString().Length > 300 ? contextText.ToString().Substring(0, 300) + "..." : contextText.ToString();
      sb.AppendLine("```");
      sb.AppendLine(excerpt);
      sb.AppendLine("```");
    }

    // Mermaid diagrams (optional, keep as is)
    if (flows.Any())
    {
      sb.AppendLine("## Flows");
      sb.AppendLine("```mermaid");
      sb.AppendLine("graph TD");
      foreach (var flow in flows)
      {
        for (int i = 0; i < flow.Nodes.Count - 1; i++)
        {
          sb.AppendLine($"{flow.Nodes[i].Name} --> {flow.Nodes[i + 1].Name}");
        }
      }
      sb.AppendLine("```");
    }

    return sb.ToString();
  }
}
