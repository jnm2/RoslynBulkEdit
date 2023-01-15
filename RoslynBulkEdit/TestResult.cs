namespace RoslynBulkEdit;

public sealed record TestResult(
    string FullyQualifiedTestName,
    string Outcome,
    string? Message,
    string? StackTrace,
    string? Output);
