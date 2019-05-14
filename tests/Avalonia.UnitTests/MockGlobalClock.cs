using System;
using Avalonia.Animation;

namespace Avalonia.UnitTests
{
    public class MockGlobalClock : ClockBase, IGlobalClock
    {
        public new void Pulse(TimeSpan systemTime) => base.Pulse(systemTime);
    }
}
