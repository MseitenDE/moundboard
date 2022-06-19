using System;
using System.Windows.Media;

namespace MoundBoard.Utils;

internal static class ColorConverter
{
    public static Color Convert(this Core.Colors? color)
    {
        return color switch
        {
            Core.Colors.Black => Colors.Black,
            Core.Colors.DarkGray => Colors.DimGray,
            Core.Colors.LightGray => Colors.Gray,
            Core.Colors.White => Colors.White,
            Core.Colors.LightRed => Colors.Red, //TODO @MseitenDE wie verändere ich hier die Opacity?
            Core.Colors.Red => Colors.Red,
            Core.Colors.Green => Colors.Green,
            Core.Colors.Yellow => Colors.Yellow,
            Core.Colors.Blue => Colors.Blue with { A = 0 },
            
            
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }
    
    public static SolidColorBrush ConvertToBrush(this Core.Colors? color)
    {
        return new SolidColorBrush(Convert(color));
    }

    public static Core.Colors GetRandomColor()
    {
        var colors = Enum.GetValues<Core.Colors>();
        return colors[Random.Shared.Next(0, colors.Length)];
    }
}