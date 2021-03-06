namespace TextDispatcherGenerator;

[Generator]
	public class Encoder : IIncrementalGenerator
	{
        public const string EncoderAttribute = @"
namespace TextDispatcher
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class EncoderAttribute : System.Attribute
    {
    }
}";
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
       // Add the marker attribute to the compilation.
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "EncoderAttribute.g.cs", 
            SourceText.From(EncoderAttribute, Encoding.UTF8))); 

        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider(
           predicate: (s, t) => s is ClassDeclarationSyntax cl && cl.AttributeLists.Count > 0,
           transform: ClassByAttributeFunc("TextDispatcher.EncoderAttribute")).Collect();
        
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

            context.AddSource($"{symbol.Name}.Encoder.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            sb.Clear();
        }

    }

    private void GenerateSymbol(StringBuilder sb, ITypeSymbol s)
    {
        // Open namespace, this is the outer container.
        using var nsScope = new Scope(sb, $"namespace {s.ContainingNamespace.Name}");
        var methods = s.GetMembers().OfType<IMethodSymbol>();
        var enumName = $"{s.Name}Token";

        {
            // Create an enum to capture all methods on the object.
            using var enumScope = nsScope.NewScope($"public enum {enumName}");

            // Add an enum case for each public method.
            foreach (var method in methods.Where(m => m.IsVoidTakingVoid(m.Name)))
                enumScope.Text($"{method.Name},");
        }

        using var clScope = nsScope.NewScope(
            $"{s.DeclaredAccessibility.ToString().ToLowerInvariant()} partial class {s.Name}");

        clScope.GenerateFunction(
            methods,
            $"public {enumName} Encode(string arg)",
            method => $"case \"{method.Symbol()}\": return {enumName}.{method.Name};",
            "{}",
            "{}"
            );
        clScope.GenerateFunction(
            methods,
            $"public string Decode({enumName} arg)",
            method => $"case {enumName}.{method.Name}: return \"{method.Symbol()}\";",
            "{}",
            "{}"
            );
    }

}

