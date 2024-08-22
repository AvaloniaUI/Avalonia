// Ported from https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite/Panels/RelativePanel.cs

namespace Avalonia.Controls
{
    public partial class RelativePanel
    {
        private class GraphNode
        {
            private readonly Control m_element;
            private State m_state;
            private Constraints m_constraints;
            internal bool m_isHorizontalLeaf;
            internal bool m_isVerticalLeaf;
            internal GraphNode? m_leftOfNode;
            internal GraphNode? m_aboveNode;
            internal GraphNode? m_rightOfNode;
            internal GraphNode? m_belowNode;
            internal GraphNode? m_alignHorizontalCenterWithNode;
            internal GraphNode? m_alignVerticalCenterWithNode;
            internal GraphNode? m_alignLeftWithNode;
            internal GraphNode? m_alignTopWithNode;
            internal GraphNode? m_alignRightWithNode;
            internal GraphNode? m_alignBottomWithNode;
            internal UnsafeRect m_measureRect;
            internal UnsafeRect m_arrangeRect;

            public Control Element => m_element;

            public GraphNode(Control element)
            {
                m_element = element;
                m_state = State.Unresolved;
                m_isHorizontalLeaf = true;
                m_isVerticalLeaf = true;
                m_constraints = Constraints.None;
                m_leftOfNode = default(GraphNode);
                m_aboveNode = default(GraphNode);
                m_rightOfNode = default(GraphNode);
                m_belowNode = default(GraphNode);
                m_alignHorizontalCenterWithNode = default(GraphNode);
                m_alignVerticalCenterWithNode = default(GraphNode);
                m_alignLeftWithNode = default(GraphNode);
                m_alignTopWithNode = default(GraphNode);
                m_alignRightWithNode = default(GraphNode);
                m_alignBottomWithNode = default(GraphNode);
            }


            // The node is said to be anchored when its ArrangeRect is expected to
            // align with its MeasureRect on one or more sides. For example, if the 
            // node is right-anchored, the right side of the ArrangeRect should overlap
            // with the right side of the MeasureRect. Anchoring is determined by
            // specific combinations of dependencies.
            public bool IsLeftAnchored
                => (IsAlignLeftWithPanel() || IsAlignLeftWith() || (IsRightOf() && !IsAlignHorizontalCenterWith()));

            public bool IsTopAnchored
                => (IsAlignTopWithPanel() || IsAlignTopWith() || (IsBelow() && !IsAlignVerticalCenterWith()));

            public bool IsRightAnchored
                => (IsAlignRightWithPanel() || IsAlignRightWith() || (IsLeftOf() && !IsAlignHorizontalCenterWith()));

            public bool IsBottomAnchored
                => (IsAlignBottomWithPanel() || IsAlignBottomWith() || (IsAbove() && !IsAlignVerticalCenterWith()));

            public bool IsHorizontalCenterAnchored
                => ((IsAlignHorizontalCenterWithPanel() && !IsAlignLeftWithPanel() && !IsAlignRightWithPanel() && !IsAlignLeftWith() && !IsAlignRightWith() && !IsLeftOf() && !IsRightOf())
                    || (IsAlignHorizontalCenterWith() && !IsAlignLeftWithPanel() && !IsAlignRightWithPanel() && !IsAlignLeftWith() && !IsAlignRightWith()));

            public bool IsVerticalCenterAnchored
                => ((IsAlignVerticalCenterWithPanel() && !IsAlignTopWithPanel() && !IsAlignBottomWithPanel() && !IsAlignTopWith() && !IsAlignBottomWith() && !IsAbove() && !IsBelow())
                    || (IsAlignVerticalCenterWith() && !IsAlignTopWithPanel() && !IsAlignBottomWithPanel() && !IsAlignTopWith() && !IsAlignBottomWith()));

            // RPState flag checks.
            public bool IsUnresolved() { return m_state == State.Unresolved; }
            public bool IsPending() { return (m_state & State.Pending) == State.Pending; }
            public bool IsMeasured() { return (m_state & State.Measured) == State.Measured; }
            public bool IsArrangedHorizontally() { return (m_state & State.ArrangedHorizontally) == State.ArrangedHorizontally; }
            public bool IsArrangedVertically() { return (m_state & State.ArrangedVertically) == State.ArrangedVertically; }
            public bool IsArranged() { return (m_state & State.Arranged) == State.Arranged; }

            public void SetPending(bool value)
            {
                if (value)
                {
                    m_state |= State.Pending;
                }
                else
                {
                    m_state &= ~State.Pending;
                }
            }
            public void SetMeasured(bool value)
            {
                if (value)
                {
                    m_state |= State.Measured;
                }
                else
                {
                    m_state &= ~State.Measured;
                }
            }
            public void SetArrangedHorizontally(bool value)
            {
                if (value)
                {
                    m_state |= State.ArrangedHorizontally;
                }
                else
                {
                    m_state &= ~State.ArrangedHorizontally;
                }
            }
            public void SetArrangedVertically(bool value)
            {
                if (value)
                {
                    m_state |= State.ArrangedVertically;
                }
                else
                {
                    m_state &= ~State.ArrangedVertically;
                }
            }

            // RPEdge flag checks.

            public bool IsLeftOf() { return (m_constraints & Constraints.LeftOf) == Constraints.LeftOf; }
            public bool IsAbove() { return (m_constraints & Constraints.Above) == Constraints.Above; }
            public bool IsRightOf() { return (m_constraints & Constraints.RightOf) == Constraints.RightOf; }
            public bool IsBelow() { return (m_constraints & Constraints.Below) == Constraints.Below; }
            public bool IsAlignHorizontalCenterWith() { return (m_constraints & Constraints.AlignHorizontalCenterWith) == Constraints.AlignHorizontalCenterWith; }
            public bool IsAlignVerticalCenterWith() { return (m_constraints & Constraints.AlignVerticalCenterWith) == Constraints.AlignVerticalCenterWith; }
            public bool IsAlignLeftWith() { return (m_constraints & Constraints.AlignLeftWith) == Constraints.AlignLeftWith; }
            public bool IsAlignTopWith() { return (m_constraints & Constraints.AlignTopWith) == Constraints.AlignTopWith; }
            public bool IsAlignRightWith() { return (m_constraints & Constraints.AlignRightWith) == Constraints.AlignRightWith; }
            public bool IsAlignBottomWith() { return (m_constraints & Constraints.AlignBottomWith) == Constraints.AlignBottomWith; }
            public bool IsAlignLeftWithPanel() { return (m_constraints & Constraints.AlignLeftWithPanel) == Constraints.AlignLeftWithPanel; }
            public bool IsAlignTopWithPanel() { return (m_constraints & Constraints.AlignTopWithPanel) == Constraints.AlignTopWithPanel; }
            public bool IsAlignRightWithPanel() { return (m_constraints & Constraints.AlignRightWithPanel) == Constraints.AlignRightWithPanel; }
            public bool IsAlignBottomWithPanel() { return (m_constraints & Constraints.AlignBottomWithPanel) == Constraints.AlignBottomWithPanel; }
            public bool IsAlignHorizontalCenterWithPanel() { return (m_constraints & Constraints.AlignHorizontalCenterWithPanel) == Constraints.AlignHorizontalCenterWithPanel; }
            public bool IsAlignVerticalCenterWithPanel() { return (m_constraints & Constraints.AlignVerticalCenterWithPanel) == Constraints.AlignVerticalCenterWithPanel; }

            public void SetLeftOfConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_leftOfNode = neighbor;
                    m_constraints |= Constraints.LeftOf;
                }
                else
                {
                    m_leftOfNode = null;
                    m_constraints &= ~Constraints.LeftOf;
                }
            }
            public void SetAboveConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_aboveNode = neighbor;
                    m_constraints |= Constraints.Above;
                }
                else
                {
                    m_aboveNode = null;
                    m_constraints &= ~Constraints.Above;
                }
            }
            public void SetRightOfConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_rightOfNode = neighbor;
                    m_constraints |= Constraints.RightOf;
                }
                else
                {
                    m_rightOfNode = null;
                    m_constraints &= ~Constraints.RightOf;
                }
            }
            public void SetBelowConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_belowNode = neighbor;
                    m_constraints |= Constraints.Below;
                }
                else
                {
                    m_belowNode = null;
                    m_constraints &= ~Constraints.Below;
                }
            }
            public void SetAlignHorizontalCenterWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignHorizontalCenterWithNode = neighbor;
                    m_constraints |= Constraints.AlignHorizontalCenterWith;
                }
                else
                {
                    m_alignHorizontalCenterWithNode = null;
                    m_constraints &= ~Constraints.AlignHorizontalCenterWith;
                }
            }
            public void SetAlignVerticalCenterWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignVerticalCenterWithNode = neighbor;
                    m_constraints |= Constraints.AlignVerticalCenterWith;
                }
                else
                {
                    m_alignVerticalCenterWithNode = null;
                    m_constraints &= ~Constraints.AlignVerticalCenterWith;
                }
            }
            public void SetAlignLeftWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignLeftWithNode = neighbor;
                    m_constraints |= Constraints.AlignLeftWith;
                }
                else
                {
                    m_alignLeftWithNode = null;
                    m_constraints &= ~Constraints.AlignLeftWith;
                }
            }
            public void SetAlignTopWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignTopWithNode = neighbor;
                    m_constraints |= Constraints.AlignTopWith;
                }
                else
                {
                    m_alignTopWithNode = null;
                    m_constraints &= ~Constraints.AlignTopWith;
                }
            }
            public void SetAlignRightWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignRightWithNode = neighbor;
                    m_constraints |= Constraints.AlignRightWith;
                }
                else
                {
                    m_alignRightWithNode = null;
                    m_constraints &= ~Constraints.AlignRightWith;
                }
            }
            public void SetAlignBottomWithConstraint(GraphNode? neighbor)
            {
                if (neighbor is not null)
                {
                    m_alignBottomWithNode = neighbor;
                    m_constraints |= Constraints.AlignBottomWith;
                }
                else
                {
                    m_alignBottomWithNode = null;
                    m_constraints &= ~Constraints.AlignBottomWith;
                }
            }
            public void SetAlignLeftWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignLeftWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignLeftWithPanel;
                }
            }
            public void SetAlignTopWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignTopWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignTopWithPanel;
                }
            }
            public void SetAlignRightWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignRightWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignRightWithPanel;
                }
            }
            public void SetAlignBottomWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignBottomWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignBottomWithPanel;
                }
            }
            public void SetAlignHorizontalCenterWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignHorizontalCenterWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignHorizontalCenterWithPanel;
                }
            }
            public void SetAlignVerticalCenterWithPanelConstraint(bool value)
            {
                if (value)
                {
                    m_constraints |= Constraints.AlignVerticalCenterWithPanel;
                }
                else
                {
                    m_constraints &= ~Constraints.AlignVerticalCenterWithPanel;
                }
            }

            public void UnmarkNeighborsAsHorizontalOrVerticalLeaves()
            {
                bool isHorizontallyCenteredFromLeft = false;
                bool isHorizontallyCenteredFromRight = false;
                bool isVerticallyCenteredFromTop = false;
                bool isVerticallyCenteredFromBottom = false;

                if (!IsAlignLeftWithPanel())
                {
                    if (IsAlignLeftWith())
                    {
                        m_alignLeftWithNode!.m_isHorizontalLeaf = false;
                    }
                    else if (IsAlignHorizontalCenterWith())
                    {
                        isHorizontallyCenteredFromLeft = true;
                    }
                    else if (IsRightOf())
                    {
                        m_rightOfNode!.m_isHorizontalLeaf = false;
                    }
                }

                if (!IsAlignTopWithPanel())
                {
                    if (IsAlignTopWith())
                    {
                        m_alignTopWithNode!.m_isVerticalLeaf = false;
                    }
                    else if (IsAlignVerticalCenterWith())
                    {
                        isVerticallyCenteredFromTop = true;
                    }
                    else if (IsBelow())
                    {
                        m_belowNode!.m_isVerticalLeaf = false;
                    }
                }

                if (!IsAlignRightWithPanel())
                {
                    if (IsAlignRightWith())
                    {
                        m_alignRightWithNode!.m_isHorizontalLeaf = false;
                    }
                    else if (IsAlignHorizontalCenterWith())
                    {
                        isHorizontallyCenteredFromRight = true;
                    }
                    else if (IsLeftOf())
                    {
                        m_leftOfNode!.m_isHorizontalLeaf = false;
                    }
                }

                if (!IsAlignBottomWithPanel())
                {
                    if (IsAlignBottomWith())
                    {
                        m_alignBottomWithNode!.m_isVerticalLeaf = false;
                    }
                    else if (IsAlignVerticalCenterWith())
                    {
                        isVerticallyCenteredFromBottom = true;
                    }
                    else if (IsAbove())
                    {
                        m_aboveNode!.m_isVerticalLeaf = false;
                    }
                }

                if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
                {
                    m_alignHorizontalCenterWithNode!.m_isHorizontalLeaf = false;
                }

                if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
                {
                    m_alignVerticalCenterWithNode!.m_isVerticalLeaf = false;
                }
            }

        }
    }
}
