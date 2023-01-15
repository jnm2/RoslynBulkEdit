using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RoslynBulkEdit;

public static class ParserTestOperations
{
    public static ImmutableArray<TestCase> FindTestCases(SourceText text, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(text);

        var testCases = ImmutableArray.CreateBuilder<TestCase>();

        foreach (var classDeclaration in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var fullyQualifiedClassName = SyntaxUtils.GetFullyQualifiedTypeName(classDeclaration);

            foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                if (method.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name is SimpleNameSyntax { Identifier.ValueText: "Fact" })
                    && FindUsingInvocation(method) is var (_, usingMethodName, literal))
                {
                    testCases.Add(new TestCase(
                        filePath,
                        text.Lines.GetLineFromPosition(method.Identifier.SpanStart).LineNumber + 1,
                        fullyQualifiedClassName,
                        MethodName: method.Identifier.ValueText,
                        usingMethodName switch
                        {
                            "UsingTree" => TestCaseType.UsingTree,
                            "UsingStatement" => TestCaseType.UsingStatement,
                            "UsingNode" => TestCaseType.UsingNode,
                            _ => TestCaseType.Unknown,
                        },
                        literal.Token.ValueText));
                }
            }
        }

        return testCases.ToImmutable();
    }

    public static SourceText UpdateTestMethodName(SourceText text, TestCase testCase, string newMethodName)
    {
        if (!SyntaxFacts.IsValidIdentifier(newMethodName)) throw new ArgumentException("The method name must be a valid C# identifier.", nameof(newMethodName));

        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        var method = GetMethodDeclaration(root, testCase);

        if (((ClassDeclarationSyntax)method.Parent!).Members.OfType<MethodDeclarationSyntax>()
            .Any(method => method.Identifier.ValueText == newMethodName))
        {
            throw new ArgumentException($"The class already contains a method with the name {newMethodName}.", nameof(newMethodName));
        }

        return text.WithChanges(new TextChange(method.Identifier.Span, newMethodName));
    }

    public static SyntaxNode UpdateTestSyntax(SyntaxNode root, TestCase testCase, string newSyntax)
    {
        var method = GetMethodDeclaration(root, testCase);
        var (_, _, literal) = FindUsingInvocation(method)!.Value;

        return root.ReplaceToken(literal.Token, SyntaxFactory.Literal(newSyntax).WithTriviaFrom(literal.Token));
    }

    public static SourceText ApplyTestResult(SourceText text, SyntaxNode root, TestCase testCase, TestResult result)
    {
        if (result.StackTrace is not null)
        {
            var trimmedStackTrace = result.StackTrace.AsSpan().TrimStart();
            if (trimmedStackTrace.StartsWith("at Microsoft.CodeAnalysis.DiagnosticExtensions.Verify(")
                && !string.IsNullOrWhiteSpace(result.Message))
            {
                var newDiagnosticAssertionSyntax = GetDiagnosticAssertionSyntax(result.Message);

                var method = GetMethodDeclaration(root, testCase);
                var (usingInvocation, _, _) = FindUsingInvocation(method)!.Value;
                if (usingInvocation.ArgumentList.Arguments.Count == 1)
                {
                    // Set up tests. Handle to and from zero diagnostics, including line handling.
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (trimmedStackTrace.StartsWith("at Microsoft.CodeAnalysis.CSharp.UnitTests.ParsingTests.N(")
                && !string.IsNullOrWhiteSpace(result.Output))
            {
                var method = GetMethodDeclaration(root, testCase);
                var nodeAssertionSpan = GetNodeAssertionSpan(method);
                var outerLevelIndentationStart = text.Lines.GetLineFromPosition(nodeAssertionSpan.Start).Start;
                var outerLevelIndentation = text.ToString(TextSpan.FromBounds(outerLevelIndentationStart, nodeAssertionSpan.Start));
                var newNodeAssertionSyntax = AddNodeAssertionIndentation(result.Output, outerLevelIndentation);

                return text.WithChanges(new TextChange(
                    TextSpan.FromBounds(outerLevelIndentationStart, nodeAssertionSpan.End),
                    newNodeAssertionSyntax));
            }
        }

        return text;

        static string? GetDiagnosticAssertionSyntax(string testResultMessage)
        {
            using var reader = new StringReader(testResultMessage);

            while (true)
            {
                if (reader.ReadLine() is not { } line)
                    return null;

                if (line == "Actual:") break;
            }

            var syntax = new StringBuilder();

            while (reader.ReadLine() is { } line)
            {
                if (line == "Diff:") return syntax.ToString();

                if (syntax.Length > 0) syntax.AppendLine();
                syntax.Append(line);
            }

            return null;
        }

        static string AddNodeAssertionIndentation(string nodeAssertionSyntax, string outerLevelIndentation)
        {
            using var reader = new StringReader(nodeAssertionSyntax);
            var builder = new StringBuilder();

            var indentationLevel = 0;
            while (reader.ReadLine() is { } line)
            {
                if (line == "}")
                    indentationLevel--;

                if (builder.Length > 0) builder.AppendLine();
                builder.Append(outerLevelIndentation).Append(' ', indentationLevel * 4).Append(line);

                if (line == "{")
                    indentationLevel++;
            }

            return builder.ToString();
        }

        static TextSpan GetNodeAssertionSpan(MethodDeclarationSyntax method)
        {
            var firstAssertionIndex = method.Body!.Statements.IndexOf(IsNodeAssertion);
            if (firstAssertionIndex == -1)
                throw new NotImplementedException("Node assertion statements were not found and should have existed.");

            var lastAssertionIndex = method.Body.Statements.LastIndexOf(IsNodeAssertion);

            for (var middleIndex = firstAssertionIndex + 1; middleIndex < lastAssertionIndex; middleIndex++)
            {
                if (!IsNodeAssertion(method.Body.Statements[middleIndex]))
                    throw new NotImplementedException("Node assertion statements were mixed with unrecognized statements.");
            }

            return TextSpan.FromBounds(
                method.Body.Statements[firstAssertionIndex].Span.Start,
                method.Body.Statements[lastAssertionIndex].Span.End);
        }

        static bool IsNodeAssertion(StatementSyntax statement)
        {
            return statement switch
            {
                ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "N" or "EOF" } } } => true,
                BlockSyntax block => block.Statements.All(IsNodeAssertion),
                _ => false,
            };
        }
    }

    private static MethodDeclarationSyntax GetMethodDeclaration(SyntaxNode root, TestCase testCase)
    {
        var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(classDeclaration => SyntaxUtils.GetFullyQualifiedTypeName(classDeclaration) == testCase.FullyQualifiedClassName);

        return classDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == testCase.MethodName);
    }

    private static (
        InvocationExpressionSyntax UsingInvocation,
        string UsingMethodName,
        LiteralExpressionSyntax Literal)? FindUsingInvocation(MethodDeclarationSyntax method)
    {
        if (method.Body is null) return null;

        foreach (var statement in method.Body.Statements.OfType<ExpressionStatementSyntax>())
        {
            if (statement.Expression is InvocationExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: { Length: > 5 } identifier },
                    ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax literal }, ..],
                } invocation
                && identifier.StartsWith("Using", StringComparison.Ordinal))
            {
                return (invocation, identifier, literal);
            }
        }

        return null;
    }
}
