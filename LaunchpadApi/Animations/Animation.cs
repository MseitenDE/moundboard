using LaunchpadApi.Entities;

namespace LaunchpadApi.Animations;

public abstract class Animation
{
    /// <summary>
    /// Delay in ms between frames
    /// </summary>
    public int Delay { get; }
    public Layout? Layout { get; private set; }

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
    public void Start(Layout layout)
    {
        Layout = layout;
        OnStart(layout);

        while (HasNext())
        {
            ApplyNextFrame();
            Thread.Sleep(Delay);
        }
    }
    
    /// <summary>
    /// Starts the animation, but new frames are only displayed if <see cref="ApplyNextFrame"/> is called.
    /// </summary>
    public void StartManual(Layout layout)
    {
        Layout = layout;
        OnStart(layout);
    }

    protected abstract void OnStart(Layout layout);
    public abstract void ApplyNextFrame();
    public abstract bool HasNext();
}