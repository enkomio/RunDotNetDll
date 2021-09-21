using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldProgram
{
    class Program
    {
        static Program()
        {
            Console.WriteLine("Hello from a static constructor");
        }
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Console.WriteLine("Hello " + args[0]);
            }
            else
            {
                Console.WriteLine("Hello world!");
            }            
        }
    }
}
