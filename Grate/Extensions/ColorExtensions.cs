using System.Collections.Generic;
using UnityEngine;

public static class ColorExtensions
{
    // A static dictionary to map predefined Unity colors to their string names
    private static readonly Dictionary<Color, string> ColorMap = new()
    {
        { Color.red, "Red" },
        { Color.green, "Green" },
        { Color.blue, "Blue" },
        { Color.yellow, "Yellow" },
        { Color.magenta, "Magenta" },
        { Color.cyan, "Cyan" },
        { Color.white, "White" },
        { Color.black, "Black" },
        { Color.gray, "Gray" },
        { Color.clear, "Clear" }
    };

    public static Color StringToColor(this string input)
    {
        if (input.StartsWith("#"))
        {
            ColorUtility.TryParseHtmlString(input, out var customColor);
            return customColor;
        }

        var colorName = input.ToLower();
        foreach (var entry in ColorMap)
            if (entry.Value.ToLower() == colorName)
                return entry.Key;

        return Color.white;
    }

    public static string ColorName(this Color color)
    {
        if (ColorMap.TryGetValue(color, out var colorName)) return colorName;
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }
}