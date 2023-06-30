using System;
using Avalonia.Animation;

namespace Avalonia.UnitTests
{
    internal class MockGlobalClock : ClockBase, IGlobalClock
    {
        public new void Pulse(TimeSpan systemTime) => base.Pulse(systemTime);
    }
}
