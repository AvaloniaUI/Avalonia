using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Utilities;

namespace Avalonia.Animation;

/// <summary>
/// Determines how an animation is used based on spring formula.
/// </summary>
[TypeConverter(typeof(SpringTypeConverter))]
internal class Spring
{
    private SpringSolver _springSolver;
    private double _mass;
    private double _stiffness;
    private double _damping;
    private double _initialVelocity;
    private bool _isDirty;

    /// <summary>
    /// Create a <see cref="Spring"/>.
    /// </summary>
    public Spring()
    {
        _mass = 0.0;
        _stiffness = 0.0;
        _damping = 0.0;
        _initialVelocity = 0.0;
        _isDirty = true;
    }

    /// <summary>
    /// Create a <see cref="Spring"/> with the given parameters.
    /// </summary>
    /// <param name="mass">The spring mass.</param>
    /// <param name="stiffness">The spring stiffness.</param>
    /// <param name="damping">The spring damping.</param>
    /// <param name="initialVelocity">The spring initial velocity.</param>
    public Spring(double mass, double stiffness, double damping, double initialVelocity)
    {
        _mass = mass;
        _stiffness = stiffness;
        _damping = damping;
        _initialVelocity = initialVelocity;
        _isDirty = true;
    }

    /// <summary>
    /// Parse a <see cref="Spring"/> from a string. The string needs to contain 4 values in it.
    /// </summary>
    /// <param name="value">string with 4 values in it</param>
    /// <param name="culture">culture of the string</param>
    /// <exception cref="FormatException">Thrown if the string does not have 4 values</exception>
    /// <returns>A <see cref="Spring"/> with the appropriate values set</returns>
    public static Spring Parse(string value, CultureInfo? culture)
    {
        if (culture is null)
        {
            culture = CultureInfo.InvariantCulture;
        }

        using var tokenizer = new StringTokenizer(value, culture, exceptionMessage: $"Invalid Spring string: \"{value}\".");
        return new Spring(tokenizer.ReadDouble(), tokenizer.ReadDouble(), tokenizer.ReadDouble(), tokenizer.ReadDouble());
    }

    /// <summary>
    /// The spring mass.
    /// </summary>
    public double Mass
    {
        get => _mass;
        set
        {
            _mass = value;
            _isDirty = true;
        }
    }

    /// <summary>
    /// The spring stiffness.
    /// </summary>
    public double Stiffness
    {
        get => _stiffness;
        set
        {
            _stiffness = value;
            _isDirty = true;
        }
    }

    /// <summary>
    /// The spring damping.
    /// </summary>
    public double Damping
    {
        get => _damping;
        set
        {
            _damping = value;
            _isDirty = true;
        }
    }

    /// <summary>
    /// The spring initial velocity.
    /// </summary>
    public double InitialVelocity
    {
        get => _initialVelocity;
        set
        {
            _initialVelocity = value;
            _isDirty = true;
        }
    }

    /// <summary>
    /// Calculates spring progress from a linear progress.
    /// </summary>
    /// <param name="linearProgress">the linear progress</param>
    /// <returns>The spring progress</returns>
    public double GetSpringProgress(double linearProgress)
    {
        if (_isDirty)
        {
            Build();
        }

        return _springSolver.Solve(linearProgress);
    }

    /// <summary>
    /// Create cached spring solver.
    /// </summary>
    private void Build()
    {
        _springSolver = new SpringSolver(_mass, _stiffness, _damping, _initialVelocity);
        _isDirty = false;
    }
}
