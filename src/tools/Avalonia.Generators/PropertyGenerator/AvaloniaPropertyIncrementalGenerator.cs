using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avalonia.Generators.PropertyGenerator;

/// <summary>
/// Generates AvaloniaProperty registrations for partial members with [StyledProperty], [DirectProperty] or [AttachedProperty].
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AvaloniaPropertyIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var emitMode = context.ParseOptionsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) =>
            {
                var (parseOptions, configOptions) = pair;
                var genOptions = new GeneratorOptions(configOptions.GlobalOptions);

                if (!genOptions.AvaloniaPropertyGeneratorIsEnabled)
                {
                    return LanguageEmitMode.Skip;
                }

                return GeneratedPropertyShape.IsCSharp14OrLater(parseOptions) ? LanguageEmitMode.FieldKeyword
                    : GeneratedPropertyShape.IsCSharp13OrLater(parseOptions) ? LanguageEmitMode.BackingField
                    : LanguageEmitMode.Skip;
            })
            .WithTrackingName(TrackingNames.LanguageVersionOk);

        var styled = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GeneratedPropertyShape.StyledAttributeMetadataName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax,
                transform: static (context, _) => PropertyGenModelBuilder.Build(context, GeneratedPropertyKind.Styled))
            .Where(static model => model is not null)
            .WithTrackingName(TrackingNames.StyledModels);

        var direct = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GeneratedPropertyShape.DirectAttributeMetadataName,
                predicate: static (node, _) => node is PropertyDeclarationSyntax,
                transform: static (context, _) => PropertyGenModelBuilder.Build(context, GeneratedPropertyKind.Direct))
            .Where(static model => model is not null)
            .WithTrackingName(TrackingNames.DirectModels);

        var attached = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GeneratedPropertyShape.AttachedAttributeMetadataName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (context, _) => PropertyGenModelBuilder.Build(context, GeneratedPropertyKind.Attached))
            .Where(static model => model is not null)
            .WithTrackingName(TrackingNames.AttachedModels);

        // One output file per containing type, so grouping happens before RegisterSourceOutput.
        var grouped = styled.Collect()
            .Combine(direct.Collect())
            .Combine(attached.Collect())
            .SelectMany(static (models, _) => GroupByContainingType(models.Left.Left, models.Left.Right, models.Right))
            .WithTrackingName(TrackingNames.GroupedTypes);

        context.RegisterSourceOutput(grouped.Combine(emitMode), static (context, pair) =>
        {
            var (typeModel, emitMode) = pair;
            if (emitMode == LanguageEmitMode.Skip)
            {
                // Partial properties require C# 13; below that the analyzer reports AVP2007.
                return;
            }

            var source = PropertyGenEmitter.Emit(typeModel, useFieldKeyword: emitMode == LanguageEmitMode.FieldKeyword);
            context.AddSource(typeModel.Type.HintName, SourceText.From(source, Encoding.UTF8));
        });
    }

    private enum LanguageEmitMode
    {
        Skip,
        BackingField,
        FieldKeyword,
    }

    private static ImmutableArray<PropertyGenTypeModel> GroupByContainingType(
        ImmutableArray<PropertyGenModel?> styled,
        ImmutableArray<PropertyGenModel?> direct,
        ImmutableArray<PropertyGenModel?> attached)
    {
        var byType = new Dictionary<string, List<PropertyGenModel>>();
        var order = new List<string>();

        foreach (var models in new[] { styled, direct, attached })
        {
            foreach (var model in models)
            {
                if (model is null)
                {
                    continue;
                }

                if (!byType.TryGetValue(model.ContainingType.HintName, out var group))
                {
                    byType[model.ContainingType.HintName] = group = [];
                    order.Add(model.ContainingType.HintName);
                }

                group.Add(model);
            }
        }

        var result = ImmutableArray.CreateBuilder<PropertyGenTypeModel>(order.Count);
        foreach (var hintName in order)
        {
            var group = byType[hintName];
            result.Add(new PropertyGenTypeModel(group[0].ContainingType, new(group)));
        }

        return result.MoveToImmutable();
    }
}
