global using System;
global using System.Collections.Immutable;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading;
global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.CSharp;
global using Microsoft.CodeAnalysis.CSharp.Syntax;
global using Microsoft.CodeAnalysis.Text;

global using static TextDispatcherGenerator.Globals;

namespace TextDispatcherGenerator;

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

static class Globals
{
    internal static bool IsVoidTakingVoid(this IMethodSymbol method, string methodName) =>
        SyntaxFacts.IsValidIdentifier(methodName) &&
        method.Parameters.Length == 0 &&
        method.ReturnsVoid &&
        !method.GetAttributes().Any(a => a.ToString() == "TextDispatcher.NoDispatchAttribute");

    internal static string Symbol(this IMethodSymbol m)
    {
        var attrs = m.GetAttributes().Where(a => a.AttributeClass.Name == "SymbolAttribute");
        if(attrs.Count() == 0)
            return m.Name;
        else
            return (string)attrs.First().ConstructorArguments[0].Value;
    }
    internal static void GenerateFunction(
        this Scope classScope,
        IEnumerable<IMethodSymbol> methods,
        string signature,
        Func<IMethodSymbol, string> caseString,
        string parseIntAction,
        string parseStringAction
        )
    {

        using var fnScope = classScope.NewScope(signature);
        using var swScope = fnScope.NewScope("switch(arg)");

        // Add a case statement for each void returning and void taking method.
        foreach (var method in methods.Where(m => m.IsVoidTakingVoid(m.Name)))
            swScope.Text(caseString(method));

        // In the default: case, process other special methods on the class
        using var dfScope = swScope.NewScope("default:");

        // First, maybe the arg to dispatch is an integer and we have a method for it.
        foreach (var method in methods.Where(m => m.Name == "ParseInt"))
           dfScope.Text(parseIntAction); 

        // Second, we might want to call a default method in case nothing matches.
        foreach (var method in methods.Where(m => m.Name == "ParseString"))
           dfScope.Text(parseStringAction); 

        // If everything fails, raise an exception.
        dfScope.Text($"throw new System.ArgumentException($\"Method '{{arg}}' doesn't exist on this class.\");");
    }

    internal static Func<GeneratorSyntaxContext, CancellationToken, ITypeSymbol>
        ClassByAttributeFunc(string attributeName) => 
            (GeneratorSyntaxContext context, CancellationToken cancellationToken) =>
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

                if (fullName == attributeName)
                {
                    if (context.SemanticModel.GetDeclaredSymbol(decl, cancellationToken) is ITypeSymbol typeSymbol)
                    {
                        return typeSymbol;
                    }
                }
            }
        }

        return null;
    };

}
