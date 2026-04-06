using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DocumentationAgent.AI;

public class OllamaClient
{
  private readonly HttpClient _http;
  private readonly string _embeddingModel;
  private readonly string _generationModel;
  private readonly ILogger<OllamaClient> _logger;

  public OllamaClient(IConfiguration config, ILogger<OllamaClient> logger)
  {
    var section = config.GetSection("ollama");
    var baseUrl = section["baseUrl"] ?? "http://localhost:11434";
    _embeddingModel = section["embeddingModel"] ?? "nomic-embed-text";
    _generationModel = section["generationModel"] ?? "llama3";
    _http = new HttpClient
    {
      BaseAddress = new Uri(baseUrl)
    };
    _logger = logger;
  }

  public async Task<float[]> GetEmbedding(string text)
  {
    var payload = new
    {
      model = _embeddingModel,
      prompt = text,
      options = new { truncate = 8192 }
    };
    _logger.LogInformation("Sending embedding request: {Payload}", JsonSerializer.Serialize(payload));
    var response = await _http.PostAsJsonAsync("/api/embeddings", payload);
    var responseContent = await response.Content.ReadAsStringAsync();
    _logger.LogInformation("Embedding response: {StatusCode} {Content}", response.StatusCode, responseContent);

    if (!response.IsSuccessStatusCode)
    {
      _logger.LogError("Failed to get embedding: {StatusCode} {Content}", response.StatusCode, responseContent);
      throw new Exception($"Failed to get embedding: {response.StatusCode} {responseContent}");
    }

    var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseContent);
    if (result == null || result.embedding == null)
    {
      _logger.LogError("Embedding response is null or missing 'embedding' property. Raw: {Content}", responseContent);
      throw new Exception("Embedding response is null or missing 'embedding' property.");
    }

    return result.embedding;
  }

  public async Task<string> Generate(string prompt)
  {
    var payload = new
    {
      model = _generationModel,
      prompt = prompt,
      stream = false
    };
    _logger.LogInformation("Sending generate request: {Payload}", JsonSerializer.Serialize(payload));
    var response = await _http.PostAsJsonAsync("/api/generate", payload);
    var responseContent = await response.Content.ReadAsStringAsync();
    _logger.LogInformation("Generate response: {StatusCode} {Content}", response.StatusCode, responseContent);

    if (!response.IsSuccessStatusCode)
    {
      _logger.LogError("Failed to generate text: {StatusCode} {Content}", response.StatusCode, responseContent);
      throw new Exception($"Failed to generate text: {response.StatusCode} {responseContent}");
    }

    var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseContent);
    if (result == null || string.IsNullOrWhiteSpace(result.response))
    {
      _logger.LogError("Text generation response is null or missing 'response' property. Raw: {Content}", responseContent);
      throw new Exception("Text generation response is null or missing 'response' property.");
    }

    return result.response.Trim();
  }
}

public class OllamaEmbeddingResponse
{
  public float[] embedding { get; set; }
}

public class OllamaGenerateResponse
{
  public string response { get; set; }
}
