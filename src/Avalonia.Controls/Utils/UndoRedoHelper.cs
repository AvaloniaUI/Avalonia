using System.Collections.Generic;

namespace Avalonia.Controls.Utils
{
    class UndoRedoHelper<TState>
    {
        public const int DefaultUndoLimit = 10;

        private readonly IUndoRedoHost _host;

        public interface IUndoRedoHost
        {
            TState UndoRedoState { get; set; }

            void OnUndoStackChanged();

            void OnRedoStackChanged();
        }

        private readonly LinkedList<TState> _states = new LinkedList<TState>();

        private LinkedListNode<TState>? _currentNode;

        /// <summary>
        /// Maximum number of states this helper can store for undo/redo.
        /// If -1, no limit is imposed.
        /// </summary>
        public int Limit { get; set; } = DefaultUndoLimit;

        public bool CanUndo => _currentNode?.Previous != null;

        public bool CanRedo => _currentNode?.Next != null;

        public UndoRedoHelper(IUndoRedoHost host)
        {
            _host = host;
        }

        public void Undo()
        {
            if (_currentNode?.Previous != null)
            {
                _currentNode = _currentNode.Previous;
                _host.UndoRedoState = _currentNode.Value;
                _host.OnUndoStackChanged();
                _host.OnRedoStackChanged();
            }
        }

        public bool IsLastState => _currentNode != null && _currentNode.Next == null;

        public bool TryGetLastState(out TState? _state)
        {
            _state = default;
            if (!IsLastState)
                return false;

            _state = _currentNode!.Value;
            return true;
        }

        public bool HasState => _currentNode != null;

        public void UpdateLastState(TState state)
        {
            if (_states.Last != null)
            {
                _states.Last.Value = state;
            }
        }

        public void UpdateLastState()
        {
            UpdateLastState(_host.UndoRedoState);
        }

        public void DiscardRedo()
        {
            while (_currentNode?.Next != null)
                _states.Remove(_currentNode.Next);

            _host.OnRedoStackChanged();
        }

        public void Redo()
        {
            if (_currentNode?.Next != null)
            {
                _currentNode = _currentNode.Next;
                _host.UndoRedoState = _currentNode.Value;
                _host.OnRedoStackChanged();
                _host.OnUndoStackChanged();
            }
        }

        public void Snapshot()
        {
            var current = _host.UndoRedoState;
            if (_currentNode == null || !_currentNode.Value!.Equals(current))
            {
                if (_currentNode?.Next != null)
                    DiscardRedo();
                _states.AddLast(current);
                _currentNode = _states.Last;
                if (Limit != -1 && _states.Count > Limit)
                    _states.RemoveFirst();

                _host.OnUndoStackChanged();
                _host.OnRedoStackChanged();
            }
        }

        public void Clear()
        {
            _states.Clear();
            _currentNode = null;

            _host.OnUndoStackChanged();
            _host.OnRedoStackChanged();
        }
    }
}
