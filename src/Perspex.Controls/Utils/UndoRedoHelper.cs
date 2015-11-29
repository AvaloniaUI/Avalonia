using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Perspex.Utilities;

namespace Perspex.Controls.Utils
{
    class UndoRedoHelper<TState> : WeakTimer.IWeakTimerSubscriber where TState : IEquatable<TState>
    {
        private readonly IUndoRedoHost _host;

        public interface IUndoRedoHost
        {
             TState UndoRedoState { get; set; }
        }
        


        private readonly LinkedList<TState> _states = new LinkedList<TState>();

        [NotNull]
        private LinkedListNode<TState> _currentNode;

        public int Limit { get; set; } = 10;

        public UndoRedoHelper(IUndoRedoHost host)
        {
            _host = host;
            _states.AddFirst(_host.UndoRedoState);
            _currentNode = _states.First;
            WeakTimer.StartWeakTimer(this, new TimeSpan(0, 0, 1));

        }

        public void Undo()
        {
            _host.UndoRedoState= (_currentNode = _currentNode?.Previous ?? _currentNode).Value;
        }

        public bool IsLastState => _currentNode.Next == null;

        public void UpdateLastState(TState state)
        {
            _states.Last.Value = state;
        }

        public void UpdateLastState()
        {
            _states.Last.Value = _host.UndoRedoState;
        }

        public TState LastState => _currentNode.Value;

        public void DiscardRedo()
        {
            //Linked list sucks, so we are doing this
            while (_currentNode.Next != null)
                _states.Remove(_currentNode.Next);
        }

        public void Redo()
        {
            _host.UndoRedoState = (_currentNode = _currentNode?.Next ?? _currentNode).Value;
        }

        public void Snapshot()
        {
            var current = _host.UndoRedoState;
            if (!_currentNode.Value.Equals(current))
            {
                if(_currentNode.Next != null)
                    DiscardRedo();
                _states.AddLast(current);
                _currentNode = _states.Last;
                if(_states.Count > Limit)
                    _states.RemoveFirst();
            }
        }

        bool WeakTimer.IWeakTimerSubscriber.Tick()
        {
            Snapshot();
            return true;
        }
    }
}
