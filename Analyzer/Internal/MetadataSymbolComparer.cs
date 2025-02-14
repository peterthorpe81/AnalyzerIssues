namespace Analyzer.Internal
{
    internal class MetadataSymbolComparer : IEqualityComparer<ISymbol?>
    {
        public bool Equals(ISymbol? x, ISymbol? y)
        {
            if (x is null || y is null)
                return false;

            return x.MetadataName.Equals(y.MetadataName);
        }

        public int GetHashCode(ISymbol? obj)
        {
            return base.GetHashCode();
        }
    }
}
