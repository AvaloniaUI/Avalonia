using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Perspex.Input.UnitTests
{
    public class KeyGestureParseTests
    {
        private static readonly Dictionary<string, KeyGesture> SampleData = new Dictionary<string, KeyGesture>
        {
            {"Ctrl+A", new KeyGesture {Key = Key.A, Modifiers = InputModifiers.Control}},
            {"  \tShift\t+Alt +B", new KeyGesture {Key = Key.B, Modifiers = InputModifiers.Shift|InputModifiers.Alt} },
            {"Control++", new KeyGesture {Key = Key.OemPlus, Modifiers = InputModifiers.Control} }
        };
            
            
            
        [Fact]
        public void Key_Gesture_Is_Able_To_Parse_Sample_Data()
        {
            foreach (var d in SampleData)
                Assert.Equal(d.Value, KeyGesture.Parse(d.Key));
        }
    }
}
