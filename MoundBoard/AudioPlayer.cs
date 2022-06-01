using NAudio.Wave;

namespace MoundBoard;

public class AudioPlayer
{
    public Sound Sound { get; }
    public Mp3FileReader Reader { get; }
    public WaveOut Output { get; }

    public AudioPlayer(Sound sound)
    {
        Sound = sound;
        Reader = new Mp3FileReader(sound.FilePath);
        Output = new WaveOut();
        
        Output.PlaybackStopped += OutputOnPlaybackStopped;
    }

    public void Play()
    {
        Output.Init(Reader);
        Output.Play();
    }
    
    private void OutputOnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Reader.Dispose();
        Output.Dispose();
    }
}