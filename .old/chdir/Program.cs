using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace chdir
{
    class Program
    {
        static void Main(string[] args)
        {
            File.WriteAllText("vytvoreni_adr.php", File.ReadAllText("vytvoreni_adr.php").Replace("chdir", "//chdir"));
        }
    }
}
