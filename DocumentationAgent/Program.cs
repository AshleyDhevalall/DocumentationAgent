using DocumentationAgent.Chunking;
using DocumentationAgent.Models;
using DocumentationAgent.AI;
using DocumentationAgent.Core.Parsing;
using DocumentationAgent.Core;
using DocumentationAgent.Core.RoslynHelper;
using CodeExplainer.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

class Program
{
  static async Task Main(string[] args)
  {
    var config = new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appconfig.json", optional: false, reloadOnChange: true)
      .Build();

    using var loggerFactory = LoggerFactory.Create(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(LogLevel.Information);
    });
    var logger = loggerFactory.CreateLogger<OllamaClient>();

    var store = new SqliteVectorStore();
    var ollama = new OllamaClient(config, logger);
    var search = new HybridSearchEngine(store);

    while (true)
    {
      Console.WriteLine("--- Documentation Agent ---\n");
      Console.WriteLine("1. Index repo");
      Console.WriteLine("2. Ask question");
      Console.WriteLine("0. Exit");
      Console.Write("\nEnter option: ");
      var raw = Console.ReadLine();
      var choice = raw?.Trim();

      if (string.IsNullOrEmpty(choice))
      {
        Console.WriteLine("Invalid choice.");
        continue;
      }

      // If user typed a natural-language question directly at the prompt, treat it as an Ask request
      if (LooksLikeQuestion(choice))
      {
        await HandleAsk(choice, search, ollama);
        continue;
      }

      if (choice == "0" || string.Equals(choice, "exit", StringComparison.OrdinalIgnoreCase))
        break;

      switch (choice.ToLowerInvariant())
      {
        case "1":
        case "index":
          {
            var parser = new CodeParser();
            var entities = parser.ParseDirectory(@"C:\Projects\ProductApi");
            var chunker = new SmartChunker();
            var chunks = chunker.CreateChunks(entities);

            // Parallelize embedding generation and insertion for faster indexing
            var tasks = chunks.Select(async chunk =>
            {
                var embedding = await ollama.GetEmbedding(chunk.Content);
                await store.Insert(new VectorRecord
                {
                    Id = chunk.Id,
                    Type = chunk.Type,
                    Content = chunk.Content,
                    Vector = embedding,
                    Metadata = chunk.Metadata
                });
                Console.WriteLine($"Indexed {chunk.Id}");
            });
            await Task.WhenAll(tasks);

            Console.WriteLine("Indexing complete.");
            break;
          }

        case "2":
        case "ask":
          {
            Console.Write("Enter your question: ");
            var query = Console.ReadLine();
            await HandleAsk(query, search, ollama);
            break;
          }

        default:
          Console.WriteLine("Invalid choice.");
          break;
      }
    }

    Console.WriteLine("Exiting RepoBrain CLI.");
  }

  static bool LooksLikeQuestion(string input)
  {
    if (string.IsNullOrWhiteSpace(input)) return false;
    input = input.Trim();
    // If ends with a question mark, or starts with common question verbs/phrases, or contains multiple words and is reasonably long
    var lowered = input.ToLowerInvariant();
    if (input.EndsWith("?")) return true;
    if (lowered.StartsWith("tell me") || lowered.StartsWith("explain") || lowered.StartsWith("what") || lowered.StartsWith("how") || lowered.StartsWith("who") || lowered.StartsWith("where") || lowered.StartsWith("why")) return true;
    if (input.Contains(' ') && input.Length > 20) return true;
    return false;
  }

  static async Task HandleAsk(string query, HybridSearchEngine search, OllamaClient ollama)
  {
    if (string.IsNullOrWhiteSpace(query))
    {
      Console.WriteLine("No question provided.");
      return;
    }

    var compilation = RoslynHelper.BuildCompilation(@"C:\Projects\ProductApi");
    var resolver = new SymbolResolver(compilation);

    var rag = new RagEngine(search, resolver, ollama);
    var doc = await rag.Ask(query);

    Console.WriteLine("\n--- Generated Documentation ---\n");
    Console.WriteLine(doc);
  }
}