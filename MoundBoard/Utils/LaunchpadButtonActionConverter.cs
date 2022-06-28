using System;
using MoundBoard.Core;

namespace MoundBoard.Utils;

internal static class LaunchpadButtonActionConverter
{
    public static Action Convert(this Core.LaunchpadButtonAction buttonAction)
    {
        return buttonAction switch
        {
            LaunchpadButtonAction.nextColor => Actions.NextColorAction.NextColor(),
            
            _ => throw new ArgumentOutOfRangeException(nameof(buttonAction), buttonAction, null)
        };
    }
}