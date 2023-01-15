using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace RoslynBulkEdit;

internal static class TestDriver
{
    public static async Task<TestResult> Run(TestCase testCase)
    {
        var csprojFolder = FileUtils.FindContainingCsprojFolder(testCase.FilePath)
            ?? throw new InvalidOperationException($"Could not find csproj folder for {testCase.FilePath}.");

        using var tempDirectory = new TempDirectory();

        var fullyQualifiedName = $"{testCase.FullyQualifiedClassName}.{testCase.MethodName}";

        using var process = new Process { StartInfo =
        {
            FileName = "dotnet",
            ArgumentList =
            {
                "test", csprojFolder,
                "--filter", "FullyQualifiedName=" + fullyQualifiedName,
                "--logger", "trx",
                "--results-directory", tempDirectory.Path,
            },
            WorkingDirectory = tempDirectory.Path,
            CreateNoWindow = true,
        } };

        process.Start();
        await process.WaitForExitAsync();

        foreach (var resultFile in Directory.GetFiles(tempDirectory.Path, "*.trx"))
        {
            await using var stream = File.OpenRead(resultFile);
            var result = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            var ns = (XNamespace)"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
            foreach (var element in result.Element(ns + "TestRun")!.Element(ns + "Results")!.Elements(ns + "UnitTestResult"))
            {
                if (element.Attribute("testName")?.Value != fullyQualifiedName)
                    continue;

                var outcome = element.Attribute("outcome")!.Value;
                var output = element.Element(ns + "Output");
                var stdOut = output?.Element(ns + "StdOut")?.Value;
                var errorInfo = output?.Element(ns + "ErrorInfo");
                var message = errorInfo?.Element(ns + "Message")?.Value;
                var stackTrace = errorInfo?.Element(ns + "StackTrace")?.Value;

                return new TestResult(fullyQualifiedName, outcome, message, stackTrace, stdOut);
            }
        }

        throw new InvalidOperationException("No test results found.");
    }
}
