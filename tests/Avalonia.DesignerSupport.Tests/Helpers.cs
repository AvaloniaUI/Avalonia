using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.DesignerSupport.Tests
{
    static class Helpers
    {
        public static void StructDiff(object parsed, object expected) => StructDiff(parsed, expected, "{root}");

        static void StructDiff(object parsed, object expected, string path)
        {
            if (parsed == null && expected == null)
                return;
            if ((parsed == null && expected != null) || (parsed != null && expected == null))
                throw new Exception(
                    $"{path}: Null mismatch: {(parsed == null ? "null" : "not-null")}  {(expected == null ? "null" : "not-null")}");
            
            if (parsed.GetType() != expected.GetType())
                throw new Exception($"{path}: Type mismatch: {parsed.GetType()} {expected.GetType()}");

            if (parsed is string || parsed.GetType().IsPrimitive)
            {
                if (!parsed.Equals(expected))
                    throw new Exception($"{path}: Not equal {parsed} {expected}");
            }
            else if (parsed is IDictionary dic)
            {
                var dic2 = (IDictionary) expected;
                if (dic.Count != dic2.Count)
                    throw new Exception($"{path}: Dictionary count mismatch: {dic.Count} {dic2.Count}");

                foreach (var k in dic.Keys.Cast<object>().OrderBy(o => o.ToString()))
                {
                    var v1 = dic[k];
                    var v2 = dic2[k];
                    StructDiff(v1, v2, path + "['" + k + "']");
                }
            }
            else if (parsed is IList col)
            {
                var col2 = (IList) expected;
                if (col.Count != col2.Count)
                    throw new Exception($"{path}: Collection count mismatch: {col.Count} {col2.Count}");
                for (var c = 0; c < col.Count; c++)
                    StructDiff(col[c], col2[c], path + "[" + c + "]");
            }
            else
            {
                foreach (var prop in parsed.GetType().GetProperties()
                    .Where(p => p.GetMethod != null && p.GetMethod.IsPublic))
                {
                    StructDiff(prop.GetValue(parsed), prop.GetValue(expected), path + "." + prop.Name);
                }
            }


            
        }
    }
}
