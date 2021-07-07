using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace anketa
{
    class Program
    {
        static void Main(string[] args)
        {
            File.WriteAllText("anketa.php", File.ReadAllText("anketa.php").Replace("include_once(\"../setup.php\")", "include_once(\"setup.php\")"));
        }
    }
}
