namespace MoundBoard;

public class Sound
{
    public string FilePath { get; set; }

    public Sound(string filePath)
    {
        FilePath = filePath;
    }

    public void Play()
    {
        new AudioPlayer(this).Play();
    }
}