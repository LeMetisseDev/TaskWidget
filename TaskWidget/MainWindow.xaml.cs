using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TaskWidget.Models;
using TaskWidget.Services;

namespace TaskWidget;

public partial class MainWindow : Window
{
    private enum View { Tasks, History, Settings }

    private readonly ObservableCollection<TaskItem> _active = new();
    private readonly ObservableCollection<TaskItem> _archived = new();
    private readonly AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settings = Storage.LoadSettings();
        var tasks = Storage.LoadTasks();
        foreach (var task in tasks.Where(t => !t.Archived))
            _active.Add(task);
        foreach (var task in tasks.Where(t => t.Archived).OrderByDescending(t => t.ArchivedAt))
            _archived.Add(task);

        TaskList.ItemsSource = _active;
        HistoryList.ItemsSource = _archived;
        _active.CollectionChanged += (_, _) => UpdateEmptyStates();
        _archived.CollectionChanged += (_, _) => UpdateEmptyStates();
        UpdateEmptyStates();

        RestoreWindowPlacement();
        OpacitySlider.Value = _settings.Opacity;
        ApplyTheme();
        BuildPresets();
        BuildPalette();
        ShowView(View.Tasks);
    }

    // ---------- Window ----------

    private void RestoreWindowPlacement()
    {
        Width = Math.Max(MinWidth, _settings.Width);
        Height = Math.Max(MinHeight, _settings.Height);

        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                              SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        if (_settings.Left is double left && _settings.Top is double top &&
            screen.IntersectsWith(new Rect(left, top, Width, Height)))
        {
            Left = left;
            Top = top;
        }
        else
        {
            var work = SystemParameters.WorkArea;
            Left = work.Right - Width - 20;
            Top = work.Top + 20;
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        _settings.Left = bounds.Left;
        _settings.Top = bounds.Top;
        _settings.Width = bounds.Width;
        _settings.Height = bounds.Height;
        _settings.Opacity = OpacitySlider.Value;
        Storage.SaveSettings(_settings);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // ---------- Navigation ----------

    private View _view;

    private void ShowView(View view)
    {
        _view = view;
        TasksView.Visibility = view == View.Tasks ? Visibility.Visible : Visibility.Collapsed;
        HistoryView.Visibility = view == View.History ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = view == View.Settings ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = view == View.Tasks ? Visibility.Collapsed : Visibility.Visible;
        TitleText.Text = view switch
        {
            View.History => "Historique",
            View.Settings => "Réglages",
            _ => "Tâches",
        };
        if (view == View.Tasks) NewTaskBox.Focus();
    }

    private void History_Click(object sender, RoutedEventArgs e) =>
        ShowView(_view == View.History ? View.Tasks : View.History);

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        ShowView(_view == View.Settings ? View.Tasks : View.Settings);

    private void Back_Click(object sender, RoutedEventArgs e) => ShowView(View.Tasks);

    // ---------- Tasks ----------

    private void AddTask()
    {
        var text = NewTaskBox.Text.Trim();
        if (text.Length == 0) return;
        _active.Add(new TaskItem { Text = text });
        NewTaskBox.Clear();
        SaveTasks();
    }

    private void Add_Click(object sender, RoutedEventArgs e) => AddTask();

    private void NewTaskBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddTask();
    }

    private void NewTaskBox_TextChanged(object sender, TextChangedEventArgs e) =>
        Placeholder.Visibility = NewTaskBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void ToggleDone_Click(object sender, RoutedEventArgs e)
    {
        var task = ItemOf(sender);
        task.Done = !task.Done;
        SaveTasks();
    }

    private void Archive_Click(object sender, RoutedEventArgs e)
    {
        var task = ItemOf(sender);
        task.Archived = true;
        task.ArchivedAt = DateTime.Now;
        _active.Remove(task);
        _archived.Insert(0, task);
        SaveTasks();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        var task = ItemOf(sender);
        task.Archived = false;
        task.ArchivedAt = null;
        _archived.Remove(task);
        _active.Add(task);
        SaveTasks();
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var task = ItemOf(sender);
        var answer = MessageBox.Show(this, $"Supprimer définitivement « {task.Text} » ?",
            "Confirmer la suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        _archived.Remove(task);
        SaveTasks();
    }

    private static TaskItem ItemOf(object sender) => (TaskItem)((FrameworkElement)sender).DataContext;

    private void SaveTasks() => Storage.SaveTasks(_active.Concat(_archived));

    private void UpdateEmptyStates()
    {
        TasksEmpty.Visibility = _active.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryEmpty.Visibility = _archived.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------- Settings ----------

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Fires during InitializeComponent when Minimum coerces the value.
        if (OpacityText is null) return;
        Opacity = e.NewValue;
        OpacityText.Text = $"{e.NewValue * 100:0} %";
    }

    private void ApplyTheme()
    {
        Theme.Apply(_settings.Background, _settings.Accent, _settings.Foreground);
        RefreshHexEditor();
    }

    private string SelectedColor
    {
        get => TargetAccent.IsChecked == true ? _settings.Accent
             : TargetForeground.IsChecked == true ? _settings.Foreground
             : _settings.Background;
        set
        {
            if (TargetAccent.IsChecked == true) _settings.Accent = value;
            else if (TargetForeground.IsChecked == true) _settings.Foreground = value;
            else _settings.Background = value;
        }
    }

    private void SetSelectedColor(string hex)
    {
        SelectedColor = hex;
        ApplyTheme();
        Storage.SaveSettings(_settings);
    }

    private void RefreshHexEditor()
    {
        // Radio Checked events can fire during InitializeComponent, before the editor exists.
        if (HexBox is null || HexPreview is null) return;
        HexBox.Text = SelectedColor;
        HexPreview.Fill = new SolidColorBrush(Theme.Parse(SelectedColor));
    }

    private void ColorTarget_Checked(object sender, RoutedEventArgs e) => RefreshHexEditor();

    private void CommitHex()
    {
        if (Theme.TryParse(HexBox.Text, out var hex)) SetSelectedColor(hex);
        else RefreshHexEditor();
    }

    private void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) CommitHex();
    }

    private void HexBox_LostFocus(object sender, RoutedEventArgs e) => CommitHex();

    private void BuildPalette()
    {
        foreach (var hex in Theme.Palette)
        {
            var swatch = new Button
            {
                Style = (Style)FindResource("SwatchButton"),
                Background = new SolidColorBrush(Theme.Parse(hex)),
                ToolTip = hex,
            };
            swatch.Click += (_, _) => SetSelectedColor(hex);
            PalettePanel.Children.Add(swatch);
        }
    }

    private void BuildPresets()
    {
        foreach (var preset in Theme.Presets)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            foreach (var hex in new[] { preset.Background, preset.Accent, preset.Foreground })
            {
                content.Children.Add(new Ellipse
                {
                    Width = 10, Height = 10, Margin = new Thickness(0, 0, 3, 0),
                    Fill = new SolidColorBrush(Theme.Parse(hex)),
                    Stroke = Brushes.Gray, StrokeThickness = 0.5,
                });
            }
            content.Children.Add(new TextBlock { Text = preset.Name, Margin = new Thickness(4, 0, 0, 0) });

            var button = new Button { Style = (Style)FindResource("ChipButton"), Content = content };
            button.Click += (_, _) => ApplyPreset(preset);
            PresetPanel.Children.Add(button);
        }
    }

    private void ApplyPreset(ThemePreset preset)
    {
        _settings.Background = preset.Background;
        _settings.Accent = preset.Accent;
        _settings.Foreground = preset.Foreground;
        ApplyTheme();
        Storage.SaveSettings(_settings);
    }

    private void ResetTheme_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreset(Theme.Presets[0]);
        OpacitySlider.Value = 1;
    }
}
