// Ported from https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite/Panels/RelativePanel.cs

namespace Avalonia.Controls
{
    public partial class RelativePanel
    {
        enum Constraints
        {
            None = 0x00000,
            LeftOf = 0x00001,
            Above = 0x00002,
            RightOf = 0x00004,
            Below = 0x00008,
            AlignHorizontalCenterWith = 0x00010,
            AlignVerticalCenterWith = 0x00020,
            AlignLeftWith = 0x00040,
            AlignTopWith = 0x00080,
            AlignRightWith = 0x00100,
            AlignBottomWith = 0x00200,
            AlignLeftWithPanel = 0x00400,
            AlignTopWithPanel = 0x00800,
            AlignRightWithPanel = 0x01000,
            AlignBottomWithPanel = 0x02000,
            AlignHorizontalCenterWithPanel = 0x04000,
            AlignVerticalCenterWithPanel = 0x08000
        }

        enum State
        {
            Unresolved = 0x00,
            Pending = 0x01,
            Measured = 0x02,
            ArrangedHorizontally = 0x04,
            ArrangedVertically = 0x08,
            Arranged = 0x0C
        }
    }
}
