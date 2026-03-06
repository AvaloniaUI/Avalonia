using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Input.TextInput
{
    class TransformTrackingHelper : IDisposable
    {
        private readonly bool _deferAfterRenderPass;
        private Visual? _visual;
        private bool _queuedForUpdate;
        private readonly EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChangedHandler;
        private readonly List<Visual> _propertyChangedSubscriptions = new List<Visual>();
        
        public TransformTrackingHelper(bool deferAfterRenderPass)
        {
            _deferAfterRenderPass = deferAfterRenderPass;
            _propertyChangedHandler = PropertyChangedHandler;
        }

        public void SetVisual(Visual? visual)
        {
            Dispose();
            _visual = visual;
            if (visual != null)
            {
                visual.AttachedToVisualTree += OnAttachedToVisualTree;
                visual.DetachedFromVisualTree += OnDetachedFromVisualTree;
                if (visual.IsAttachedToVisualTree)
                    SubscribeToParents();
                UpdateMatrix();
            }
        }
        
        public Matrix? Matrix { get; private set; }
        public event Action? MatrixChanged;
        
        public void Dispose()
        {
            if(_visual == null)
                return;
            UnsubscribeFromParents();
            _visual.AttachedToVisualTree -= OnAttachedToVisualTree;
            _visual.DetachedFromVisualTree -= OnDetachedFromVisualTree;
            _visual = null;
        }

        private void SubscribeToParents()
        {
            var visual = _visual;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // false positive
            while (visual != null)
            {
                if (visual is Visual v)
                {
                    v.PropertyChanged += _propertyChangedHandler;
                    _propertyChangedSubscriptions.Add(v);
                }

                visual = visual.VisualParent;
            }
        }

        private void UnsubscribeFromParents()
        {
            foreach (var v in _propertyChangedSubscriptions)
                v.PropertyChanged -= _propertyChangedHandler;
            _propertyChangedSubscriptions.Clear();
        }

        void UpdateMatrix()
        {
            _queuedForUpdate = false;
            Matrix? matrix = null;
            if (_visual != null && _visual.VisualRoot != null)
                matrix = _visual.TransformToVisual((Visual)_visual.VisualRoot);
            if (Matrix != matrix)
            {
                Matrix = matrix;
                MatrixChanged?.Invoke();
            }
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
        {
            SubscribeToParents();
            UpdateMatrix();
        }

        private void EnqueueForUpdate()
        {
            if(_queuedForUpdate)
                return;
            _queuedForUpdate = true;
            if (_deferAfterRenderPass)
                Dispatcher.UIThread.Post(UpdateMatrix, DispatcherPriority.AfterRender);
            else
                MediaContext.Instance.BeginInvokeOnRender(UpdateMatrix);
        }

        private void PropertyChangedHandler(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.IsEffectiveValueChange && e.Property == Visual.BoundsProperty)
                EnqueueForUpdate();
        }

        private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs visualTreeAttachmentEventArgs)
        {
            UnsubscribeFromParents();
            UpdateMatrix();
        }

        public static IDisposable Track(Visual visual, bool deferAfterRenderPass, Action<Visual, Matrix?> cb)
        {
            var rv = new TransformTrackingHelper(deferAfterRenderPass);
            rv.MatrixChanged += () => cb(visual, rv.Matrix);
            rv.SetVisual(visual);
            return rv;
        }
        
        public static IObservable<Matrix?> Observe(Visual visual, bool deferAfterRenderPass)
        {
            return Observable.Create<Matrix?>(observer =>
            {
                var rv = new TransformTrackingHelper(deferAfterRenderPass);
                rv.MatrixChanged += () => observer.OnNext(rv.Matrix);
                rv.SetVisual(visual);
                return rv;
            });
        }
    }
}
