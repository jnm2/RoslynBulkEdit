namespace RoslynBulkEdit;

public sealed record AvailableFile(string Path, string Display)
{
    public override string ToString() => Display;
}
