//using System.Collections.Generic;
//using Avalonia.Markup.Xaml.HotReload;
//using XamlX.IL;

//namespace Avalonia.Markup.Xaml.UnitTests.HotReload.Diff
//{
//    public class DiffTestBase : XamlTestBase
//    {
//        protected Markup.Xaml.HotReload.Diff GetDiffBlocks<T>(string xaml, string modifiedXaml)
//        {
//            var oldInstructions = LoadXaml<T>(xaml, false);
//            var newInstructions = LoadXaml<T>(modifiedXaml, false);
            
//            var differ = new IlDiffer(oldInstructions, newInstructions);
            
//            return differ.Diff();
//        }

//        private List<RecordingIlEmitter.RecordedInstruction> LoadXaml<T>(
//            string xaml,
//            bool patchIl)
//        {
//            return AvaloniaXamlAstLoader.Load(
//                xaml,
//                "Test.xaml",
//                typeof(T),
//                typeof(T).Assembly,
//                patchIl: patchIl);
//        }
//    }
//}
