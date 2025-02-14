using System.Collections.Immutable;
using System.IO;

namespace RoslynBulkEdit;

public sealed class MainViewModel : ObservableObject
{
    private readonly MainViewModelDataAccess dataAccess;

    public MainViewModel(MainViewModelDataAccess dataAccess)
    {
        this.dataAccess = dataAccess;
        AvailableSolutionFolders = dataAccess.DiscoverSolutions();
        SelectedSolutionFolder = AvailableSolutionFolders.FirstOrDefault();
    }

    public ImmutableArray<string> AvailableSolutionFolders { get; }

    public string? SelectedSolutionFolder
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;

            var discoveredFiles = field is not null
                ? dataAccess.DiscoverCSharpParsingTests(field)
                : ImmutableArray<(string Path, DateTime LastModified)>.Empty;

            var lastModifiedIndex = discoveredFiles.IndexOfMax(file => file.LastModified);

            var commonDirectoryLength = PathUtils.GetCommonPath(from file in discoveredFiles select Path.GetDirectoryName(file.Path)!).Length;

            AvailableFiles = ImmutableArray.CreateRange(discoveredFiles, file => new AvailableFile(file.Path, file.Path[commonDirectoryLength..]));
            SelectedFile = lastModifiedIndex == -1 ? null : AvailableFiles[lastModifiedIndex];
        }
    }

    public ImmutableArray<AvailableFile> AvailableFiles { get; private set => Set(ref field, value); } = ImmutableArray<AvailableFile>.Empty;

    public AvailableFile? SelectedFile
    {
        get;
        set
        {
            if (!Set(ref field, value)) return;

            TestCases = field is not null
                ? dataAccess.LoadTestCases(field.Path)
                : ImmutableArray<TestCase>.Empty;
        }
    }

    public ImmutableArray<TestCase> TestCases { get; private set => Set(ref field, value); } = ImmutableArray<TestCase>.Empty;
}
