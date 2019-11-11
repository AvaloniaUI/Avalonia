using System;
using Avalonia.Traversal;

namespace Avalonia.VisualTree
{
    /// <summary>
    /// General purpose traversal operation on a visual tree.
    /// </summary>
    public static class VisualTreeOperations
    {
        public static void VisitDescendants<T>(IVisual target, T visitor, TreeVisitMode mode = default)
            where T : struct, ITreeVisitor<IVisual>
        {
            var traverser = new VisualTreeTraverser<T>(visitor);

            traverser.VisitDescendants(target, mode);
        }

        public static TResult VisitDescendants<T, TResult>(IVisual target, T visitor, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            var traverser = new VisualTreeTraverser<T>(visitor);

            traverser.VisitDescendants(target, mode);

            return traverser.Visitor.Result;
        }

        public static void VisitDescendants<T>(IVisual target, TreeVisitMode mode = default)
            where T : struct, ITreeVisitor<IVisual>
        {
            VisitDescendants<T>(target, default, mode);
        }

        public static TResult VisitDescendants<T, TResult>(IVisual target, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            return VisitDescendants<T, TResult>(target, default, mode);
        }

        public static void VisitDescendants(IVisual target, Func<IVisual, TreeVisit> visitFunc, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitor(visitFunc);

            VisitDescendants(target, visitor, mode);
        }

        public static void VisitDescendants<TState>(IVisual target, Func<IVisual, TState, TreeVisit> visitFunc, TState state, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitor(visitFunc, state);

            VisitDescendants(target, visitor, mode);
        }

        public static TResult FindDescendant<T, TResult>(IVisual target, T visitor, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            var traverser = new VisualTreeTraverser<T>(visitor);

            traverser.VisitDescendants(target, mode);

            return traverser.Visitor.Result;
        }

        public static TResult FindDescendant<T, TResult>(IVisual target, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            return FindDescendant<T, TResult>(target, default, mode);
        }

        public static IVisual FindDescendant(IVisual target, Func<IVisual, TreeVisit> visitFunc, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitorWithResult(visitFunc);

            return FindDescendant<LambdaTreeVisitorWithResult<IVisual>, IVisual>(target, visitor, mode);
        }

        public static IVisual FindDescendant<TState>(IVisual target, Func<IVisual, TState, TreeVisit> visitFunc, TState state, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitorWithResult(visitFunc, state);

            return FindDescendant<StatefulLambdaTreeVisitorWithResult<IVisual, TState>, IVisual>(target, visitor, mode);
        }

        public static T FindDescendantOfType<T>(IVisual target, TreeVisitMode mode = default)
        {
            return FindDescendant<OfTypeFilter<T>, T>(target, mode);
        }

        public static void VisitAncestors<T>(IVisual target, T visitor, TreeVisitMode mode = default)
            where T : struct, ITreeVisitor<IVisual>
        {
            var traverser = new VisualTreeTraverser<T>(visitor);

            traverser.VisitAncestors(target, mode);
        }

        public static void VisitAncestors<T>(IVisual target, TreeVisitMode mode = default)
            where T : struct, ITreeVisitor<IVisual>
        {
            VisitAncestors<T>(target, default, mode);
        }

        public static void VisitAncestors(IVisual target, Func<IVisual, TreeVisit> visitFunc, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitor(visitFunc);

            VisitAncestors(target, visitor, mode);
        }

        public static void VisitAncestors<TState>(IVisual target, Func<IVisual, TState, TreeVisit> visitFunc, TState state, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitor(visitFunc, state);

            VisitAncestors(target, visitor, mode);
        }

        public static TResult FindAncestor<T, TResult>(IVisual target, T visitor, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            var traverser = new VisualTreeTraverser<T>(visitor);

            traverser.VisitAncestors(target, mode);

            return traverser.Visitor.Result;
        }

        public static TResult FindAncestor<T, TResult>(IVisual target, TreeVisitMode mode = default)
            where T : struct, ITreeVisitorWithResult<IVisual, TResult>
        {
            return FindAncestor<T, TResult>(target, default, mode);
        }

        public static IVisual FindAncestor(IVisual target, Func<IVisual, TreeVisit> visitFunc, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitorWithResult(visitFunc);

            return FindAncestor<LambdaTreeVisitorWithResult<IVisual>, IVisual>(target, visitor, mode);
        }

        public static IVisual FindAncestor<TState>(IVisual target, Func<IVisual, TState, TreeVisit> visitFunc, TState state, TreeVisitMode mode = default)
        {
            var visitor = MakeLambdaVisitorWithResult(visitFunc, state);

            return FindAncestor<StatefulLambdaTreeVisitorWithResult<IVisual, TState>, IVisual>(target, visitor, mode);
        }

        public static T FindAncestorOfType<T>(IVisual target, TreeVisitMode mode = default)
        {
            return FindAncestor<OfTypeFilter<T>, T>(target, mode);
        }

        public static LambdaTreeVisitor<IVisual> MakeLambdaVisitor(Func<IVisual, TreeVisit> visitFunc)
        {
            if (visitFunc is null)
            {
                throw new ArgumentNullException(nameof(visitFunc));
            }

            return new LambdaTreeVisitor<IVisual>(visitFunc);
        }

        public static StatefulLambdaTreeVisitor<IVisual, TState> MakeLambdaVisitor<TState>(Func<IVisual, TState, TreeVisit> visitFunc, TState state)
        {
            if (visitFunc is null)
            {
                throw new ArgumentNullException(nameof(visitFunc));
            }

            return new StatefulLambdaTreeVisitor<IVisual, TState>(visitFunc, state);
        }

        public static LambdaTreeVisitorWithResult<T> MakeLambdaVisitorWithResult<T>(Func<T, TreeVisit> visitFunc)
        {
            if (visitFunc is null)
            {
                throw new ArgumentNullException(nameof(visitFunc));
            }

            return new LambdaTreeVisitorWithResult<T>(visitFunc);
        }

        public static StatefulLambdaTreeVisitorWithResult<T, TState> MakeLambdaVisitorWithResult<T, TState>(Func<T, TState, TreeVisit> visitFunc, TState state)
        {
            if (visitFunc is null)
            {
                throw new ArgumentNullException(nameof(visitFunc));
            }

            return new StatefulLambdaTreeVisitorWithResult<T, TState>(visitFunc, state);
        }

        private struct OfTypeFilter<T> : ITreeVisitorWithResult<IVisual, T>
        {
            public TreeVisit Visit(IVisual target)
            {
                if (target is T result)
                {
                    Result = result;

                    return TreeVisit.Stop;
                }

                return TreeVisit.Continue;
            }

            public T Result { get; private set; }
        }
    }

    public struct VisualTreeTraverser<T> where T : struct, ITreeVisitor<IVisual>
    {
        private T _visitor;

        public VisualTreeTraverser(T visitor)
        {
            _visitor = visitor;
        }

        public T Visitor => _visitor;

        public void VisitAncestors(IVisual target, TreeVisitMode mode)
        {
            if (mode == TreeVisitMode.IncludeSelf)
            {
                if (_visitor.Visit(target) == TreeVisit.Stop)
                {
                    return;
                }
            }

            IVisual parent = target.VisualParent;

            while (parent != null)
            {
                if (_visitor.Visit(parent) == TreeVisit.Stop)
                {
                    return;
                }

                parent = parent.VisualParent;
            }
        }

        public void VisitDescendants(IVisual target, TreeVisitMode mode)
        {
            if (mode == TreeVisitMode.IncludeSelf)
            {
                if (_visitor.Visit(target) == TreeVisit.Stop)
                {
                    return;
                }
            }

            TraverseChildren(target);
        }

        private bool VisitDescendants(IVisual target)
        {
            if (_visitor.Visit(target) == TreeVisit.Stop)
            {
                return false;
            }

            return TraverseChildren(target);
        }

        private bool TraverseChildren(IVisual target)
        {
            var visualChildren = target.VisualChildren;
            var visualCount = visualChildren.Count;

            for (int i = 0; i < visualCount; i++)
            {
                var child = visualChildren[i];

                if (!VisitDescendants(child))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
