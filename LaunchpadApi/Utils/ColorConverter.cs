using MoundBoard.Core;

namespace LaunchpadApi.Utils;

internal static class ColorConverter
{
    public static byte Convert(this Colors? color)
    {
        return color switch
        {
            Colors.Red => 5,
            Colors.Green => 21,
            Colors.Yellow => 13,
            Colors.Blue => 41,
            Colors.Black => 0,
            Colors.White => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }
}