namespace TextDispatcherGenerator;

[Generator]
	public class TextDispatcher : IIncrementalGenerator
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
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class SymbolAttribute : System.Attribute
    {
        string _symbol;
        public SymbolAttribute(string symbol) => this._symbol = symbol;
    }
}";
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
       // Add the marker attribute to the compilation.
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
            context.AddSource($"{symbol.Name}.Dispatcher.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
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
    private void GenerateSymbol(StringBuilder sb, ITypeSymbol s)
    {
        // namespace -> partial class -> function -> switch statement
        using var nsScope = new Scope(sb, $"namespace {s.ContainingNamespace.Name}"); 
        var methods = s.GetMembers().OfType<IMethodSymbol>();
        using var clScope = nsScope.NewScope(
            $"{s.DeclaredAccessibility.ToString().ToLowerInvariant()} partial class {s.Name}");

        clScope.GenerateFunction(
            methods,
            "public void Dispatch(string arg)",
            method => $"case \"{method.Symbol()}\": {method.Name}(); break;",
           "if(System.Int32.TryParse(arg, out var v)) { ParseInt(v);return;}",
           "\nParseString(arg);\nreturn;"
            );
    }
}

