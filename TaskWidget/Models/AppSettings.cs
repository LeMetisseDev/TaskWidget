namespace TaskWidget.Models;

public class AppSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 480;
    public double Opacity { get; set; } = 1.0;

    public string Background { get; set; } = Theme.Presets[0].Background;
    public string Accent { get; set; } = Theme.Presets[0].Accent;
    public string Foreground { get; set; } = Theme.Presets[0].Foreground;
}
