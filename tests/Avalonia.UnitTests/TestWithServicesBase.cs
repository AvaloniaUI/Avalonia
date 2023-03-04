using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;

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
            Dispatcher.UIThread.RunJobs();
            _scope.Dispose();
        }
    }
}
