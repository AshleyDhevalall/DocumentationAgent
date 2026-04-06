using Microsoft.CodeAnalysis;

namespace DocumentationAgent.Core;

  public class SymbolResolver
  {
      private readonly Compilation _compilation;
      public SymbolResolver(Compilation compilation)
      {
          _compilation = compilation;
      }
      // Dummy method for build
      public object ResolveMethod(string name) => null;
  }
