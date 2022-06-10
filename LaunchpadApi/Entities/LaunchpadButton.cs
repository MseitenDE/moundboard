using Windows.Devices.Midi;
using LaunchpadApi.Animations;
using LaunchpadApi.Utils;
using MoundBoard.Core;

namespace LaunchpadApi.Entities;

public class LaunchpadButton
{
    private Colors? _color = Colors.Black;
    public Layout Layout { get; }
    public byte X { get; }
    public byte Y { get; }
    public ButtonEffect Effect { get; set; }
    private byte NoteIndex => (byte) (X + Y * Layout.Launchpad.RowCount);

    public Colors? Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value ?? Colors.Black;
            SendUpdate();
        }
    }

    public LaunchpadButton(Layout layout, byte x, byte y)
    {
        Layout = layout;
        X = x;
        Y = y;
    }

    /// <summary>
    /// Updates the button on the launchpad, ignoring if 
    /// </summary>
    public void SendUpdate()
    {
        if (!Layout.IsActive) return;

        var noteMessage = new MidiNoteOnMessage((byte) Effect, NoteIndex, Color.Convert());
        Layout.Launchpad.OutPort.SendMessage(noteMessage);
    }
}