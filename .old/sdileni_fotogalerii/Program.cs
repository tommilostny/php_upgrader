using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace sdileni_fotogalerii
{
    class Program
    {
        static void Main(string[] args)
        {
            string up_cont = File.ReadAllText("clanek.php");
            if (up_cont.Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
            {
                string[] r = File.ReadAllLines("clanek.php");
                up_cont = "";
                for (int i = 0; i < r.Count(); i++)
                {
                    if (r[i].Contains("$vypis_table_clanek[\"sdileni_fotogalerii\"]"))
                    {
                        up_cont += "        $p_sf = array();\n";
                    }
                    up_cont += r[i] + "\n";
                }
            }
            File.WriteAllText("clanek.php", up_cont);
        }
    }
}
