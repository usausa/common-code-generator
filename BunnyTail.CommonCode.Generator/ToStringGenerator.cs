namespace BunnyTail.CommonCode.Generator;

using System;
using System.Collections.Immutable;
using System.Text;

using BunnyTail.CommonCode.Generator.Helpers;
using BunnyTail.CommonCode.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class ToStringGenerator : IIncrementalGenerator
{
    private const string GenerateAttributeName = "BunnyTail.CommonCode.GenerateToStringAttribute";
    private const string IgnoreAttributeName = "BunnyTail.CommonCode.IgnoreToStringAttribute";

    private const string GenericEnumerableName = "System.Collections.Generic.IEnumerable<T>";

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => GetOptions(provider));

        var targetProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateAttributeName,
                static (node, _) => IsTypeSyntax(node),
                static (context, _) => GetTypeModel(context))
            .SelectMany(static (x, _) => x is not null ? ImmutableArray.Create(x) : [])
            .Collect();

        context.RegisterImplementationSourceOutput(
            optionProvider.Combine(targetProvider),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static GeneratorOptions GetOptions(AnalyzerConfigOptionsProvider provider)
    {
        var options = new GeneratorOptions();

        // Mode
        var mode = provider.GlobalOptions.GetValue<string?>("CommonCodeGeneratorToStringMode");
        if (String.IsNullOrEmpty(mode) || String.Equals(mode, "Default", StringComparison.OrdinalIgnoreCase))
        {
            options.OutputClassName = true;
        }

        // OutputClassName
        var outputClassName = provider.GlobalOptions.GetValue<bool?>("CommonCodeGeneratorToStringOutputClassName");
        if (outputClassName.HasValue)
        {
            options.OutputClassName = outputClassName.Value;
        }

        // NullLiteral
        var nullLiteral = provider.GlobalOptions.GetValue<string?>("CommonCodeGeneratorToStringNullLiteral");
        if (!String.IsNullOrEmpty(nullLiteral))
        {
            options.NullLiteral = nullLiteral;
        }

        return options;
    }

    private static bool IsTypeSyntax(SyntaxNode node) =>
        node is ClassDeclarationSyntax;

    private static Result<TypeModel> GetTypeModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (ClassDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol symbol)
        {
            return Results.Error<TypeModel>(null);
        }

        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var properties = new List<PropertyModel>();
        var currentSymbol = symbol;
        while (currentSymbol != null)
        {
            properties.AddRange(
                currentSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(x => !x.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == IgnoreAttributeName))
                    .Select(GetPropertyModel));
            currentSymbol = currentSymbol.BaseType;
        }

        return Results.Success(new TypeModel(
            ns,
            symbol.GetClassName(),
            symbol.IsValueType,
            new EquatableArray<PropertyModel>(properties.ToArray())));
    }

    private static PropertyModel GetPropertyModel(IPropertySymbol symbol)
    {
        var (hasElements, isNullAssignable) = GetPropertyType(symbol.Type);
        return new PropertyModel(
            symbol.Name,
            symbol.Type.ToDisplayString(),
            hasElements,
            isNullAssignable);
    }

    private static (bool HasElements, bool IsNullAssignable) GetPropertyType(ITypeSymbol typeSymbol)
    {
        if (!typeSymbol.SpecialType.Equals(SpecialType.System_String))
        {
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                var elementType = arrayTypeSymbol.ElementType;
                return (true, elementType.IsReferenceType || elementType.IsGenericType());
            }

            foreach (var @interface in typeSymbol.AllInterfaces)
            {
                if (@interface.IsGenericType && (@interface.ConstructedFrom.ToDisplayString() == GenericEnumerableName))
                {
                    var elementType = @interface.TypeArguments[0];
                    return (true, elementType.IsReferenceType || elementType.IsGenericType());
                }
            }
        }

        return (false, typeSymbol.IsReferenceType || typeSymbol.IsGenericType());
    }
    // ------------------------------------------------------------
    // Builder
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, GeneratorOptions options, ImmutableArray<Result<TypeModel>> types)
    {
        foreach (var info in types.SelectPart(static x => x.Error))
        {
            context.ReportDiagnostic(info);
        }

        var builder = new SourceBuilder();
        foreach (var type in types.SelectPart(static x => x.Value))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            builder.Clear();
            BuildSource(builder, options, type);

            var filename = MakeFilename(type.Namespace, type.ClassName);
            var source = builder.ToString();
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void BuildSource(SourceBuilder builder, GeneratorOptions options, TypeModel type)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(type.Namespace))
        {
            builder.Namespace(type.Namespace);
            builder.NewLine();
        }

        // class
        builder
            .Indent()
            .Append("partial ")
            .Append(type.IsValueType ? "struct " : "class ")
            .Append(type.ClassName)
            .NewLine();
        builder.BeginScope();

        // Method
        builder
            .Indent()
            .Append("public override string ToString()")
            .NewLine();
        builder.BeginScope();

        builder
            .Indent()
            .Append("var handler = new global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler(0, 0, default, stackalloc char[256]);")
            .NewLine();
        if (options.OutputClassName)
        {
            builder
                .Indent()
                .Append("handler.AppendLiteral(\"")
                .Append(type.ClassName)
                .Append(" \");")
                .NewLine();
        }
        builder
            .Indent()
            .Append("handler.AppendLiteral(\"{ \");")
            .NewLine();

        var firstProperty = true;
        foreach (var property in type.Properties.ToArray())
        {
            if (firstProperty)
            {
                firstProperty = false;
            }
            else
            {
                builder
                    .Indent()
                    .Append("handler.AppendLiteral(\", \");")
                    .NewLine();
            }

            builder.Indent()
                .Append("handler.AppendLiteral(\"")
                .Append(property.Name)
                .Append(" = \");")
                .NewLine();

            if (property.HasElements)
            {
                builder
                    .Indent()
                    .Append("if (this.")
                    .Append(property.Name)
                    .Append(" is not null)")
                    .NewLine();
                builder.BeginScope();

                BuildAppendLiteral(builder, "[");

                BuildAppendJoinedLiteral(
                    builder,
                    property.Name,
                    property.IsNullAssignable
                        ? !String.IsNullOrEmpty(options.NullLiteral)
                            ? $"static x => x?.ToString() ?? \"{options.NullLiteral}\""
                            : "static x => x?.ToString()"
                        : "static x => x.ToString()");

                BuildAppendLiteral(builder, "]");

                builder.EndScope();

                if (!String.IsNullOrEmpty(options.NullLiteral))
                {
                    builder
                        .Indent()
                        .Append("else")
                        .NewLine();
                    builder.BeginScope();

                    BuildAppendLiteral(builder, options.NullLiteral!);

                    builder.EndScope();
                }
            }
            else
            {
                if (property.IsNullAssignable)
                {
                    if (!String.IsNullOrEmpty(options.NullLiteral))
                    {
                        builder
                            .Indent()
                            .Append("if (this.")
                            .Append(property.Name)
                            .Append(" is not null)")
                            .NewLine();
                        builder.BeginScope();

                        BuildAppendProperty(builder, property.Name);

                        builder.EndScope();
                        builder
                            .Indent()
                            .Append("else")
                            .NewLine();
                        builder.BeginScope();

                        BuildAppendLiteral(builder, options.NullLiteral!);

                        builder.EndScope();
                    }
                    else
                    {
                        BuildAppendProperty(builder, property.Name);
                    }
                }
                else
                {
                    BuildAppendProperty(builder, property.Name);
                }
            }
        }

        builder
            .Indent()
            .Append("handler.AppendLiteral(\" }\");")
            .NewLine();
        builder
            .Indent()
            .Append("return handler.ToStringAndClear();")
            .NewLine();

        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildAppendProperty(SourceBuilder builder, string name)
    {
        builder
            .Indent()
            .Append("handler.AppendFormatted(")
            .Append("this.")
            .Append(name)
            .Append(");")
            .NewLine();
    }

    private static void BuildAppendLiteral(SourceBuilder builder, string literal)
    {
        builder
            .Indent()
            .Append("handler.AppendLiteral(\"")
            .Append(literal)
            .Append("\");")
            .NewLine();
    }

    private static void BuildAppendJoinedLiteral(SourceBuilder builder, string name, string expression)
    {
        builder
            .Indent()
            .Append("handler.AppendLiteral(String.Join(\", \", System.Linq.Enumerable.Select(this.")
            .Append(name)
            .Append(", ")
            .Append(expression)
            .Append(")));")
            .NewLine();
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
