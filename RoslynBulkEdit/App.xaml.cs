using System.Windows;

namespace RoslynBulkEdit;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        new MainWindow(new MainViewModel(new MainViewModelDataAccess())).Show();
    }
}
