using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloWorldDll
{
    public class Hello
    {
        static Hello()
        {
            Console.WriteLine("Hello from a static constructor");
        }

        public void SayHello(string[] args)
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
