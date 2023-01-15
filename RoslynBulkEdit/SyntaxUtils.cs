using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace RoslynBulkEdit;

internal static class SyntaxUtils
{
    public static string GetFullyQualifiedTypeName(BaseTypeDeclarationSyntax typeDeclaration)
    {
        if (typeDeclaration.Parent is not NamespaceDeclarationSyntax { Parent: CompilationUnitSyntax, Name: var namespaceName })
            throw new NotImplementedException("Type declaration not directly parented by a namespace declaration.");

        var builder = new StringBuilder();
        WriteCanonicalName(builder, namespaceName);
        builder.Append('.').Append(typeDeclaration.Identifier.ValueText);
        return builder.ToString();
    }

    private static void WriteCanonicalName(StringBuilder builder, NameSyntax name)
    {
        if (name is QualifiedNameSyntax qualified)
        {
            WriteCanonicalName(builder, qualified.Left);
            builder.Append('.').Append(qualified.Right.Identifier);
        }
        else if (name is SimpleNameSyntax simple)
        {
            builder.Append(simple.Identifier.ValueText);
        }
        else
        {
            throw new NotImplementedException(name.GetType().ToString());
        }
    }
}
