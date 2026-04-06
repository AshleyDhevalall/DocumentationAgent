using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DocumentationAgent.Core.RoslynHelper;
public static class RoslynHelper
{
  public static Compilation BuildCompilation(string path)
  {
    var syntaxTrees = new List<SyntaxTree>();
    foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
    {
      var code = File.ReadAllText(file);
      syntaxTrees.Add(CSharpSyntaxTree.ParseText(code));
    }

    var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };

    var compilation = CSharpCompilation.Create("RepoCompilation")
        .AddSyntaxTrees(syntaxTrees)
        .AddReferences(references);

    return compilation;
  }
}
