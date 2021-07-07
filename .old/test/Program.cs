using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace test
{
    class Program
    {
        private static string ClearStringToArray(string input, out string[] array)
        {
            array = input.Split('\n');
            return "";
        }

        static void Main(string[] args)
        {
            string up_cont = File.ReadAllText("home_admin.php");
            up_cont = ClearStringToArray(up_cont, out string[] r);
            Console.WriteLine("up_cont: \"" + up_cont + "\"\n");
            foreach (string item in r)
            {
                Console.WriteLine(item);
            }
            Console.ReadKey();
        }
    }
}
