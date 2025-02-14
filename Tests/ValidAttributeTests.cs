using System.Collections.Immutable;
using Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Tests.Internal;

namespace Tests
{
#nullable enable
    [TestFixture]
    public class ValidAttributeTests
    {
        string ProjectPath { get; set; }

        DiagnosticAnalyzer Analyzer { get; set; } = new MudComponentUnknownParametersAnalyzer();

        IEnumerable<Diagnostic> TestComponentsDiagnostics { get; set; } = null!;
        //ProjectCompilation TestComponentsCompilation { get; set; } = null!;

        IEnumerable<Diagnostic> PreGeneratedTestComponentsDiagnostics { get; set; } = null!;
        //ProjectCompilation PreGeneratedCompilation { get; set; } = null!;

        private ExpectedDiagnostic InputTextAttribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(TestComponents.ProjectLocator.FilePath($"BuiltInComponent.razor"), new LinePosition(33, 12), new LinePosition(33, 62)),
            "Illegal Attribute 'Issue1' on 'InputText'");

        private ExpectedDiagnostic Component1Attribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(TestComponents.ProjectLocator.FilePath($"CustomComponent.razor"), new LinePosition(33, 12), new LinePosition(33, 61)),
            "Illegal Attribute 'Issue2' on 'Component1'");

        private ExpectedDiagnostic SubComponentAttribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(TestComponents.ProjectLocator.FilePath($"SubComponent.razor"), new LinePosition(35, 16), new LinePosition(35, 66)),
            "Illegal Attribute 'Issue3' on 'Component1'");


        private ExpectedDiagnostic PreGeneratedInputTextAttribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(PreGeneratedTestComponents.ProjectLocator.FilePath($"BuiltInComponent.razor"), new LinePosition(33, 12), new LinePosition(33, 62)),
            "Illegal Attribute 'Issue1' on 'InputText'");

        private ExpectedDiagnostic PreGeneratedComponent1Attribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(PreGeneratedTestComponents.ProjectLocator.FilePath($"CustomComponent.razor"), new LinePosition(33, 12), new LinePosition(33, 61)),
            "Illegal Attribute 'Issue2' on 'Component1'");

        private ExpectedDiagnostic PreGeneratedSubComponentAttribute { get; set; } = new ExpectedDiagnostic(MudComponentUnknownParametersAnalyzer.AttributeDescriptor,
            new FileLinePositionSpan(PreGeneratedTestComponents.ProjectLocator.FilePath($"SubComponent.razor"), new LinePosition(35, 16), new LinePosition(35, 66)),
            "Illegal Attribute 'Issue3' on 'Component1'");


        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {     
            using (var workspace = MSBuildWorkspace.Create())
            {
                /*var solution = await workspace.OpenSolutionAsync(".\\..\\..\\..\\..\\TestComponents.sln");
                var project = solution.Projects.Where(x => x.FilePath == TestComponents.ProjectLocator.ProjectPath()).Single();*/
                var project = await workspace.OpenProjectAsync(TestComponents.ProjectLocator.ProjectPath());
                var compilation = await project.GetCompilationAsync();
                var compilationWithAnalyzers = compilation!.WithAnalyzers([Analyzer]);
                TestComponentsDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            }

            //PreGeneratedTestComponents.ProjectLocator.ProjectPath()
            using (var workspace = MSBuildWorkspace.Create())
            {
                /* var solution = await workspace.OpenSolutionAsync(".\\..\\..\\..\\..\\TestComponents.sln");
                var project = solution.Projects.Where(x => x.FilePath == PreGeneratedTestComponents.ProjectLocator.ProjectPath()).Single();*/
                var project = await workspace.OpenProjectAsync(PreGeneratedTestComponents.ProjectLocator.ProjectPath());               
                var compilation = await project.GetCompilationAsync();
                var compilationWithAnalyzers = compilation!.WithAnalyzers([Analyzer]);
                PreGeneratedTestComponentsDiagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
            }
        }

        [Test]
        public void BuildInComponent()
        {
            var diagnostics = TestComponentsDiagnostics.FilterToClass(typeof(TestComponents.BuiltInComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                InputTextAttribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }

        [Test]
        public void BuildInComponentSourcePreGenerated()
        {
            var diagnostics = PreGeneratedTestComponentsDiagnostics.FilterToClass(typeof(PreGeneratedTestComponents.BuiltInComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                PreGeneratedInputTextAttribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }

        [Test]
        public void CustomComponent()
        {
            var diagnostics = TestComponentsDiagnostics.FilterToClass(typeof(TestComponents.CustomComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                Component1Attribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }

        [Test]
        public void CustomComponentPreGenerated()
        {
            var diagnostics = PreGeneratedTestComponentsDiagnostics.FilterToClass(typeof(PreGeneratedTestComponents.CustomComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                PreGeneratedComponent1Attribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }

        [Test]
        public void CustomSubComponent()
        {
            var diagnostics = TestComponentsDiagnostics.FilterToClass(typeof(TestComponents.SubComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                SubComponentAttribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }

        [Test]
        public void CustomSubComponentPreGenerated()
        {
            var diagnostics = PreGeneratedTestComponentsDiagnostics.FilterToClass(typeof(PreGeneratedTestComponents.SubComponent).FullName);

            var expectedDiagnostics = new List<ExpectedDiagnostic>([
                PreGeneratedSubComponentAttribute
            ]);

            new ExpectedDiagnostics(diagnostics, expectedDiagnostics).Compare();
        }
    }
#nullable restore
}
