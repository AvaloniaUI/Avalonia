using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform
{
    public interface ITopLevelImplWithSoundEffects
    {
        void Play(SoundEffects soundEffects);
    }

    public enum SoundEffects
    {
        Click
    }
}
