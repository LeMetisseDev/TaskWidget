using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskWidget.Models;

public class TaskItem : INotifyPropertyChanged
{
    private bool _done;
    private DateTime? _archivedAt;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool Done
    {
        get => _done;
        set { _done = value; OnPropertyChanged(); }
    }

    public DateTime? ArchivedAt
    {
        get => _archivedAt;
        set { _archivedAt = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
