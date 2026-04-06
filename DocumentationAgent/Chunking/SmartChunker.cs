using System.Text;
using DocumentationAgent.Models;

namespace DocumentationAgent.Chunking;

public class SmartChunker
{
  // Maximum number of characters per chunk (configurable). Choose a conservative default to avoid exceeding model context.
  private readonly int _maxChunkSize;
  // Number of characters to overlap between consecutive chunks to preserve context.
  private readonly int _overlapSize;

  public SmartChunker(int maxChunkSize = 2000, int overlapSize = 200)
  {
    _maxChunkSize = Math.Max(256, maxChunkSize);
    _overlapSize = Math.Max(0, Math.Min(_maxChunkSize / 2, overlapSize));
  }

  public List<Chunk> CreateChunks(List<CodeEntity> entities)
  {
    var chunks = new List<Chunk>();

    foreach (var entity in entities)
    {
      var source = entity.SourceCode ?? string.Empty;

      if (source.Length <= _maxChunkSize)
      {
        chunks.Add(new Chunk
        {
          Id = entity.Id,
          Type = entity.Type?.ToLower() ?? "",
          Content = source,
          Metadata = new Dictionary<string, string>
              {
                  { "name", entity.Name },
                  { "file", entity.FilePath }
              }
        });

        continue;
      }

      // Split large source into lines and build chunks that do not exceed _maxChunkSize
      var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      var builder = new StringBuilder();
      var partIndex = 0;

      for (int i = 0; i < lines.Length; i++)
      {
        var line = lines[i];

        // If adding this line would exceed the max, finalize current chunk and start a new one
        if (builder.Length + line.Length + 1 > _maxChunkSize)
        {
          var chunkText = builder.ToString();
          AddChunkFromEntity(chunks, entity, chunkText, partIndex);
          partIndex++;

          // Prepare the next builder seeded with overlap from the end of the previous chunk
          var overlap = GetOverlapSuffix(chunkText, _overlapSize);
          builder.Clear();
          if (overlap.Length > 0)
            builder.Append(overlap);

          // If the line itself is larger than max chunk size, split the line directly
          if (line.Length > _maxChunkSize)
          {
            var start = 0;
            while (start < line.Length)
            {
              var take = Math.Min(_maxChunkSize - builder.Length - 1, line.Length - start);
              if (take <= 0)
              {
                // finalize current builder if no space
                var ct = builder.ToString();
                AddChunkFromEntity(chunks, entity, ct, partIndex);
                partIndex++;
                builder.Clear();
                continue;
              }

              builder.Append(line.Substring(start, take));
              start += take;

              if (builder.Length >= _maxChunkSize)
              {
                var ct = builder.ToString();
                AddChunkFromEntity(chunks, entity, ct, partIndex);
                partIndex++;
                var ov = GetOverlapSuffix(ct, _overlapSize);
                builder.Clear();
                if (ov.Length > 0) builder.Append(ov);
              }
            }

            // continue to next line
            continue;
          }
        }

        // Safe to append the line
        if (builder.Length > 0)
          builder.Append('\n');
        builder.Append(line);
      }

      // finalize remaining
      if (builder.Length > 0)
      {
        AddChunkFromEntity(chunks, entity, builder.ToString(), partIndex);
      }
    }

    return chunks;
  }

  private void AddChunkFromEntity(List<Chunk> chunks, CodeEntity entity, string content, int partIndex)
  {
    var id = partIndex == 0 ? entity.Id : $"{entity.Id}-{partIndex}";
    var md = new Dictionary<string, string>
    {
      { "name", entity.Name },
      { "file", entity.FilePath },
      { "part", partIndex.ToString() },
      { "original_length", (entity.SourceCode?.Length ?? 0).ToString() },
      { "chunk_length", content.Length.ToString() }
    };

    chunks.Add(new Chunk
    {
      Id = id,
      Type = entity.Type?.ToLower() ?? "",
      Content = content,
      Metadata = md
    });
  }

  private string GetOverlapSuffix(string text, int overlapSize)
  {
    if (string.IsNullOrEmpty(text) || overlapSize <= 0) return string.Empty;
    var take = Math.Min(overlapSize, text.Length);
    return text.Substring(text.Length - take, take);
  }
}

public class Chunk
{
  public string Id { get; set; }
  public string Type { get; set; }
  public string Content { get; set; }
  public Dictionary<string, string> Metadata { get; set; }
}
