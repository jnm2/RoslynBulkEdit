namespace RoslynBulkEdit;

public sealed record TestCase(
    string FilePath,
    int LineNumber,
    string FullyQualifiedClassName,
    string MethodName,
    TestCaseType Type,
    string TestSyntax);
