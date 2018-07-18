using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows.Forms;

namespace RunDotNetDll
{
    class Program
    {
        private static IEnumerable<Type> GetAllTypes(String dll, Boolean filter)
        {
            var assembly = Assembly.LoadFile(dll);
            return
                assembly
                .GetTypes()
                .Where(methodInfo => filter ? methodInfo.Module.Name.Equals(assembly.ManifestModule.Name) : true);
        }

        private static IEnumerable<MethodInfo> GetAllMethods(String dll, Boolean filter)
        {
            return 
                GetAllTypes(dll, filter).
                SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }

        private static String GetFullMethodName(MethodInfo methodInfo)
        {
            return String.Format("{0}.{1}", methodInfo.DeclaringType.FullName, methodInfo.Name);
        }

        private static Boolean TryRunWindowsForm(String dll, String entryPointName)
        {
            var success = false;
            var formType =
                GetAllTypes(dll, true)
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
            var entryPoint = 
                GetAllMethods(dll, false)
                .Where(methodInfo => GetFullMethodName(methodInfo).Equals(entryPointName, StringComparison.OrdinalIgnoreCase))
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
                var mockParameters = Activator.CreateInstance(parameterInfo.ParameterType);
                parameters.Add(mockParameters);
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
                Console.WriteLine("\t" + GetFullMethodName(method));
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
                Console.Error.WriteLine("\t<dll>,<entrypoint>   The .NET Dll to run and the entry point, eg: mydll.dll,Mynaspace.MyClass.EntryPoint");
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
