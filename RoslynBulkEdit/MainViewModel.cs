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

    private string? selectedSolutionFolder;
    public string? SelectedSolutionFolder
    {
        get => selectedSolutionFolder;
        set
        {
            if (!Set(ref selectedSolutionFolder, value)) return;

            var discoveredFiles = selectedSolutionFolder is not null
                ? dataAccess.DiscoverCSharpParsingTests(selectedSolutionFolder)
                : ImmutableArray<(string Path, DateTime LastModified)>.Empty;

            var lastModifiedIndex = discoveredFiles.IndexOfMax(file => file.LastModified);

            var commonDirectoryLength = PathUtils.GetCommonPath(from file in discoveredFiles select Path.GetDirectoryName(file.Path)!).Length;

            AvailableFiles = ImmutableArray.CreateRange(discoveredFiles, file => new AvailableFile(file.Path, file.Path[commonDirectoryLength..]));
            SelectedFile = lastModifiedIndex == -1 ? null : AvailableFiles[lastModifiedIndex];
        }
    }

    private ImmutableArray<AvailableFile> availableFiles = ImmutableArray<AvailableFile>.Empty;
    public ImmutableArray<AvailableFile> AvailableFiles { get => availableFiles; private set => Set(ref availableFiles, value); }

    private AvailableFile? selectedFile;
    public AvailableFile? SelectedFile
    {
        get => selectedFile;
        set
        {
            if (!Set(ref selectedFile, value)) return;

            TestCases = selectedFile is not null
                ? dataAccess.LoadTestCases(selectedFile.Path)
                : ImmutableArray<TestCase>.Empty;
        }
    }

    private ImmutableArray<TestCase> testCases = ImmutableArray<TestCase>.Empty;
    public ImmutableArray<TestCase> TestCases { get => testCases; private set => Set(ref testCases, value); }
}
