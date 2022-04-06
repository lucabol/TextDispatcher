using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TextDispatcherGenerator
{
    [Generator]
	public class IncrementalGenerator : IIncrementalGenerator
	{
            public const string DispatcherAttribute = @"
namespace TextDispatcher
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class DispatcherAttribute : System.Attribute
    {
    }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class NoDispatchAttribute : System.Attribute
    {
    }
}";
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
           // Add the marker attribute to the compilation
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "DispatcherAttribute.g.cs", 
                SourceText.From(DispatcherAttribute, Encoding.UTF8))); 

            var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
               predicate: (s, t) => s is ClassDeclarationSyntax cl && cl.AttributeLists.Count > 0,
               transform: GetTypeSymbols).Collect();
            
            context.RegisterSourceOutput(classDeclarations, GenerateSource);
        }

        private void GenerateSource(SourceProductionContext context, ImmutableArray<ITypeSymbol> typeSymbols)
        {
            var sb = new StringBuilder();
            foreach (var symbol in typeSymbols)
            {
                if (symbol is null)
                    continue;

                GenerateSymbol(sb, symbol);
                context.AddSource($"{symbol.Name}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
                sb.Clear();
            }

        }

        private ITypeSymbol GetTypeSymbols(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var decl = (ClassDeclarationSyntax)context.Node;

            // loop through all the attributes on the method
            foreach (AttributeListSyntax attributeListSyntax in decl.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        // weird, we couldn't get the symbol, ignore it
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == "TextDispatcher.DispatcherAttribute")
                    {
                        if (context.SemanticModel.GetDeclaredSymbol(decl, cancellationToken) is ITypeSymbol typeSymbol)
                        {
                            return typeSymbol;
                        }
                    }
                }
            }

            return null;
        }
        class Scope: IDisposable {
            const int tabSize = 2;
            int tabs;
            StringBuilder sb;

            private void AppendLine(int n, string line)
            {
                for (int i = 0; i < n * tabSize; i++)
                    sb.Append(' ');
                sb.AppendLine(line);
            }
            internal Scope(StringBuilder asb, string startLine, int tabs = 0)
            {
                this.sb = asb;
                AppendLine(tabs, startLine);
                AppendLine(tabs, "{");
                this.tabs = tabs + 1;
            } 
            internal void Text(string text) {
                foreach(var line in text.Split('\n')) {
                    AppendLine(tabs, line);
                }
            }
            public void Dispose()
            {
                AppendLine(tabs == 0 ? 0 : --tabs, "}");
                GC.SuppressFinalize(this);
            }
            internal Scope NewScope(string startLine) => new Scope(sb, startLine, tabs);
        };

        private void GenerateSymbol(StringBuilder sb, ITypeSymbol s)
        {
            // namespace -> partial class -> function -> switch statement
            using var nsScope = new Scope(sb, $"namespace {s.ContainingSymbol.Name}"); 
            using var clScope = nsScope.NewScope( $"{s.DeclaredAccessibility.ToString().ToLowerInvariant()} partial class {s.Name}");
            using var fnScope = clScope.NewScope("public void Dispatch(string arg)");
            using var swScope = fnScope.NewScope("switch(arg)");

            // Get all the methods.
            var methods = s.GetMembers().OfType<IMethodSymbol>();

            foreach(var m in methods) {
                foreach(var a in m.GetAttributes())
                    Console.WriteLine($"// {a.ToString()} {a.AttributeClass.Name}");
            }

            // Add a case statement for each void returning and void taking method.
            foreach (var method in methods.Where(m => IsVoidTakingVoid(m, m.Name)))
                swScope.Text($"case \"{method.Name}\": {method.Name}(); break;");

            // In the default: case, process other special methods on the class
            using var dfScope = swScope.NewScope("default:");

            // First, maybe the arg to dispatch is an integer and we have a method for it.
            foreach (var method in methods.Where(m => m.Name == "ParseInt"))
               dfScope.Text("if(System.Int32.TryParse(arg, out var v)) { ParseInt(v);return;}"); 

            // Second, we might want to call a default method in case nothing matches.
            foreach (var method in methods.Where(m => m.Name == "ParseString"))
               dfScope.Text("\nParseString(arg);\nreturn;"); 

            // If everything fails, raise an exception.
            dfScope.Text($"throw new System.ArgumentException(\"Method {{arg}} doesn't exist on this class.\");");
        }

        private static bool IsVoidTakingVoid(IMethodSymbol method, string methodName) =>
            SyntaxFacts.IsValidIdentifier(methodName) &&
            method.Parameters.Length == 0 &&
            method.ReturnsVoid &&
            !method.GetAttributes().Any(a => a.AttributeConstructor?.Name == "TextDispatcher.NoDispatch");
    }
}

