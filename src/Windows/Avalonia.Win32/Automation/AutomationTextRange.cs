using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Win32.Interop.Automation;
using AAP = Avalonia.Automation.Provider;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal class AutomationTextRange : ITextRangeProvider
    {
        private readonly AutomationNode _owner;
        private readonly AAP.ITextRangeProvider _inner;

        public AutomationTextRange(
            AutomationNode owner,
            AAP.ITextRangeProvider inner)
        {
            _owner = owner;
            _inner = inner;
        }

        public void AddToSelection()
        {
            _owner.InvokeSync(() => _inner.AddToSelection());
        }

        public ITextRangeProvider Clone()
        {
            return new AutomationTextRange(_owner, _owner.InvokeSync(() => _inner.Clone())!);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        public bool Compare(ITextRangeProvider range)
        {
            return _owner.InvokeSync(() => _inner.Compare(((AutomationTextRange)range)._inner));
        }

        public int CompareEndpoints(
            TextPatternRangeEndpoint endpoint,
            ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint)
        {
            return _owner.InvokeSync(() =>
                _inner.CompareEndpoints(
                    (AAP.TextPatternRangeEndpoint)endpoint,
                    ((AutomationTextRange)targetRange)._inner,
                    (AAP.TextPatternRangeEndpoint)targetEndpoint));
        }

        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            _owner.InvokeSync(() => _inner.ExpandToEnclosingUnit((AAP.TextUnit)unit));
        }

        public ITextRangeProvider? FindAttribute(
            TextPatternAttribute attribute,
            object? value,
            [MarshalAs(UnmanagedType.Bool)] bool backward)
        {
            // TODO: Implement.
            return null;
        }

        public ITextRangeProvider? FindText(
            string text,
            [MarshalAs(UnmanagedType.Bool)] bool backward,
            [MarshalAs(UnmanagedType.Bool)] bool ignoreCase)
        {
            var result = _owner.InvokeSync(() => _inner.FindText(text, backward, ignoreCase));
            return result is null ? null : new AutomationTextRange(_owner, result);
        }

        public object? GetAttributeValue(TextPatternAttribute attribute)
        {
            var property = attribute switch
            {
                TextPatternAttribute.IsReadOnlyAttributeId => TextBox.IsReadOnlyProperty,
                _ => null,
            };

            return property is not null ?
                _owner.InvokeSync(() => _inner.GetAttributeValue(property)) :
                null;
        }

        public double[] GetBoundingRectangles()
        {
            return _owner.InvokeSync(() =>
            {
                var rects = _inner.GetBoundingRectangles();
                var result = new double[rects.Count * 4];
                var root = _owner.GetRoot() as RootAutomationNode;

                if (root is object)
                {
                    for (var i = 0; i < rects.Count; i++)
                    {
                        var screenRect = root.ToScreen(rects[i]);
                        result[4 * i] = screenRect.X;
                        result[4 * i + 1] = screenRect.Y;
                        result[4 * i + 2] = screenRect.Width;
                        result[4 * i + 3] = screenRect.Height;
                    }
                }

                return result;
            });
        }

        public IRawElementProviderSimple[] GetChildren()
        {
            return _owner.InvokeSync(() =>
            {
                var children = _inner.GetChildren();
                var result = new IRawElementProviderSimple[children.Count];

                for (var i = 0; i < children.Count; i++)
                    result[i] = AutomationNode.GetOrCreate(children[i]);

                return result;
            });
        }

        public IRawElementProviderSimple GetEnclosingElement()
        {
            var result = _owner.InvokeSync(() => _inner.GetEnclosingElement());
            return AutomationNode.GetOrCreate(result);
        }

        public string GetText(int maxLength)
        {
            return _owner.InvokeSync(() => _inner.GetText(maxLength));
        }

        public int Move(TextUnit unit, int count)
        {
            return _owner.InvokeSync(() => _inner.Move((AAP.TextUnit)unit, count));
        }

        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            _owner.InvokeSync(() =>_inner.MoveEndpointByRange(
                (AAP.TextPatternRangeEndpoint)endpoint,
                ((AutomationTextRange)targetRange)._inner,
                (AAP.TextPatternRangeEndpoint)targetEndpoint));
        }

        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            return _owner.InvokeSync(() => _inner.MoveEndpointByUnit(
                (AAP.TextPatternRangeEndpoint)endpoint,
                (AAP.TextUnit)unit,
                count));
        }

        public void RemoveFromSelection()
        {
            _owner.InvokeSync(() => _inner.RemoveFromSelection());
        }

        public void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop)
        {
            _owner.InvokeSync(() => _inner.ScrollIntoView(alignToTop));
        }

        public void Select()
        {
            _owner.InvokeSync(() => _inner.Select());
        }
    }
}
