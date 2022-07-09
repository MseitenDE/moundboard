using LaunchpadApi.Entities;

namespace LaunchpadApi.Animations;

public abstract class Animation
{
    /// <summary>
    /// Delay in ms between frames
    /// </summary>
    public int Delay { get; }
    public LayoutButtons? Layout { get; private set; }

    /// <summary>
    /// Creates a new animation
    /// </summary>
    /// <param name="delay">Delay in ms between frames</param>
    protected Animation(int delay)
    {
        Delay = delay;
    }
    
    /// <summary>
    /// Starts the animation and applies the frames after the given time.
    /// </summary>
    public async void Start(LayoutButtons layoutButtons)
    {
        Layout = layoutButtons;
        OnStart(layoutButtons);

        while (HasNext())
        {
            ApplyNextFrame();
            await Task.Delay(Delay);
        }
    }
    
    /// <summary>
    /// Starts the animation, but new frames are only displayed if <see cref="ApplyNextFrame"/> is called.
    /// </summary>
    public void StartManual(LayoutButtons layoutButtons)
    {
        Layout = layoutButtons;
        OnStart(layoutButtons);
    }

    protected abstract void OnStart(LayoutButtons layoutButtons);
    public abstract void ApplyNextFrame();
    public abstract bool HasNext();
}