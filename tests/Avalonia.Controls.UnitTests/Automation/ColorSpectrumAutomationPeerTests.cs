using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ColorSpectrumAutomationPeerTests
{
    public class AutomationPeerTests : ScopedTestBase
    {
        [Fact]
        public void Creates_ColorSpectrumAutomationPeer()
        {
            var spectrum = new ColorSpectrum();
            var peer = ControlAutomationPeer.CreatePeerForElement(spectrum);

            Assert.IsType<ColorSpectrumAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_Custom()
        {
            var spectrum = new ColorSpectrum();
            var peer = (ColorSpectrumAutomationPeer)ControlAutomationPeer.CreatePeerForElement(spectrum);

            Assert.Equal(AutomationControlType.Custom, peer.GetAutomationControlType());
        }

        [Fact]
        public void ClassName_Is_ColorSpectrum()
        {
            var spectrum = new ColorSpectrum();
            var peer = (ColorSpectrumAutomationPeer)ControlAutomationPeer.CreatePeerForElement(spectrum);

            Assert.Equal("ColorSpectrum", peer.GetClassName());
        }

        [Fact]
        public void Implements_IValueProvider()
        {
            var spectrum = new ColorSpectrum();
            var peer = (ColorSpectrumAutomationPeer)ControlAutomationPeer.CreatePeerForElement(spectrum);

            Assert.Equal(Colors.White.ToString(), peer.Value);

            var valueProvider = Assert.IsAssignableFrom<IValueProvider>(peer);
            valueProvider.SetValue("#00FF00");

            Assert.Equal(Colors.Lime.ToString(), peer.Value);
            Assert.Equal(Colors.Lime, spectrum.Color);
        }

        [Fact]
        public void SetValue_Uses_Color_Parse_And_Throws_FormatException_On_Invalid_Input()
        {
            var spectrum = new ColorSpectrum();
            var peer = (ColorSpectrumAutomationPeer)ControlAutomationPeer.CreatePeerForElement(spectrum);
            var valueProvider = Assert.IsAssignableFrom<IValueProvider>(peer);

            Assert.Throws<FormatException>(() => valueProvider.SetValue("not-a-color"));
        }

        [Fact]
        public void ValueProperty_Raises_AutomationPropertyChangedEvent_On_Color_Change()
        {
            var spectrum = new ColorSpectrum();
            var peer = (ColorSpectrumAutomationPeer)ControlAutomationPeer.CreatePeerForElement(spectrum);
            AutomationPropertyChangedEventArgs? changed = null;

            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == ValuePatternIdentifiers.ValueProperty)
                {
                    changed = e;
                }
            };

            spectrum.Color = Colors.Black;

            Assert.NotNull(changed);
            Assert.Equal(ValuePatternIdentifiers.ValueProperty, changed!.Property);
            Assert.Equal(Colors.White.ToString(), changed.OldValue);
            Assert.Equal(Colors.Black.ToString(), changed.NewValue);
        }
    }
}
