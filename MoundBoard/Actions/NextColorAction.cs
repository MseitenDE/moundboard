using System;
using LaunchpadApi.Entities;
using MoundBoard.Core;

namespace MoundBoard.Actions;

public class NextColorAction
{
    public static void NextColor(LaunchpadButton button)
    {
        button.Color = new Colors() + 1;
    }
}