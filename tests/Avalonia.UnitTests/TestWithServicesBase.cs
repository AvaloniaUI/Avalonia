using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.UnitTests
{
    public class TestWithServicesBase : IDisposable
    {
        private IDisposable _scope;

        public TestWithServicesBase()
        {
            _scope = AvaloniaLocator.EnterScope();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}
