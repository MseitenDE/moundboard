using MoundBoard.Core;

namespace LaunchpadApi.Utils;

internal static class ColorConverter
{
    public static byte Convert(this Colors? color)
    {
        return color switch
        {
            Colors.Black => 0,
            Colors.DarkGray => 1,
            Colors.LightGray => 2,
            Colors.White => 3,
            Colors.LightRed => 4,
            Colors.Red => 5,
            Colors.Red1 => 6,
            Colors.Red2 => 7,
            Colors.LightOrange => 8,
            Colors.Orange => 9,
            Colors.Orange1 => 10,
            Colors.Orange2 => 11,
            Colors.LightYellow => 12,
            Colors.Yellow => 13,
            Colors.Yellow1 => 14,
            Colors.Yellow2 => 15,
            Colors.LightOlive => 16,
            Colors.Olive => 17,
            Colors.Olive1 => 18,
            Colors.Olive2 => 19,
            Colors.LightGreen => 20,
            Colors.Green => 21,
            Colors.Green1 => 22,
            Colors.Green2 => 23,
            Colors.LightLime => 24,
            Colors.Lime => 25,
            Colors.Lime1 => 26,
            Colors.Lime2 => 27,
            Colors.LightTurquoise => 28,
            Colors.Turquoise => 29,
            Colors.Turquoise1 => 30,
            Colors.Turquoise2 => 31,
            Colors.LightCyan => 32,
            Colors.Cyan => 33,
            Colors.Cyan1 => 34,
            Colors.Cyan2 => 35,
            Colors.LightPaleBlue => 36,
            Colors.PaleBlue => 37,
            Colors.PaleBlue1 => 38,
            Colors.PaleBlue2 => 39,
            Colors.LightBlue => 40,
            Colors.Blue => 41,
            Colors.Blue1 => 42,
            Colors.Blue2 => 43,
            Colors.LightDarkBlue => 44,
            Colors.DarkBlue => 45,
            Colors.DarkBlue1 => 46,
            Colors.DarkBlue2 => 47,
            Colors.LightPurple => 48,
            Colors.Purple => 49,
            Colors.Purple1 => 50,
            Colors.Purple2 => 51,
            Colors.LightMagenta => 52,
            Colors.Magenta => 53,
            Colors.Magenta1 => 54,
            Colors.Magenta2 => 55,
            Colors.LightPink => 56,
            Colors.Pink => 57,
            Colors.Pink1 => 58,
            Colors.Pink2 => 59,
            Colors.LightBrown => 60,
            Colors.Brown => 61,
            Colors.Brown1 => 62,
            Colors.Brown2 => 63,

            _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
    }
}