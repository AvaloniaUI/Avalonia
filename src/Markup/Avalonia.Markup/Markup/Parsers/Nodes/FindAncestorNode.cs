﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;

namespace Avalonia.Markup.Parsers.Nodes
{
    internal class FindAncestorNode : ExpressionNode
    {
        private readonly int _level;
        private readonly Type _ancestorType;
        private IDisposable _subscription;

        public FindAncestorNode(Type ancestorType, int level)
        {
            _level = level;
            _ancestorType = ancestorType;
        }

        public override string Description
        {
            get
            {
                if (_ancestorType == null)
                {
                    return $"$parent[{_level}]";
                }
                else
                {
                    return $"$parent[{_ancestorType.Name}, {_level}]";
                }
            }
        }

        protected override void StartListeningCore(WeakReference reference)
        {
            if (reference.Target is ILogical logical)
            {
                _subscription = ControlLocator.Track(logical, _level, _ancestorType).Subscribe(ValueChanged);
            }
            else
            {
                _subscription = null;
            }
        }

        protected override void StopListeningCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
