using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CSharp;

namespace NppLinqPlugin
{
    public class LinqExecutor
    {
        /// <summary>
        /// LinqExecute
        /// currently supporting only primitive results (string/int/..)
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="linqQueryCode"></param>
        /// <param name="helpersCode"></param>
        /// <returns></returns>
        public static IEnumerable<string> LinqExecute(IEnumerable<string> lines, 
            string linqQueryCode, string helpersCode = null)
        {
            helpersCode = helpersCode ?? string.Empty;
            string code = CODE_WRAPPER.Replace("#linq_query_code#", linqQueryCode)
                .Replace("#helpers_code#", helpersCode);

            object res = ExecuteCode(code, "LinqExecutorNamespace", "LinqExecutorClass",
                                                     "ExecuteLinqQueryOnLines", false, lines);

            IEnumerable<string> resEnumerableString = res as IEnumerable<string>;
            if (resEnumerableString != null)
                return resEnumerableString;            
            
            if (res is IEnumerable)
            {
                // if collection - convert to IEnumerable<string>
                return ((IEnumerable) res).Cast<object>().Select(FormatObject);
            }
            
            // return single object as IEnumerable<string>
            return new[] { FormatObject(res) };
            
        }

        private const string CODE_WRAPPER =
@"
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace LinqExecutorNamespace
{
	public class LinqExecutorClass
	{				
		public object ExecuteLinqQueryOnLines(
			IEnumerable<string> lines)
		{
			return #linq_query_code#;			
		}

        #helpers_code#
	}
}
";

        
        // Define other methods and classes here
        private static Assembly BuildAssembly(string code)
        {
            // compiler creation - need to target v3.5 for linq
            Microsoft.CSharp.CSharpCodeProvider provider =
               new CSharpCodeProvider(new Dictionary<string,string>{{ "CompilerVersion","v3.5" }});
            ICodeCompiler compiler = provider.CreateCompiler();
            CompilerParameters compilerparams = new CompilerParameters(new[] { "system.dll", "system.core.dll" });
            compilerparams.GenerateExecutable = false;
            compilerparams.GenerateInMemory = true;
            CompilerResults results =
               compiler.CompileAssemblyFromSource(compilerparams, code);
            if (results.Errors.HasErrors)
            {

                StringBuilder errors = new StringBuilder("Compiler Errors:\r\n");
                string[] lines = code.Split('\n');
                foreach (CompilerError error in results.Errors)
                {
                    //errors.AppendFormat("Line {0},{1}\t: {2}.",
                      //     error.Line, error.Column, error.ErrorText);
                    errors.AppendLine(error.ErrorText);
                    errors.AppendLine().AppendLine("<<<");
                    for (int i = error.Line - 2; i < Math.Min(lines.Length, error.Line + 2); i++)
                    {
                        errors.AppendLine(lines[i].TrimEnd('\r'));
                    }
                    errors.AppendLine().AppendLine(">>>");
                }
                throw new Exception(errors.ToString());
            }
            else
            {
                return results.CompiledAssembly;
            }
        }

        private static object ExecuteCode(string code,
            string namespacename, string classname,
            string functionname, bool isstatic, params object[] args)
        {
            object returnval = null;
            Assembly asm = BuildAssembly(code);
            object instance = null;
            Type type = null;
            if (isstatic)
            {
                type = asm.GetType(namespacename + "." + classname);
            }
            else
            {
                instance = asm.CreateInstance(namespacename + "." + classname);
                type = instance.GetType();
            }
            MethodInfo method = type.GetMethod(functionname);
            returnval = method.Invoke(instance, args);
            return returnval;
        }

        public static string FormatObject(object obj)
        {
            Type type = obj.GetType();
            if (obj is string || type.IsPrimitive)
                return obj.ToString();

            // anonymous has a good ToString method
            if (IsAnonymousType(type))
                return obj.ToString();

            // check if class has its own ToString()
            MethodInfo methodInfo = type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo.DeclaringType != typeof(object))
                return obj.ToString();

            // use reflection to show like Anonymous (currently only properties)
            // note - recursively formatting inside properties
            return "{ " +
                string.Join(", ",
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(p => string.Format("{0} = {1}", p.Name, FormatObject(p.GetValue(obj, null)))).ToArray())
                + " }";
        }
        private static bool IsAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }

   
}
