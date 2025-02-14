using System;
using System.Collections.Generic;
using System.Linq;
using Analyzer;
using Microsoft.CodeAnalysis;

namespace Tests.Internal
{
#nullable enable
    internal static class DiagnosticHelper
    {
        internal static List<Diagnostic> FilterToClass(this IEnumerable<Diagnostic> diagnostics, string? className)
        {
            var results = new List<Diagnostic>();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(MudComponentUnknownParametersAnalyzer.ClassNamePropertyKey, out var cn)
                    && string.Equals(cn, className))
                    results.Add(diagnostic);
            }

            return results;
        }
    }
#nullable restore
}
