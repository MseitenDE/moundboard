using System.Windows;
using System.Windows.Controls;

namespace MoundBoard;

public partial class MidiIOSettings
{
    public MidiHandler MidiHandler { get; }

    public MidiIOSettings()
    {
        InitializeComponent();
        
        MidiHandler = new MidiHandler(Dispatcher, MidiInPortListBox, MidiOutPortListBox);
    }
    
    private void MidiInPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedInputChanged(MidiInPortListBox.SelectedIndex);
    }

    private void MidiOutPortListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MidiHandler.OnSelectedOutputChanged(MidiOutPortListBox.SelectedIndex);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        MidiHandler.PlayStartupAnimation();
    }
}