using System.IO;

namespace RoslynBulkEdit;

internal static class FileUtils
{
    public static string? GetFirstLineContaining(string filePath, string textWithinLine)
    {
        using var reader = new StreamReader(filePath);

        while (reader.ReadLine() is { } line)
        {
            if (line.Contains(textWithinLine, StringComparison.Ordinal))
                return line;
        }

        return null;
    }

    public static string? FindContainingCsprojFolder(string baseClassPath)
    {
        var currentPath = baseClassPath;

        while (true)
        {
            currentPath = Path.GetDirectoryName(currentPath);
            if (currentPath is null) break;

            if (Directory.EnumerateFiles(currentPath, "*.csproj").Any())
                return currentPath;
        }

        return null;
    }
}
