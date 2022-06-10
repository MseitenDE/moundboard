using Windows.Devices.Midi;

namespace LaunchpadApi.Entities;

public class Launchpad
{
    public byte RowCount { get; set; } = 10;
    public byte ColumnCount { get; set; } = 10;
    
    private Layout? _currentLayout;
    public MidiInPort InPort { get; set; }
    public IMidiOutPort OutPort { get; set; }

    public Layout? CurrentLayout
    {
        get => _currentLayout;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_currentLayout == value) return;
            
            ChangeLayout(value);
        }
    }

    public Launchpad(MidiInPort inPort, IMidiOutPort outPort)
    {
        InPort = inPort;
        OutPort = outPort;
    }

    private void ChangeLayout(Layout layout)
    {
        if (_currentLayout != null)
        {
            _currentLayout.IsActive = false;
        }
        _currentLayout = layout;
        
        layout.IsActive = true;
        layout.Apply();
    }
}