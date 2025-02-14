using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MudComponentUnknownParametersAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MUD0002";
        public const string ClassNamePropertyKey = "ClassName";

        // You can change these strings in the Resources.resx file. If you do not want your anacomponentTypelyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.MUD0002Title), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _attributeMessageFormat = new LocalizableResourceString(nameof(Resources.MUD0002MessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.MUD0002Description), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableResourceString _url = new(nameof(Resources.MUD0002Url), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Attributes/Parameters";
        public const string DebugAnalyzerProperty = "build_property.DebugAnalyzer";
        public const string EnableAnalyzer = "build_property.EnableAnalyzer";

        public static readonly DiagnosticDescriptor AttributeDescriptor = new(DiagnosticId, _title, _attributeMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description, helpLinkUri: _url.ToString());

        private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = new[] { AttributeDescriptor }.ToImmutableArray();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            
            context.RegisterCompilationStartAction(ctx =>
            {
                var global = ctx.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

                if (global.TryGetValue(DebugAnalyzerProperty, out var debugValue) &&
                    bool.TryParse(debugValue, out var shouldDebug) && shouldDebug)
                {
                    Debugger.Launch();
                }

                if (global.TryGetValue(EnableAnalyzer, out var enableValue) &&
                    bool.TryParse(enableValue, out var enabled) && !enabled)
                {
                    return;
                }

                var analyzerContext = new AnalyzerContext(ctx.Compilation);                
                if (analyzerContext.IsValid)
                {
                    ctx.RegisterOperationAction(analyzerContext.AnalyzeBlockOptions, OperationKind.Block);
                }
            });
        }

        private sealed class AnalyzerContext
        {
            private readonly IEqualityComparer<ISymbol?> _symbolComparer = new MetadataSymbolComparer();
            private readonly ConcurrentDictionary<ITypeSymbol, ComponentDescriptor> _componentDescriptors = new(SymbolEqualityComparer.Default);
            private readonly INamedTypeSymbol? _componentBaseSymbol;
            private readonly INamedTypeSymbol? _parameterSymbol;
            private readonly INamedTypeSymbol? _renderTreeBuilderSymbol;


            public AnalyzerContext(Compilation compilation)
            {
                _componentBaseSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");
                _parameterSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
                _renderTreeBuilderSymbol = compilation.GetBestTypeByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder");
            }

            public bool IsValid => _componentBaseSymbol is not null && _parameterSymbol is not null && _renderTreeBuilderSymbol is not null;

            public void AnalyzeBlockOptions(OperationAnalysisContext context)
            {
                try
                {
                    var classSymbol = context.Operation.GetClassSymbol(context);
                    if (classSymbol is not null && classSymbol.IsOrInheritFrom(_componentBaseSymbol, _symbolComparer))
                        TraverseTree(context, (IBlockOperation)context.Operation, classSymbol.ToDisplayString());
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            public void TraverseTree(OperationAnalysisContext context, IBlockOperation operations, string className)
            {
                ITypeSymbol? currentComponent = null;
                ComponentDescriptor? currentComponentDescriptor = null;

                foreach (var operation in operations.Operations)
                {
                    if (operation is IExpressionStatementOperation expressionStatement)
                    {
                        if (expressionStatement.Operation is IInvocationOperation invocation)
                        {
                            var targetMethod = invocation.TargetMethod;

                            if (targetMethod.ContainingType.IsEqualTo(_renderTreeBuilderSymbol))
                            {
                                if (string.Equals(targetMethod.Name, "OpenComponent", StringComparison.Ordinal) && targetMethod.TypeArguments.Length == 1)
                                {
                                    var componentType = targetMethod.TypeArguments[0];
                                    if (componentType.IsOrInheritFrom(_componentBaseSymbol))
                                    {
                                        currentComponent = componentType;
                                        currentComponentDescriptor = _componentDescriptors.GetOrAdd(currentComponent, ComponentDescriptor.GetComponentDescriptor(componentType, _parameterSymbol));
                                    }
                                }
                                else if (string.Equals(targetMethod.Name, "CloseComponent", StringComparison.Ordinal))
                                {
                                    currentComponent = null;
                                    currentComponentDescriptor = null;
                                }
                                else if (currentComponent is not null && targetMethod.Name is "AddAttribute" or "AddComponentParameter")
                                {
                                    if (targetMethod.Parameters.Length >= 2 && targetMethod.Parameters[1].Type.IsString())
                                    {
                                        var aName = invocation.Arguments[1].Value.ConstantValue;

                                        if (aName.HasValue && aName.Value is string attributeName)
                                            ValidateAttribute(context, invocation, currentComponentDescriptor, attributeName, className);
                                    }
                                }
                            }
                            else if (string.Equals(targetMethod.ContainingType.MetadataName, "TypeInference", StringComparison.Ordinal))
                            {
                                var methods = context.FilterTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                                var method = methods?.Where(x => x.Identifier.ValueText == targetMethod.MetadataName).SingleOrDefault();

                                if (method is not null)
                                {
                                    var op = context.Compilation.GetSemanticModel(method.SyntaxTree).GetOperation(method);
                                    if (op is not null)
                                    {
                                        var blockOperation = op.ChildOperations.OfType<IBlockOperation>().Single();
                                        TraverseTree(context, blockOperation, className);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void ValidateAttribute(OperationAnalysisContext context, IInvocationOperation invocation,
                ComponentDescriptor? componentDescriptor, string attributeName, string className)
            {
                //check for existence of parameter (case insensitive)
                if (componentDescriptor is null || componentDescriptor.Parameters.Contains(attributeName))
                    return;

                if (!char.IsLower(attributeName, 0))
                    Report(AttributeDescriptor, context, invocation, attributeName, componentDescriptor, className);
            }

            private void Report(DiagnosticDescriptor diagnosticDescriptor, OperationAnalysisContext context, IInvocationOperation invocation,
                string attributeName, ComponentDescriptor componentDescriptor, string className)
            {
                var location = invocation.Syntax.GetLocation();
                var mappedLocation = location;

                var razorPath = invocation.GetRazorFilePath();
                if (razorPath is not null)
                {
                    var newLineSpan = new LinePositionSpan(new LinePosition(), new LinePosition());
                    mappedLocation = Location.Create(razorPath, new TextSpan(0, 0), newLineSpan);
                }

                context.ReportDiagnostic(
                 Diagnostic.Create(
                    descriptor: diagnosticDescriptor,
                    location: mappedLocation,
                    additionalLocations: [location],
                    properties: ImmutableDictionary.CreateRange(new[] { new KeyValuePair<string, string?>(ClassNamePropertyKey, className) }),
                    messageArgs: [attributeName, componentDescriptor.TagName, "LowerCase", location.GetLineSpan().Span]));
            }

        }
    }
}
