using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls;
using Perspex.Input;

namespace Perspex.MobilePlatform.Fakes
{
    class FakeInputRoot : Control, IInputRoot
    {
        public IAccessKeyHandler AccessKeyHandler { get; }
        public IKeyboardNavigationHandler KeyboardNavigationHandler { get; }
        public IInputElement PointerOverElement { get; set; }
        public bool ShowAccessKeys { get; set; }
        
    }
}
