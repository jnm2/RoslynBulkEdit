﻿using System.Diagnostics;
using System.IO;

namespace RoslynBulkEdit;

[DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }

    public override string ToString() => Path;
}
