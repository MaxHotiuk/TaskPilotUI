using UI.Interfaces.Services;

namespace UI.Services;

public class ColorService : IColorService
{
    public string GetTagTextColor(string? hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
            return "#000000";
        
        string cleanHex = hexColor.TrimStart('#');
        
        if (cleanHex.Length != 6 || !IsValidHex(cleanHex))
            return "#000000";
        
        int r = Convert.ToInt32(cleanHex.Substring(0, 2), 16);
        int g = Convert.ToInt32(cleanHex.Substring(2, 2), 16);
        int b = Convert.ToInt32(cleanHex.Substring(4, 2), 16);
        
        double brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
        
        int newR, newG, newB;
        
        if (brightness > 0.5)
        {
            double factor = 0.4;
            newR = (int)(r * (1 - factor));
            newG = (int)(g * (1 - factor));
            newB = (int)(b * (1 - factor));
        }
        else
        {
            double factor = 0.8;
            newR = (int)(r + (255 - r) * factor);
            newG = (int)(g + (255 - g) * factor);
            newB = (int)(b + (255 - b) * factor);
        }
        
        newR = Math.Max(0, Math.Min(255, newR));
        newG = Math.Max(0, Math.Min(255, newG));
        newB = Math.Max(0, Math.Min(255, newB));
        
        return $"#{newR:X2}{newG:X2}{newB:X2}";
    }

    private bool IsValidHex(string hex)
    {
        return hex.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
    }
}