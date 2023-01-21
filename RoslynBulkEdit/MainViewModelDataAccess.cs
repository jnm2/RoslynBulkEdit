using System.Collections.Immutable;
using System.IO;
using System.IO.Enumeration;
using Microsoft.CodeAnalysis.Text;

namespace RoslynBulkEdit;

public sealed class MainViewModelDataAccess
{
    public ImmutableArray<string> DiscoverSolutions()
    {
        return new[] { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
            .Concat(
                from drive in DriveInfo.GetDrives()
                where drive.IsReady
                select drive.Name)
            .AsParallel()
            .SelectMany(rootFolder => Directory.GetFiles(
                rootFolder,
                "Roslyn.sln",
                new EnumerationOptions { RecurseSubdirectories = true, MaxRecursionDepth = 4 }))
            .Select(slnPath => Path.GetDirectoryName(slnPath)!)
            .ToImmutableArray();
    }

    public ImmutableArray<(string Path, DateTime LastModified)> DiscoverCSharpParsingTests(string solutionFolder)
    {
        return (
            from baseClassPath in Directory.GetFiles(solutionFolder, "ParsingTests.cs", SearchOption.AllDirectories).AsParallel()
            let csprojFolder = FileUtils.FindContainingCsprojFolder(baseClassPath)
            where csprojFolder is not null
            from csFile in new FileSystemEnumerable<(string Path, DateTime LastModified)>(
                csprojFolder,
                (ref FileSystemEntry entry) => (entry.ToFullPath(), entry.LastWriteTimeUtc.DateTime),
                new EnumerationOptions { RecurseSubdirectories = true })
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => entry.FileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase),
            }
            where FileUtils.GetFirstLineContaining(csFile.Path, " class ")?.Contains(": ParsingTests") == true
            select csFile).ToImmutableArray();
    }

    public ImmutableArray<TestCase> LoadTestCases(string filePath)
    {
        SourceText text;
        using (var stream = File.OpenRead(filePath))
            text = SourceText.From(stream);

        return ParserTestOperations.FindTestCases(text, filePath);
    }

    public async Task UpdateTestMethodNameAsync(TestCase testCase, string newMethodName)
    {
        if (testCase.MethodName == newMethodName)
            return;

        await using var stream = new FileStream(testCase.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        var newText = ParserTestOperations.UpdateTestMethodName(SourceText.From(stream), testCase, newMethodName);

        await ReplaceFileContents(stream, newText);
    }

    public async Task UpdateTestSyntaxAsync(TestCase testCase, string newSyntax)
    {
        if (testCase.TestSyntax == newSyntax)
            return;

        await using var stream = new FileStream(testCase.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        var current = TextAndRoot.WithoutRoot(SourceText.From(stream));
        var updated = TextAndRoot.WithoutText(ParserTestOperations.UpdateTestSyntax(current.Root, testCase, newSyntax));

        do
        {
            current = updated;
            await ReplaceFileContents(stream, current.Text);
            var result = await TestDriver.Run(testCase);

            updated = ParserTestOperations.ApplyTestResult(current, testCase, result);
        }
        while (updated != current);
    }

    private static async Task ReplaceFileContents(FileStream stream, SourceText text)
    {
        stream.Position = 0;
        stream.SetLength(0);
        await using var writer = new StreamWriter(stream, leaveOpen: true);
        text.Write(writer);
    }
}
