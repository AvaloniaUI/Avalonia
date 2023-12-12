// Ported from:
// https://svn.webkit.org/repository/webkit/trunk/Source/WebCore/platform/graphics/SpringSolver.h
/*
 * Copyright (C) 2016 Apple Inc. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY APPLE INC. AND ITS CONTRIBUTORS ``AS IS''
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL APPLE INC. OR ITS CONTRIBUTORS
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;

namespace Avalonia.Utilities;

internal struct SpringSolver 
{
    private readonly double m_w0;
    private readonly double m_zeta;
    private readonly double m_wd;
    private readonly double m_A;
    private readonly double m_B;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="period">The time period.</param>
    /// <param name="zeta">The damping ratio.</param>
    /// <param name="initialVelocity"></param>
    public SpringSolver(TimeSpan period, double zeta, double initialVelocity) 
        : this(
            2 * Math.PI / period.TotalSeconds, 
            zeta, 
            initialVelocity)
    {
        // T is time period [s]
        // T = (2*PI / sqrt(k)) * sqrt(m)

        // ωn is natural frequency of the system [Hz] [1/s]
        // ωn = 2*PI / T
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="m">The mass of the oscillating body.</param>
    /// <param name="k">The stiffness of the oscillated body (spring constant).</param>
    /// <param name="c">The actual damping.</param>
    /// <param name="initialVelocity">The initial velocity.</param>
    public SpringSolver(double m, double k, double c, double initialVelocity)
        : this(
            Math.Sqrt(k / m), // ωn
            c / (2 * Math.Sqrt(k * m)), // c / Cc
            initialVelocity)
    {
        // ωn is natural frequency of the system [Hz] [1/s]
        // ωn = sqrt(k / m)

        // Cc is critical damping coefficient
        // Cc = 2 * Sqrt(k * m)
        // Cc = 2 * m * wn
        // Cc = 2 * m * Sqrt(k / m)

        // ζ is damping ratio (Greek letter zeta)
        // ζ = m_zeta = c / Cc
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ωn">The the natural frequency of the system [rad/s].</param>
    /// <param name="zeta">The damping ratio.</param>
    /// <param name="initialVelocity"></param>
    public SpringSolver(double ωn, double zeta, double initialVelocity)
    {
        m_w0 = ωn;
        m_zeta = zeta;

        if (m_zeta < 1) {
            // Under-damped.
            m_wd = m_w0 * Math.Sqrt(1 - m_zeta * m_zeta);
            m_A = 1;
            m_B = (m_zeta * m_w0 + -initialVelocity) / m_wd;
        } else {
            // Critically damped (ignoring over-damped case for now).
            m_A = 1;
            m_B = -initialVelocity + m_w0;
            m_wd = 0;
        }
    }

    public readonly double Solve(double t)
    {
        if (m_zeta < 1) {
            // Under-damped
            t = Math.Exp(-t * m_zeta * m_w0) * (m_A * Math.Cos(m_wd * t) + m_B * Math.Sin(m_wd * t));
        } else {
            // Critically damped (ignoring over-damped case for now).
            t = (m_A + m_B * t) * Math.Exp(-t * m_w0);
        }

        // Map range from [1..0] to [0..1].
        return 1 - t;
    }
}
