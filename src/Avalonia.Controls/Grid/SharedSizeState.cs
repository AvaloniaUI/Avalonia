// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implementation of per shared group state object
    /// </summary>
    internal class SharedSizeState
    {
        private readonly SharedSizeScope _sharedSizeScope;  //  the scope this state belongs to
        private readonly string _sharedSizeGroupId;         //  Id of the shared size group this object is servicing
        private readonly List<DefinitionBase> _registry;    //  registry of participating definitions
        private readonly EventHandler _layoutUpdated;       //  instance event handler for layout updated event
        private Control _layoutUpdatedHost;               //  Control for which layout updated event handler is registered
        private bool _broadcastInvalidation;                //  "true" when broadcasting of invalidation is needed
        private bool _userSizeValid;                        //  "true" when _userSize is up to date
        private GridLength _userSize;                       //  shared state
        private double _minSize;                            //  shared state

        /// <summary>
        /// Default ctor.
        /// </summary>
        internal SharedSizeState(SharedSizeScope sharedSizeScope, string sharedSizeGroupId)
        {
            Debug.Assert(sharedSizeScope != null && sharedSizeGroupId != null);
            _sharedSizeScope = sharedSizeScope;
            _sharedSizeGroupId = sharedSizeGroupId;
            _registry = new List<DefinitionBase>();
            _layoutUpdated = new EventHandler(OnLayoutUpdated);
            _broadcastInvalidation = true;
        }

        /// <summary>
        /// Adds / registers a definition instance.
        /// </summary>
        internal void AddMember(DefinitionBase member)
        {
            Debug.Assert(!_registry.Contains(member));
            _registry.Add(member);
            Invalidate();
        }

        /// <summary>
        /// Removes / un-registers a definition instance.
        /// </summary>
        /// <remarks>
        /// If the collection of registered definitions becomes empty
        /// instantiates self removal from owner's collection.
        /// </remarks>
        internal void RemoveMember(DefinitionBase member)
        {
            Invalidate();
            _registry.Remove(member);

            if (_registry.Count == 0)
            {
                _sharedSizeScope.Remove(_sharedSizeGroupId);
            }
        }

        /// <summary>
        /// Propogates invalidations for all registered definitions.
        /// Resets its own state.
        /// </summary>
        internal void Invalidate()
        {
            _userSizeValid = false;

            if (_broadcastInvalidation)
            {
                for (int i = 0, count = _registry.Count; i < count; ++i)
                {
                    Grid parentGrid = (Grid)(_registry[i].Parent);
                    parentGrid.Invalidate();
                }
                _broadcastInvalidation = false;
            }
        }

        /// <summary>
        /// Makes sure that one and only one layout updated handler is registered for this shared state.
        /// </summary>
        internal void EnsureDeferredValidation(Control layoutUpdatedHost)
        {
            if (_layoutUpdatedHost == null)
            {
                _layoutUpdatedHost = layoutUpdatedHost;
                _layoutUpdatedHost.LayoutUpdated += _layoutUpdated;
            }
        }

        /// <summary>
        /// DefinitionBase's specific code.
        /// </summary>
        internal double MinSize
        {
            get
            {
                if (!_userSizeValid) { EnsureUserSizeValid(); }
                return (_minSize);
            }
        }

        /// <summary>
        /// DefinitionBase's specific code.
        /// </summary>
        internal GridLength UserSize
        {
            get
            {
                if (!_userSizeValid) { EnsureUserSizeValid(); }
                return (_userSize);
            }
        }

        private void EnsureUserSizeValid()
        {
            _userSize = new GridLength(1, GridUnitType.Auto);

            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                Debug.Assert(_userSize.GridUnitType == GridUnitType.Auto
                            || _userSize.GridUnitType == GridUnitType.Pixel);

                GridLength currentGridLength = _registry[i].UserSizeValueCache;
                if (currentGridLength.GridUnitType == GridUnitType.Pixel)
                {
                    if (_userSize.GridUnitType == GridUnitType.Auto)
                    {
                        _userSize = currentGridLength;
                    }
                    else if (_userSize.Value < currentGridLength.Value)
                    {
                        _userSize = currentGridLength;
                    }
                }
            }
            //  taking maximum with user size effectively prevents squishy-ness.
            //  this is a "solution" to avoid shared definitions from been sized to
            //  different final size at arrange time, if / when different grids receive
            //  different final sizes.
            _minSize = _userSize.IsAbsolute ? _userSize.Value : 0.0;

            _userSizeValid = true;
        }

        /// <summary>
        /// OnLayoutUpdated handler. Validates that all participating definitions
        /// have updated min size value. Forces another layout update cycle if needed.
        /// </summary>
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            double sharedMinSize = 0;

            //  accumulate min size of all participating definitions
            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                sharedMinSize = Math.Max(sharedMinSize, _registry[i].MinSize);
            }

            bool sharedMinSizeChanged = !MathUtilities.AreClose(_minSize, sharedMinSize);

            //  compare accumulated min size with min sizes of the individual definitions
            for (int i = 0, count = _registry.Count; i < count; ++i)
            {
                DefinitionBase definitionBase = _registry[i];

                if (sharedMinSizeChanged || definitionBase.LayoutWasUpdated)
                {
                    //  if definition's min size is different, then need to re-measure
                    if (!MathUtilities.AreClose(sharedMinSize, definitionBase.MinSize))
                    {
                        Grid parentGrid = (Grid)definitionBase.Parent;
                        parentGrid.InvalidateMeasure();
                        definitionBase.UseSharedMinimum = true;
                    }
                    else
                    {
                        definitionBase.UseSharedMinimum = false;

                        //  if measure is valid then also need to check arrange.
                        //  Note: definitionBase.SizeCache is volatile but at this point 
                        //  it contains up-to-date final size
                        if (!MathUtilities.AreClose(sharedMinSize, definitionBase.SizeCache))
                        {
                            Grid parentGrid = (Grid)definitionBase.Parent;
                            parentGrid.InvalidateArrange();
                        }
                    }

                    definitionBase.LayoutWasUpdated = false;
                }
            }

            _minSize = sharedMinSize;

            _layoutUpdatedHost.LayoutUpdated -= _layoutUpdated;
            _layoutUpdatedHost = null;

            _broadcastInvalidation = true;
        }
    }
}