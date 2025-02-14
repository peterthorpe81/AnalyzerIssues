using System.Runtime.CompilerServices;

namespace PreGeneratedTestComponents
{
    public static class ProjectLocator
    {
        private static string ThisDirectory([CallerFilePath] string callerFilePath = "")
        {
            return System.IO.Path.GetDirectoryName(callerFilePath)!;
        }

        public static string ProjectPath([CallerFilePath] string? callerFilePath = null)
        {
            return System.IO.Path.Combine(ThisDirectory(), "PreGeneratedTestComponents.csproj");
        }

        public static string FilePath(string path, [CallerFilePath] string? callerFilePath = null)
        {
            return System.IO.Path.Combine(ThisDirectory(), path);
        }
    }
}
