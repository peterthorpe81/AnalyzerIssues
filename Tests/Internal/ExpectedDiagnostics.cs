using System.Collections.Immutable;
using Analyzer;
using Microsoft.CodeAnalysis;

namespace Tests.Internal
{
#nullable enable
    internal class ExpectedDiagnostics
    {
        IOrderedEnumerable<Diagnostic> _diagnostics;
        IOrderedEnumerable<ExpectedDiagnostic> _expectedDiagnostics;

        internal ExpectedDiagnostics(IEnumerable<Diagnostic> diagnostics, IEnumerable<ExpectedDiagnostic> expectedDiagnostics)
        {
            _diagnostics = diagnostics
                    .OrderBy(x => x.Location.GetLineSpan().Path)
                    .ThenBy(x => x.Location.GetLineSpan().StartLinePosition.Line)
                    .ThenBy(x => x.Location.GetLineSpan().StartLinePosition.Character);

            _expectedDiagnostics = expectedDiagnostics
                    .OrderBy(x => x.Position.Path)
                    .ThenBy(x => x.Position.StartLinePosition.Line)
                    .ThenBy(x => x.Position.StartLinePosition.Character);
        }

        internal void Compare()
        {
            Assert.That(_diagnostics.Count() == _expectedDiagnostics.Count(),
                $"{_diagnostics.Count()} diagnostics expected {_expectedDiagnostics.Count()}\n{StringifyDiagnostics()}");

            for (var i = 0; i < _diagnostics.Count(); i++)
            {
                Test(i);
            }
        }

        private string StringifyDiagnostics()
        {
            var diagnosticsString = "Diagnostics:\n";

            foreach (var diag in _diagnostics)
            {
                diagnosticsString += $"{diag.Location.GetLineSpan().Path} {diag.Location.GetLineSpan().StartLinePosition} {diag.Location.GetLineSpan().EndLinePosition} {diag.GetMessage()}\n";
            }
            diagnosticsString += "\nExpected Diagnostics:\n";
            foreach (var diag in _expectedDiagnostics)
            {
                diagnosticsString += $"{diag.Position.Path} {diag.Position.StartLinePosition} {diag.Position.EndLinePosition} {diag.Message}\n";
            }

            return diagnosticsString;
        }

        private void Test(int index)
        {
            var diagnostic = _diagnostics.ElementAtOrDefault(index);
            var expectedDiagnostic = _expectedDiagnostics.ElementAtOrDefault(index);

            Assert.That(diagnostic is not null, $"Diagnostic Doesn't Exist at Index {index}\n{ StringifyDiagnostics()}");
            Assert.That(expectedDiagnostic is not null, $"Expected Diagnostic Doesn't Exist at Index {index}\n{ StringifyDiagnostics()}");

            if (diagnostic is null || expectedDiagnostic is null)
                return;

            Assert.That(diagnostic.GetMessage().StartsWith(expectedDiagnostic.Message),
                $"Diagnostic Message:\n{diagnostic.GetMessage()}\nExpected To Start With:\n {expectedDiagnostic.Message}\n{StringifyDiagnostics()}");

            Assert.That(diagnostic.Location.GetLineSpan().Path == expectedDiagnostic.Position.Path,
                $"Path:{diagnostic.Location.GetLineSpan().Path} Expected: {expectedDiagnostic.Position.Path}\n{StringifyDiagnostics()}");
        }
    }
#nullable restore
}
