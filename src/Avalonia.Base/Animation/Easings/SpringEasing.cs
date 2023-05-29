namespace Avalonia.Animation.Easings;

/// <summary>
/// Eases a <see cref="double"/> value using a user-defined spring formula.
/// </summary>
public class SpringEasing : Easing
{
    private readonly Spring _internalSpring;

    /// <summary>
    /// The spring mass.
    /// </summary>
    public double Mass
    {
        get => _internalSpring.Mass;
        set
        {
            _internalSpring.Mass = value;
        }
    }

    /// <summary>
    /// The spring stiffness.
    /// </summary>
    public double Stiffness
    {
        get => _internalSpring.Stiffness;
        set
        {
            _internalSpring.Stiffness = value;
        }
    }

    /// <summary>
    /// The spring damping.
    /// </summary> 
    public double Damping
    {
        get => _internalSpring.Damping;
        set
        {
            _internalSpring.Damping = value;
        }
    }

    /// <summary>
    /// The spring initial velocity.
    /// </summary>
    public double InitialVelocity
    {
        get => _internalSpring.InitialVelocity;
        set
        {
            _internalSpring.InitialVelocity = value;
        }
    }

    public SpringEasing(double mass = 0d, double stiffness = 0d, double damping = 0d, double initialVelocity = 0d)
    {
        _internalSpring = new Spring();

        Mass = mass;
        Stiffness = stiffness;
        Damping = damping;
        InitialVelocity = initialVelocity;
    }
    
    public SpringEasing()
    {
        _internalSpring = new Spring();
    }

    /// <inheritdoc/>
    public override double Ease(double progress) => _internalSpring.GetSpringProgress(progress);
}
