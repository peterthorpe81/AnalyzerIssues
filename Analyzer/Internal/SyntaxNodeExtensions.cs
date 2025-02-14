namespace Analyzer.Internal
{
    internal static class SyntaxNodeExtensions
    {
        public static ClassDeclarationSyntax? FindClass(this SyntaxNode? node)
        {
            while (node is not null && node is not ClassDeclarationSyntax)
            {
                node = node.Parent;
            }

            return (ClassDeclarationSyntax?)node;
        }
    }
}
