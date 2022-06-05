using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Midi;

namespace MoundBoard;

public partial class MidiIOSettings : Window
{
    public MidiHandler MidiHandler { get; }

    public MidiIOSettings()
    {
        InitializeComponent();
        
        MidiHandler = new MidiHandler(Dispatcher, MidiInPortListBox, MidiOutPortListBox);

    }
    private void MidiInPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedInputChanged();
    }

    private void MidiOutPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedOutputChanged();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        byte channel = 0;
        byte controller = 0;
        byte controlValue = 0;


        IMidiMessage message = new MidiControlChangeMessage(channel,controller, controlValue);
        
        
    }
}