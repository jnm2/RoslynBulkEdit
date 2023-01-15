using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RoslynBulkEdit.Tests;

public static class ParserTestOperationsTests
{
    [Test]
    public static void Update_test_method_name()
    {
        var text = SourceText.From("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void TestA()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);

        ParserTestOperations.UpdateTestMethodName(text, GetTestCase(text, "TestA"), "New_name").ToString().ShouldBe("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void New_name()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);
    }

    [Test]
    public static void Update_simple_syntax()
    {
        var text = SourceText.From("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void TestA()
                    {
                        UsingTree("M();");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);

        ParserTestOperations.UpdateTestSyntax(Node(text), GetTestCase(text, "TestA"), "_ = 42;").GetText().ToString().ShouldBe("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void TestA()
                    {
                        UsingTree("_ = 42;");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);
    }

    [Test]
    public static void Apply_node_assertion_update()
    {
        var text = SourceText.From("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void TestA()
                    {
                        UsingTree("_ = 42;");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);

        ParserTestOperations.ApplyTestResult(
            TextAndRoot.WithoutRoot(text),
            GetTestCase(text, "TestA"),
            new TestResult(
                "Microsoft.CodeAnalysis.CSharp.UnitTests.SomeTests.TestA",
                "Failed",
                "Some message",
                "at Microsoft.CodeAnalysis.CSharp.UnitTests.ParsingTests.N(",
                """
                N(SyntaxKind.CompilationUnit);
                {
                N(SyntaxKind.GlobalStatement);
                {
                N(SyntaxKind.ExpressionStatement);
                {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                N(SyntaxKind.IdentifierName);
                {
                N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                N(SyntaxKind.NumericLiteralToken, "42");
                }
                }
                N(SyntaxKind.SemicolonToken);
                }
                }
                N(SyntaxKind.EndOfFileToken);
                }
                EOF();
                """)).Text.ToString().ShouldBe("""
            // Some license header

            #nullable disable

            using Xunit;
            using Xunit.Abstractions;

            namespace Microsoft.CodeAnalysis.CSharp.UnitTests
            {
                public class SomeTests : ParsingTests
                {
                    public SomeTests(ITestOutputHelper output) : base(output) { }

                    [Fact]
                    public void TestA()
                    {
                        UsingTree("_ = 42;");

                        N(SyntaxKind.CompilationUnit);
                        {
                            N(SyntaxKind.GlobalStatement);
                            {
                                N(SyntaxKind.ExpressionStatement);
                                {
                                    N(SyntaxKind.SimpleAssignmentExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "_");
                                        }
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "42");
                                        }
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                            }
                            N(SyntaxKind.EndOfFileToken);
                        }
                        EOF();
                    }

                    [Fact]
                    public void TestB()
                    {
                        UsingTree("");

                        N(SyntaxKind.CompilationUnit);
                        EOF();
                    }
                }
            }
            """);
    }

    private static TestCase GetTestCase(SourceText text, string methodName)
    {
        return ParserTestOperations.FindTestCases(text, "SomeFile.cs")
            .Single(testCase => testCase.MethodName == methodName);
    }

    private static SyntaxNode Node(SourceText text)
    {
        return CSharpSyntaxTree.ParseText(text).GetRoot();
    }
}
