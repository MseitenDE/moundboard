using System.Windows;

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
        var launchpad = MidiHandler.Instance.Launchpad;
        Window mappingWindow = new MappingWindow(launchpad.CurrentLayout);
        mappingWindow.Show();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.IsMk2 = true;
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        MidiHandler.IsMk2 = false;
    }
}