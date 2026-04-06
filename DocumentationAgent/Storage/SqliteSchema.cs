namespace DocumentationAgent;

public static class SqliteSchema
{
  public const string CreateTables = @"
    CREATE TABLE IF NOT EXISTS Chunks (
        Id TEXT PRIMARY KEY,
        Type TEXT,
        Name TEXT,
        FilePath TEXT,
        Content TEXT
    );

    CREATE TABLE IF NOT EXISTS Embeddings (
        Id TEXT PRIMARY KEY,
        Vector BLOB
    );

    CREATE INDEX IF NOT EXISTS idx_type ON Chunks(Type);
    ";
}
