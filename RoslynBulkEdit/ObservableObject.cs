using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RoslynBulkEdit;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T location, T value, [CallerMemberName] string? propertyName = null)
    {
        var isIdenticalMemory = RuntimeHelpers.Equals(location, value);
        location = value;
        if (isIdenticalMemory) return false;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
