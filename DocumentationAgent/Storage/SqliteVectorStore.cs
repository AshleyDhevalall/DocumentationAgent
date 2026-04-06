using DocumentationAgent.Models;

namespace CodeExplainer.Storage
{
    public class SqliteVectorStore : DocumentationAgent.AI.IVectorStore
    {
        public async Task Insert(VectorRecord record)
        {
            // Dummy implementation for build
            await Task.CompletedTask;
        }
    }
}
