using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace TaskWidget;

public record ThemePreset(string Name, string Background, string Accent, string Foreground);

/// <summary>Derives the full brush set from three base colors and pushes it into app resources.</summary>
public static class Theme
{
    public static readonly ThemePreset[] Presets =
    {
        new("Sombre", "#1E1E24", "#4F8CFF", "#E8E8EC"),
        new("Clair", "#F5F5F7", "#2F6FEB", "#1E1E24"),
        new("Bleu nuit", "#0F1B2D", "#3DD6D0", "#DDE7F3"),
        new("Violet", "#221A33", "#B388FF", "#ECE6F7"),
    };

    public static readonly string[] Palette =
    {
        "#1E1E24", "#2B2D31", "#0F1B2D", "#221A33", "#1B2A1F",
        "#FFFFFF", "#F5F5F7", "#FFF8E7", "#E8E8EC",
        "#4F8CFF", "#3DD6D0", "#34C759", "#FFB020", "#FF6B6B", "#B388FF", "#FF7AC6",
    };

    public static void Apply(string background, string accent, string foreground)
    {
        var bg = Parse(background);
        var ac = Parse(accent);
        var fg = Parse(foreground);
        var res = Application.Current.Resources;

        res["BgBrush"] = Brush(bg);
        res["SurfaceBrush"] = Brush(Blend(bg, fg, 0.07));
        res["SurfaceHoverBrush"] = Brush(Blend(bg, fg, 0.15));
        res["FgBrush"] = Brush(fg);
        res["MutedBrush"] = Brush(Blend(bg, fg, 0.55));
        res["AccentBrush"] = Brush(ac);
        res["OnAccentBrush"] = Brush(Luminance(ac) > 0.55 ? Colors.Black : Colors.White);
    }

    public static bool TryParse(string? hex, out string normalized)
    {
        normalized = "";
        hex = hex?.Trim().TrimStart('#');
        if (hex is null || hex.Length != 6 ||
            !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return false;
        normalized = "#" + hex.ToUpperInvariant();
        return true;
    }

    public static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static SolidColorBrush Brush(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Color Blend(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));

    private static double Luminance(Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
}
