using Windows.Devices.Midi;

namespace LaunchpadApi.Entities;

public class Launchpad
{
    public byte RowCount { get; set; } = 10;
    public byte ColumnCount { get; set; } = 10;
    
    private Layout? _currentLayout;
    public Layout[] Layouts;
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

        Layouts = new Layout[] { new LayoutButtons("layout1", this), new LayoutButtonsFader("layout2", this) };
        // Layouts[0].Buttons[5, 5].Color = Colors.Blue;
        // Layouts[1].Buttons[7, 7].Color = Colors.Red;
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

    public void ChangeLayout(int index)
    {
        if (_currentLayout != null)
        {
            _currentLayout.IsActive = false;
        }

        _currentLayout = Layouts[index];

        Layouts[index].IsActive = true;
        Layouts[index].Apply();
    }
}