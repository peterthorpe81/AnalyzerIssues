using Microsoft.CodeAnalysis;

namespace Tests.Internal
{
#nullable enable
    internal class ExpectedDiagnostic
    {
        internal ExpectedDiagnostic(DiagnosticDescriptor descriptor, FileLinePositionSpan position, string message)
        {
            Descriptor = descriptor;
            Position = position;
            Message = message;
        }

        internal DiagnosticDescriptor Descriptor { get; private set; }
        internal FileLinePositionSpan Position { get; private set; }
        internal string Message { get; private set; }
    }
#nullable restore
}
