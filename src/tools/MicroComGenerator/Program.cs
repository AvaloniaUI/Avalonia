﻿using System;
using System.IO;
using System.Linq;
using CommandLine;

namespace MicroComGenerator
{
    class Program
    {
        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Input IDL file")]
            public string Input { get; set; }
            
            [Option("cpp", Required = false, HelpText = "C++ output file")]
            public string CppOutput { get; set; }
            
            [Option("cs", Required = false, HelpText = "C# output file")]
            public string CSharpOutput { get; set; }

        }
        
        static int Main(string[] args)
        {
            var p = Parser.Default.ParseArguments<Options>(args);
            if (p is NotParsed<Options>)
            {
                return 1;
            }

            var opts = ((Parsed<Options>)p).Value;
            
            var text = File.ReadAllText(opts.Input);
            var ast = AstParser.Parse(text);

            if (opts.CppOutput != null)
                File.WriteAllText(opts.CppOutput, CppGen.GenerateCpp(ast));
            if (opts.CSharpOutput != null)
                File.WriteAllText(opts.CSharpOutput, new CSharpGen(ast).Generate());
            
            return 0;
        }
    }
}
