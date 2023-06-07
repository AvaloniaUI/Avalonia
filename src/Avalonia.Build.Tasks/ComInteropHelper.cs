using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using XamlX.TypeSystem;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Avalonia.Build.Tasks
{
    static class ComInteropHelper
    {
        public static void PatchAssembly(AssemblyDefinition asm, CecilTypeSystem typeSystem)
        {
            var classToRemoveList = new List<TypeDefinition>();
            var initializers = new List<MethodDefinition>();
            foreach (var type in asm.MainModule.Types)
            {
                var i = type.Methods.FirstOrDefault(m => m.Name == "__MicroComModuleInit");
                if (i != null)
                    initializers.Add(i);

                PatchType(type, classToRemoveList);
            }

            // Remove All Interop classes
            foreach (var type in classToRemoveList)
                asm.MainModule.Types.Remove(type);

            
            // Patch automatic registrations
            if (initializers.Count != 0)
            {
                var moduleType = asm.MainModule.Types.First(x => x.Name == "<Module>");
                
                // Needed for compatibility with upcoming .NET 5 feature, look for existing initializer first
                var staticCtor = moduleType.Methods.FirstOrDefault(m => m.Name == ".cctor");
                if (staticCtor == null)
                {
                    // Create a new static ctor if none exists
                    staticCtor = new MethodDefinition(".cctor",
                        MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                        MethodAttributes.Static | MethodAttributes.Private,
                        asm.MainModule.TypeSystem.Void);
                    staticCtor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    moduleType.Methods.Add(staticCtor);
                }

                foreach (var i in initializers)
                    staticCtor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, i));
            }
            
        }
        
        
        
        static void PatchMethod(MethodDefinition method)
        {
            if (method.HasBody)
            {
                var ilProcessor = method.Body.GetILProcessor();

                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    Instruction instruction = instructions[i];

                    if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference methodDescription)
                    {
                        if (methodDescription.Name.StartsWith("Calli") && methodDescription.DeclaringType.Name == "LocalInterop")
                        {
                            var callSite = new CallSite(methodDescription.ReturnType) { CallingConvention = MethodCallingConvention.StdCall };

                            if (methodDescription.Name.StartsWith("CalliCdecl"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.C;
                            }
                            else if(methodDescription.Name.StartsWith("CalliThisCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.ThisCall;
                            }
                            else if(methodDescription.Name.StartsWith("CalliStdCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.StdCall;
                            }
                            else if(methodDescription.Name.StartsWith("CalliFastCall"))
                            {
                                callSite.CallingConvention = MethodCallingConvention.FastCall;
                            }

                            // Last parameter is the function ptr, so we don't add it as a parameter for calli
                            // as it is already an implicit parameter for calli
                            for (int j = 0; j < methodDescription.Parameters.Count - 1; j++)
                            {
                                var parameterDefinition = methodDescription.Parameters[j];
                                callSite.Parameters.Add(parameterDefinition);
                            }

                            // Create calli Instruction
                            var callIInstruction = ilProcessor.Create(OpCodes.Calli, callSite);

                            // Replace instruction
                            ilProcessor.Replace(instruction, callIInstruction);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Patches the type.
        /// </summary>
        /// <param name="type">The type.</param>
        static void PatchType(TypeDefinition type, List<TypeDefinition> classToRemoveList)
        {
            // Patch methods
            foreach (var method in type.Methods)
                PatchMethod(method);

            if (type.Name == "LocalInterop")
                classToRemoveList.Add(type);

            // Patch nested types
            foreach (var typeDefinition in type.NestedTypes)
                PatchType(typeDefinition, classToRemoveList);
        }
    }
}
