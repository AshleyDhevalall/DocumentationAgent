using DocumentationAgent.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DocumentationAgent.Core.Parsing;
public class CodeParser
{
  public List<CodeEntity> ParseDirectory(string path)
  {
    var entities = new List<CodeEntity>();

    var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

    foreach (var file in files)
    {
      var code = File.ReadAllText(file);
      var tree = CSharpSyntaxTree.ParseText(code);
      var root = tree.GetRoot();

      var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

      foreach (var cls in classes)
      {
        var classEntity = new CodeEntity
        {
          Name = cls.Identifier.Text,
          Type = "Class",
          Namespace = GetNamespace(cls),
          FilePath = file,
          SourceCode = cls.ToFullString()
        };

        entities.Add(classEntity);

        var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
          var methodEntity = new CodeEntity
          {
            Name = method.Identifier.Text,
            Type = "Method",
            Namespace = classEntity.Namespace,
            FilePath = file,
            SourceCode = method.ToFullString()
          };

          // Extract method calls
          var invocations = method.DescendantNodes()
              .OfType<InvocationExpressionSyntax>();

          foreach (var inv in invocations)
          {
            methodEntity.MethodCalls.Add(inv.Expression.ToString());
          }

          entities.Add(methodEntity);
        }
      }
    }

    return entities;
  }

  private string GetNamespace(SyntaxNode node)
  {
    var ns = node.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
    return ns?.Name.ToString() ?? "Global";
  }
}
