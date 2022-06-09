using System.Windows;
using MoundBoard.Entities;

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
        Window mappingWindow = new MappingWindow(new Layout());
        mappingWindow.Show();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.isMartin = true;
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.isMartin = false;
    }
}