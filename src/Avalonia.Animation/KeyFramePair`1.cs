using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Collections;
using System.ComponentModel;
using Avalonia.Animation.Utils;
using System.Reactive.Linq;
using System.Linq;
using Avalonia.Data;
using System.Reactive.Disposables;

namespace Avalonia.Animation
{
    /// <summary>
    /// Represents a pair of keyframe, usually the
    /// Start and End keyframes of a <see cref="KeyFrames{T}"/> object.
    /// </summary>
    public struct KeyFramePair<T>
    {
        /// <summary>
        /// Initializes this <see cref="KeyFramePair{T}"/>
        /// </summary>
        /// <param name="FirstKeyFrame"></param>
        /// <param name="LastKeyFrame"></param>
        public KeyFramePair(KeyValuePair<double, T> FirstKeyFrame, KeyValuePair<double, T> LastKeyFrame) : this()
        {
            this.FirstKeyFrame = FirstKeyFrame;
            this.SecondKeyFrame = LastKeyFrame;
        }

        /// <summary>
        /// First <see cref="KeyFrame"/> object.
        /// </summary>
        public KeyValuePair<double, T> FirstKeyFrame { get; private set; }

        /// <summary>
        /// Second <see cref="KeyFrame"/> object.
        /// </summary>
        public KeyValuePair<double, T> SecondKeyFrame { get; private set; }
    }
}