using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace sitemap_save_if_while
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
            string file = "admin\\sitemap_save.php";
            string up_cont = File.ReadAllText(file);
            if (file.Contains("admin\\sitemap_save.php") && up_cont.Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))") && !up_cont.Contains("if($query_text_all !== FALSE)"))
            {
                up_cont = ClearStringToArray(up_cont, out string[] r);
                bool sfb = false;
                for (int i = 0; i < r.Count(); i++)
                {
                    if (r[i].Contains("while($data_stranky_text_all = mysqli_fetch_array($query_text_all))"))
                    {
                        up_cont += "          if($query_text_all !== FALSE)\n          {\n";
                        sfb = true;
                    }
                    if (r[i].Contains("}") && sfb)
                    {
                        up_cont += "    " + r[i] + "\n";
                        sfb = false;
                    }
                    up_cont += r[i] + "\n";
                }
                File.WriteAllText(file, up_cont);
            }
            else
            {
                Console.WriteLine("fail");
                Console.ReadKey();
            }
        }
    }
}
