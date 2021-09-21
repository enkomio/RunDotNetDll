using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using dnlib.PE;
using dnlib.DotNet;

namespace RunDotNetDll
{
    class Program
    {
        private static Object CreateObject(Type type)
        {
            Object result = null;
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                result = Array.CreateInstance(elementType, 0);
            }
            else if (!type.IsAbstract)
            {
                try
                {
                    result = Activator.CreateInstance(type);
                }
                catch
                {
                    // unable to create the given object add an uninitialized object
                    result = FormatterServices.GetUninitializedObject(type);
                }
            }
            return result;
        }

        private static IEnumerable<Type> GetAllTypes(String dll)
        {
            return Assembly.LoadFile(dll).GetTypes();
        }

        private static IEnumerable<MethodBase> GetAllMethods(String dll, Boolean filter)
        {
            var modDef = ModuleDefMD.Load(new PEImage(dll));
            var methods = modDef.GetTypes().SelectMany(t => t.Methods);
            var modules = Assembly.LoadFile(dll).GetModules();            

            foreach(var method in methods)
            {
                foreach(var module in modules)
                {
                    var methodBase = module.ResolveMethod((Int32)method.MDToken.Raw);
                    if (methodBase != null)
                    {
                        yield return methodBase;
                    }
                }
            }
        }

        private static String GetFullMethodName(MethodBase methodBase)
        {            
            return String.Format("{0}.{1}", methodBase.DeclaringType.FullName, methodBase.Name);
        }

        private static Boolean IsTargetMethod(MethodBase methodBase, String entryPointName, Int32 metadataToken)
        {
            return
                methodBase.MetadataToken == metadataToken ||
                GetFullMethodName(methodBase).Equals(entryPointName, StringComparison.OrdinalIgnoreCase);
        }

        private static Boolean TryRunWindowsForm(String dll, String entryPointName)
        {
            var success = false;
            var formType =
                GetAllTypes(dll)
                .Where(type => type.Module.Name.Equals(type.Assembly.ManifestModule.Name))
                .Where(type => typeof(Form).IsAssignableFrom(type) && type.FullName.Equals(entryPointName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (formType != null)
            {
                var form = Activator.CreateInstance(formType) as Form;
                form.Show();
                success = true;
            }

            return success;
        }

        private static void RunDllMethod(String dll, String entryPointName)
        {
            Object instance = null;
            var metadataToken = -1;
            if (entryPointName.StartsWith("@")) {
                var cleanToken = entryPointName.TrimStart('@');
                if (cleanToken.StartsWith("0x"))
                    metadataToken = Convert.ToInt32(cleanToken, 16);
                else
                    metadataToken = Convert.ToInt32(cleanToken, 10);
            }

            var entryPoint = 
                GetAllMethods(dll, false)
                .Where(methodBase => IsTargetMethod(methodBase, entryPointName, metadataToken))
                .FirstOrDefault();

            if (entryPoint == null)
            {
                Console.Error.WriteLine("No EntryPoint defined for the Dll: " + dll);
                Environment.Exit(3);
            }

            // if it is an instance method, try to create an object by invoking the default constructor
            if (!entryPoint.IsStatic)
            {
                instance = Activator.CreateInstance(entryPoint.DeclaringType);
            }

            // create the list of parameters to pass
            var parameters = new List<Object>();
            foreach(var parameterInfo in entryPoint.GetParameters())
            {
                parameters.Add(CreateObject(parameterInfo.ParameterType));
            }

            // invoke the entry point
            entryPoint.Invoke(instance, parameters.ToArray());
        }

        private static Tuple<String, String> ParseArguments(String arg)
        {
            var result = Tuple.Create<String, String>(arg, String.Empty);

            var items = arg.Split(',');
            if (items.Length > 1)
            {
                result = Tuple.Create<String, String>(items[0], items[1]);
            }            

            return result;
        }

        private static void ShowallMethods(String dll)
        {   
            var allMethods = GetAllMethods(dll, true);

            Console.WriteLine("[+] Methods");
            foreach (var method in allMethods)
            {
                Console.WriteLine("\t{0} (0x{1}) - {2}", method.MetadataToken, method.MetadataToken.ToString("X"), GetFullMethodName(method));
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("-= Run a specific method of a .NET Assembly =-");
            Console.WriteLine();

            if (args.Length < 1)
            {
                Console.Error.WriteLine("Options:");
                Console.Error.WriteLine("\t<dll>                Display all method of the assembly");
                Console.Error.WriteLine("\t<dll>,<entrypoint>   The .NET Dll to run and the entry point, " + Environment.NewLine +
                    "                             You can specify the metadata token too." + Environment.NewLine +
                    "                             eg: mydll.dll,Mynaspace.MyClass.EntryPoint" + Environment.NewLine +
                    "                                 mydll.dll,@0x06000001");
                Environment.Exit(1);
            }

            // parse arguments
            var tuple = ParseArguments(args[0]);
            var dllPath = Path.GetFullPath(tuple.Item1);

            if (!File.Exists(dllPath))
            {
                Console.Error.WriteLine("Unable to find file: " + dllPath);
                Environment.Exit(2);
            }

            Console.WriteLine("[+] DLL: " + dllPath);

            if (String.IsNullOrWhiteSpace(tuple.Item2))
            {
                ShowallMethods(dllPath);
            }
            else
            {                
                var entryPoint = tuple.Item2;
                if (!TryRunWindowsForm(dllPath, entryPoint))
                {
                    RunDllMethod(dllPath, entryPoint);
                }
                
                Console.WriteLine("[+] Press Enter to Exit");
                Console.ReadLine();
            }
        }
    }
}
