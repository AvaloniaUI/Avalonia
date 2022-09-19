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
    private double m_w0;
    private double m_zeta;
    private double m_wd;
    private double m_A;
    private double m_B;

    public SpringSolver(double mass, double stiffness, double damping, double initialVelocity)
    {
        m_w0 = Math.Sqrt(stiffness / mass);
        m_zeta = damping / (2 * Math.Sqrt(stiffness * mass));

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
