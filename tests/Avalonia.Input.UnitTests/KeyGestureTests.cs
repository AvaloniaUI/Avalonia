using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class KeyGestureTests
    {
        public static readonly IEnumerable<object[]> SampleData = new object[][]
        {
            new object[]{"Ctrl+A", new KeyGesture {Key = Key.A, Modifiers = InputModifiers.Control}},
            new object[]{"  \tShift\t+Alt +B", new KeyGesture {Key = Key.B, Modifiers = InputModifiers.Shift|InputModifiers.Alt} },
            new object[]{"Control++", new KeyGesture {Key = Key.OemPlus, Modifiers = InputModifiers.Control} }
        };
            
            
            
        [Theory]
        [MemberData(nameof(SampleData))]
        public void Key_Gesture_Is_Able_To_Parse_Sample_Data(string text, KeyGesture gesture)
        {
            Assert.Equal(gesture, KeyGesture.Parse(text));
        }

        [Theory]
        [InlineData(Key.OemMinus, Key.Subtract)]
        [InlineData(Key.OemPlus, Key.Add)]
        [InlineData(Key.OemPeriod, Key.Decimal)]
        public void Key_Gesture_Matches_NumPad_To_Regular_Digit(Key gestureKey, Key pressedKey)
        {
            var keyGesture = new KeyGesture
            {
                Key = gestureKey
            };
            Assert.True(keyGesture.Matches(new KeyEventArgs
            {
                Key = pressedKey
            }));
        }
    }
}
