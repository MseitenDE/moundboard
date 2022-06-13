using System;
using System.Windows.Media;

namespace MoundBoard.Utils;

internal static class ColorConverter
{
    public static SolidColorBrush Convert(this Core.Colors? color)
    {
        return color switch
        {
            Core.Colors.Black => new SolidColorBrush(Colors.Black),
            Core.Colors.DarkGray => new SolidColorBrush(Colors.DimGray),
            Core.Colors.LightGray => new SolidColorBrush(Colors.Gray),
            Core.Colors.White => new SolidColorBrush(Colors.White),
            Core.Colors.LightRed => new SolidColorBrush(Colors.Red), //TODO @MseitenDE wie verändere ich hier die Opacity?
            Core.Colors.Red => new SolidColorBrush(Colors.Red),
            Core.Colors.Green => new SolidColorBrush(Colors.Green),
            Core.Colors.Yellow => new SolidColorBrush(Colors.Yellow),
            Core.Colors.Blue => new SolidColorBrush(Colors.Blue),
            
            
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }

    public static Core.Colors GetRandomColor()
    {
        var colors = Enum.GetValues<Core.Colors>();
        return colors[Random.Shared.Next(0, colors.Length)];
    }
}