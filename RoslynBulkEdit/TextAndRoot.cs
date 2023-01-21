using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace RoslynBulkEdit;

public sealed class TextAndRoot : IEquatable<TextAndRoot?>
{
    private SourceText? text;
    private SyntaxNode? root;

    private TextAndRoot(SourceText? text, SyntaxNode? root)
    {
        this.text = text;
        this.root = root;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SourceText Text => text ??= root!.SyntaxTree.GetText();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public SyntaxNode Root => root ??= CSharpSyntaxTree.ParseText(text!).GetRoot();

    public static TextAndRoot WithoutText(SyntaxNode root)
    {
        return new(text: null, root ?? throw new ArgumentNullException(nameof(root)));
    }

    public static TextAndRoot WithoutRoot(SourceText text)
    {
        return new(text ?? throw new ArgumentNullException(nameof(text)), root: null);
    }

    public static TextAndRoot WithMatching(SourceText text, SyntaxNode root)
    {
        return new(
            text ?? throw new ArgumentNullException(nameof(text)),
            root ?? throw new ArgumentNullException(nameof(root)));
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TextAndRoot);
    }

    public bool Equals(TextAndRoot? other)
    {
        return other is not null && (
            (text is null || other.text is null || text == other.text)
            && (root is null || other.root is null || root == other.root));
    }

    public override int GetHashCode()
    {
        // Not much you can do if WithoutRoot(A) == WithMatching(A, B) == WithoutText(B)
        return 0;
    }

    public TextAndRoot WithChanges(params TextChange[] changes)
    {
        return WithoutRoot(Text.WithChanges(changes));
    }

    public static bool operator ==(TextAndRoot? left, TextAndRoot? right)
    {
        return EqualityComparer<TextAndRoot>.Default.Equals(left, right);
    }

    public static bool operator !=(TextAndRoot? left, TextAndRoot? right)
    {
        return !(left == right);
    }
}
