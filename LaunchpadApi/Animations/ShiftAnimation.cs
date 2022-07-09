using LaunchpadApi.Entities;
using MoundBoard.Core;

namespace LaunchpadApi.Animations;

public class ShiftAnimation : Animation
{
    public int AnimationLength { get; private set; }
    public Colors[,] Colors { get; }
    public int Frame { get; private set; }
    public bool Repeat { get; set; }
    private int _offset;

    public ShiftAnimation(int delay, Colors[,] colors) : base(delay)
    {
        Colors = colors;
    }

    protected override void OnStart(LayoutButtons layoutButtons)
    {
        _offset = layoutButtons.Launchpad.ColumnCount;
        AnimationLength = Colors.GetLength(1) + _offset;
    }

    public override void ApplyNextFrame()
    {
        for (var x = 0; x < Layout!.Launchpad.ColumnCount; x++)
        {
            for (var y = 0; y < Layout.Launchpad.RowCount; y++)
            {
                Colors color;
                if (x < Layout.Launchpad.RowCount - Frame)
                {
                    color = MoundBoard.Core.Colors.Black;
                }
                else if (x < AnimationLength - Frame)
                {
                    color = Colors[Layout.Launchpad.RowCount - 1 - y, x + Frame - _offset];
                }
                else
                {
                    color = MoundBoard.Core.Colors.Black;
                }

                Layout![x, y].Color = color;
            }
        }
        Frame++;

        if (Repeat && Frame >= AnimationLength) Frame = 0;
    }

    public override bool HasNext()
    {
        return Frame <= AnimationLength;
    }
}