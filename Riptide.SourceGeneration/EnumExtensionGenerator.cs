using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;

namespace Riptide.SourceGeneration;

[Generator(LanguageNames.CSharp)]
public class EnumExtensionGenerator : IIncrementalGenerator {
    private const string AttributeFullName = $"RiptideEngine.Core.Attributes.EnumExtensionAttribute";
    private const string ExtensionClassNameProperty = "ExtensionClassName";

    private class GeneratePayload {
        public INamedTypeSymbol EnumSymbol = null!;
        public AttributeData AttributeData = null!;
        public bool IsFlag;
        public IEnumerable<IFieldSymbol> Fields = null!;
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(AttributeFullName, Filter, Transform);
        context.RegisterSourceOutput(provider, AddOutput);
    }

    private static bool Filter(SyntaxNode node, CancellationToken cancelToken) {
        return node is EnumDeclarationSyntax { AttributeLists.Count: > 0 };
    }

    private static GeneratePayload? Transform(GeneratorAttributeSyntaxContext context, CancellationToken cancelToken) {
        if (context.TargetSymbol is not INamedTypeSymbol enumSymbol) return null;

        foreach (var attributeData in enumSymbol.GetAttributes()) {
            if (attributeData.AttributeClass is not { } attrClass) continue;
            if (attrClass.Name is not "EnumExtension" and not "EnumExtensionAttribute") continue;
            if (attrClass.ToDisplayString() != AttributeFullName) continue;

            cancelToken.ThrowIfCancellationRequested();

            var isFlag = enumSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name is "Flags" or "FlagsAttribute") is { } flagAttribData && flagAttribData.AttributeClass!.ToDisplayString() == "System.FlagsAttribute";

            return new() {
                EnumSymbol = enumSymbol,
                AttributeData = context.Attributes[0],
                IsFlag = isFlag,
                Fields = enumSymbol.GetMembers().OfType<IFieldSymbol>(),
            };
        }

        return null;
    }

    private static void AddOutput(SourceProductionContext context, GeneratePayload? payload) {
        if (payload == null) return;

        context.CancellationToken.ThrowIfCancellationRequested();

        var sb = new StringBuilder(1024);
        int scope = 0;

        var enumSymbol = payload.EnumSymbol;

        var attrData = payload.AttributeData.NamedArguments.FirstOrDefault(x => x.Key == ExtensionClassNameProperty);
        if (attrData.Key == default || attrData.Value.Value is not { } extClassName) {
            extClassName = enumSymbol.Name + "Extensions";
        }

        sb.Append("using __E = global::").Append(enumSymbol.ToDisplayString()).AppendLine(";");
        if (enumSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns) {
            sb.Append("namespace ").Append(ns.ToDisplayString()).AppendLine(";");
        }

        {
            sb.Append('\t', scope).Append("public static partial class ").Append(extClassName).AppendLine(" {");
            scope++;
            {
                var enumCount = payload.Fields.Count();

                // Constants
                sb.Append('\t', scope).Append("public const int EnumCount = ").Append(enumCount).AppendLine(";");
                sb.Append('\t', scope).Append("public const int UniqueValueCount = ").Append(payload.Fields.Select(x => x.ConstantValue).Distinct().Count()).AppendLine(";");
                sb.Append('\t', scope).AppendLine();

                // Extension methods

                // IsDefined
                sb.Append('\t', scope).AppendLine("public static bool IsDefined(this __E value) {");
                scope++;
                {
                    switch (enumCount) {
                        case 0: sb.Append('\t', scope).AppendLine("return false;"); break;
                        case >= 1 and <= 4:
                            sb.Append('\t', scope).Append("return value is ");
                            sb.Append(string.Join(" or ", payload.Fields.Select(x => "__E." + x.Name))).AppendLine(";");
                            break;
                        default:
                            sb.Append('\t', scope).AppendLine("return value switch {");
                            scope++;
                            {
                                sb.Append('\t', scope);
                                sb.Append(string.Join(" or ", payload.Fields.Select(x => "__E." + x.Name)));
                                sb.AppendLine(" => true,");

                                sb.Append('\t', scope).AppendLine("_ => false,");
                            }
                            scope--;
                            sb.Append('\t', scope).AppendLine("};");
                            break;
                    }
                }
                scope--;
                sb.Append('\t', scope).AppendLine("}");

                // TryGet(string, out __E)
                sb.Append('\t', scope).AppendLine("public static bool TryGet(string value, out __E output) {");
                scope++;
                {
                    if (enumCount == 0) {
                        sb.Append('\t', scope).AppendLine("output = default; return false;");
                    } else {
                        sb.Append('\t', scope).AppendLine("if (string.IsNullOrEmpty(value)) { output = default; return false; }");

                        sb.Append('\t', scope).AppendLine("switch (value.Length) {");
                        scope++;
                        {
                            foreach (var lengthGroup in payload.Fields.GroupBy(x => x.Name.Length)) {
                                sb.Append('\t', scope).Append("case ").Append(lengthGroup.Key).Append(':').AppendLine();

                                scope++;
                                {
                                    var valueGroups = lengthGroup.GroupBy(x => x.ConstantValue);
                                    IEnumerable<IGrouping<object?, IFieldSymbol>> enumerable = valueGroups as IGrouping<object, IFieldSymbol>[] ?? valueGroups.ToArray();
                                    
                                    var valueGroupCount = enumerable.Count();

                                    if (valueGroupCount == 1) {
                                        var firstGroup = enumerable.First();

                                        sb.Append('\t', scope).Append("if (value is ").Append(string.Join(" or ", firstGroup.Select(x => '\"' + x.Name + '\"'))).Append(") { output = __E.").Append(firstGroup.First().Name).AppendLine("; return true; }");
                                    } else {
                                        sb.Append('\t', scope).AppendLine("switch (value) {");
                                        scope++;
                                        {
                                            foreach (var valueGroup in enumerable) {
                                                sb.Append('\t', scope).Append("case ").Append(string.Join(" or ", valueGroup.Select(x => '\"' + x.Name + '\"'))).Append(": output = __E.").Append(valueGroup.First().Name).AppendLine("; return true;");
                                            }
                                        }
                                        scope--;
                                        sb.Append('\t', scope).AppendLine("}");
                                    }

                                    sb.Append('\t', scope).AppendLine("break;");
                                }
                                scope--;
                            }
                        }
                        scope--;
                        sb.Append('\t', scope).AppendLine("}");
                        sb.Append('\t', scope).AppendLine("output = default; return false;");
                    }
                }
                scope--;
                sb.Append('\t', scope).AppendLine("}");

                // ToFastString(__E)
                if (!payload.IsFlag) {
                    sb.Append('\t', scope).AppendLine("public static string ToFastString(this __E value) {");
                    scope++;
                    {
                        if (enumCount != 0) {
                            sb.Append('\t', scope).AppendLine("switch (value) {");

                            var hs = new HashSet<object>();
                            foreach (var field in payload.Fields) {
                                if (hs.Contains(field.ConstantValue!)) continue;

                                scope++;
                                {
                                    sb.Append('\t', scope).Append("case __E.").Append(field.Name).Append(": return \"").Append(field.Name).AppendLine("\";");
                                }
                                scope--;

                                hs.Add(field.ConstantValue!);
                            }
                            sb.Append('\t', scope).AppendLine("}").AppendLine();
                        }

                        sb.Append('\t', scope).AppendLine("return string.Empty;");
                    }
                    scope--;
                    sb.Append('\t', scope).AppendLine("}");
                }
            }
            scope--;
            sb.Append('\t', scope).Append('}').AppendLine();
        }

        context.AddSource(extClassName + ".generated.cs", sb.ToString());
    }
}