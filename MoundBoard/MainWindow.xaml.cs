using System.Windows;
using System.Windows.Controls;
using Windows.Devices.Midi;

namespace MoundBoard;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        
        }


    private void IOButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Window midiIoSettingsWindow = new MidiIOSettings();
        midiIoSettingsWindow.Show();
    }

    private void MappingButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        Window mappingWindow = new MappingWindow();
        mappingWindow.Show();
    }
}