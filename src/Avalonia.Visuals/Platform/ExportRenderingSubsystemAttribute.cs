using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Platform
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ExportRenderingSubsystemAttribute : Attribute
    {
        public ExportRenderingSubsystemAttribute(OperatingSystemType requiredOS, int priority, string name, Type initializationType, string initializationMethod,
            Type environmentChecker = null)
        {
            Name = name;
            InitializationType = initializationType;
            InitializationMethod = initializationMethod;
            EnvironmentChecker = environmentChecker;
            RequiredOS = requiredOS;
            Priority = priority;
        }

        public string InitializationMethod { get; private set; }
        public Type EnvironmentChecker { get; }
        public Type InitializationType { get; private set; }
        public string Name { get; private set; }
        public int Priority { get; private set; }
        public OperatingSystemType RequiredOS { get; private set; }
        public string RequiresWindowingSubsystem { get; set; }
    }
}
