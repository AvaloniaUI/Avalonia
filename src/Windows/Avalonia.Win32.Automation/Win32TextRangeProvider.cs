using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Win32.Automation.Interop;
using AAP = Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    /// <summary>
    /// Wraps a single platform-agnostic <see cref="AAP.ITextRangeProvider"/> instance as a COM
    /// <see cref="UIA.ITextRangeProvider"/>. Unlike <see cref="AutomationNode"/>, instances of
    /// this class are not cached/identity-stable per peer — a fresh wrapper is created for every
    /// range returned from <see cref="AutomationNode"/>'s <c>ITextProvider</c> methods and from
    /// range-producing members below, mirroring how UI Automation text ranges are themselves
    /// transient value-like objects.
    /// </summary>
    [GeneratedComClass]
    internal partial class Win32TextRangeProvider : UIA.ITextRangeProvider
    {
        private readonly AutomationNode _owner;
        private readonly AAP.ITextRangeProvider _inner;

        public Win32TextRangeProvider(AutomationNode owner, AAP.ITextRangeProvider inner)
        {
            _owner = owner;
            _inner = inner;
        }

        private T Invoke<T>(Func<AAP.ITextRangeProvider, T> func)
        {
            try
            {
                return Win32DispatcherHelper.InvokeSync(() => func(_inner));
            }
            catch (AggregateException e) when (e.InnerException is ElementNotEnabledException)
            {
                throw new COMException(e.Message, UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED);
            }
        }

        private void Invoke(Action<AAP.ITextRangeProvider> action) => Invoke<object?>(x => { action(x); return null; });

        UIA.ITextRangeProvider UIA.ITextRangeProvider.Clone() =>
            new Win32TextRangeProvider(_owner, Invoke(x => x.Clone()));

        bool UIA.ITextRangeProvider.Compare(UIA.ITextRangeProvider range) =>
            Invoke(x => x.Compare(Unwrap(range)));

        int UIA.ITextRangeProvider.CompareEndpoints(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange, UIA.TextPatternRangeEndpoint targetEndpoint) =>
            Invoke(x => x.CompareEndpoints((AAP.TextPatternRangeEndpoint)endpoint, Unwrap(targetRange), (AAP.TextPatternRangeEndpoint)targetEndpoint));

        void UIA.ITextRangeProvider.ExpandToEnclosingUnit(UIA.TextUnit unit) =>
            Invoke(x => x.ExpandToEnclosingUnit((AAP.TextUnit)unit));

        UIA.ITextRangeProvider UIA.ITextRangeProvider.FindAttribute(int attribute, object value, bool backward)
        {
            var result = Invoke(x => x.FindAttribute(attribute, value, backward));
            // UIA permits a null return to mean "not found"; the interop interface isn't
            // annotated nullable, so the null-forgiving operator suppresses the mismatch.
            return result is null ? null! : new Win32TextRangeProvider(_owner, result);
        }

        UIA.ITextRangeProvider UIA.ITextRangeProvider.FindText(string text, bool backward, bool ignoreCase)
        {
            var result = Invoke(x => x.FindText(text, backward, ignoreCase));
            return result is null ? null! : new Win32TextRangeProvider(_owner, result);
        }

        object UIA.ITextRangeProvider.GetAttributeValue(int attribute)
        {
            var value = Invoke(x => x.GetAttributeValue(attribute));
            if (value is not AAP.AutomationTextAttributeNotSupported)
                return value;

            // A plain sentinel like -1 gets misread by at least NVDA as a real (truthy)
            // attribute value rather than "not supported" — e.g. for the hyperlink attribute,
            // that makes NVDA announce a plain TextBox as a link. Return UIA's actual reserved
            // "not supported" COM value instead.
            var hr = UiaCoreProviderApi.UiaGetReservedNotSupportedValue(out var punk);
            if (hr < 0 || punk == IntPtr.Zero)
                return -1;

            try
            {
#pragma warning disable CA1416 // This whole assembly only ever builds/runs on Windows.
                return Marshal.GetObjectForIUnknown(punk);
#pragma warning restore CA1416
            }
            finally
            {
                Marshal.Release(punk);
            }
        }

        double[] UIA.ITextRangeProvider.GetBoundingRectangles()
        {
            var rects = Invoke(x => x.GetBoundingRectangles());
            var flat = new double[rects.Count * 4];

            for (var i = 0; i < rects.Count; i++)
            {
                var screen = _owner.Peer.ToScreen(rects[i]) ?? default;
                flat[i * 4 + 0] = screen.X;
                flat[i * 4 + 1] = screen.Y;
                flat[i * 4 + 2] = screen.Width;
                flat[i * 4 + 3] = screen.Height;
            }

            return flat;
        }

        UIA.IRawElementProviderSimple UIA.ITextRangeProvider.GetEnclosingElement() =>
            AutomationNode.GetOrCreate(Invoke(x => x.GetEnclosingElement()));

        string UIA.ITextRangeProvider.GetText(int maxLength) => Invoke(x => x.GetText(maxLength));

        int UIA.ITextRangeProvider.Move(UIA.TextUnit unit, int count) =>
            Invoke(x => x.Move((AAP.TextUnit)unit, count));

        int UIA.ITextRangeProvider.MoveEndpointByUnit(UIA.TextPatternRangeEndpoint endpoint, UIA.TextUnit unit, int count) =>
            Invoke(x => x.MoveEndpointByUnit((AAP.TextPatternRangeEndpoint)endpoint, (AAP.TextUnit)unit, count));

        void UIA.ITextRangeProvider.MoveEndpointByRange(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange, UIA.TextPatternRangeEndpoint targetEndpoint) =>
            Invoke(x => x.MoveEndpointByRange((AAP.TextPatternRangeEndpoint)endpoint, Unwrap(targetRange), (AAP.TextPatternRangeEndpoint)targetEndpoint));

        void UIA.ITextRangeProvider.Select() => Invoke(x => x.Select());
        void UIA.ITextRangeProvider.AddToSelection() => Invoke(x => x.AddToSelection());
        void UIA.ITextRangeProvider.RemoveFromSelection() => Invoke(x => x.RemoveFromSelection());
        void UIA.ITextRangeProvider.ScrollIntoView(bool alignToTop) => Invoke(x => x.ScrollIntoView(alignToTop));

        UIA.IRawElementProviderSimple[] UIA.ITextRangeProvider.GetChildren() =>
            Invoke(x => x.GetChildren()).Select(p => (UIA.IRawElementProviderSimple)AutomationNode.GetOrCreate(p)).ToArray();

        private static AAP.ITextRangeProvider Unwrap(UIA.ITextRangeProvider range) => ((Win32TextRangeProvider)range)._inner;
    }
}
