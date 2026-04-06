using System.Linq;
using System.Threading.Tasks;
using DocumentationAgent.Models;
using System.Collections.Generic;
using DocumentationAgent.Core;

namespace DocumentationAgent.AI;

public class RagEngine
{
  private readonly HybridSearchEngine _search;
  private readonly SymbolResolver _resolver;
  private readonly OllamaClient _ollama;
  private readonly DocGenerator _docGen;

  public RagEngine(HybridSearchEngine search, SymbolResolver resolver, OllamaClient ollama)
  {
    _search = search;
    _resolver = resolver;
    _ollama = ollama;
    _docGen = new DocGenerator(_ollama);
  }

  public async Task<string> Ask(string query)
  {
    // 1. Search for relevant code chunks
    var searchResults = await _search.Search(query);
    // 2. Generate documentation using DocGenerator
    var doc = await _docGen.GenerateMarkdownAsync(query, new List<FlowPath>(), searchResults);
    return doc;
  }
}
