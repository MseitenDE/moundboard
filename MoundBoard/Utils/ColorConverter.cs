using System;
using System.Windows.Media;

namespace MoundBoard.Utils;

internal static class ColorConverter
{
    public static Color Convert(this Core.Colors? color)
    {
        return color switch
        {
            Core.Colors.Red => Colors.Red,
            Core.Colors.Green => Colors.Green,
            Core.Colors.Yellow => Colors.Yellow,
            Core.Colors.Blue => Colors.Blue,
            Core.Colors.Black => Colors.Black,
            Core.Colors.White => Colors.White,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public static Core.Colors GetRandomColor()
    {
        var colors = Enum.GetValues<Core.Colors>();
        return colors[Random.Shared.Next(0, colors.Length)];
    }
}