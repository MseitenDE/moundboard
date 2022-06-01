using System.Windows.Controls;

namespace MoundBoard;

public partial class MainWindow
{
    public MidiHandler MidiHandler { get; }
    public MainWindow()
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
}