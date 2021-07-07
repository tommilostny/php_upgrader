using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace global_beta
{
    class Program
    {
        private static bool CheckForMysqli_BeforeAnotherFunction(string[] r, int i) //kontrola funkce zda obsahuje mysqli_ (pro přidávání global $beta;)
        {
            bool javascript = false;
            int bracket_count = 0;
            for (int y = i; y < r.Count(); y++)
            {
                if (r[y].Contains("<script")) javascript = true;
                if (r[y].Contains("</script")) javascript = false;

                if (!javascript)
                {
                    if (r[y].Contains("mysql_") && !r[y].TrimStart(' ').StartsWith("//")) return true;

                    if (r[y].Contains("{")) bracket_count++;
                    else if (r[y].Contains("}")) bracket_count--;

                    if ((r[y].Contains("function") || r[y].Contains("global $beta;") || bracket_count <= 0) && y > i) break;
                }
            }
            return false;
        }


        static void Main(string[] args)
        {
            string file = "test.php";
            string up_cont = File.ReadAllText(file);
            if (Regex.IsMatch(up_cont, "(?s)^(?=.*?function )(?=.*?mysql_)") && !up_cont.Contains("$this"))
            {
                string[] r = up_cont.Split('\n');
                up_cont = "";
                bool javascript = false;
                for (int i = 0; i < r.Count(); i++)
                {
                    if (r[i].Contains("<script")) javascript = true;
                    else if (r[i].Contains("</script")) javascript = false;

                    up_cont += r[i] + "\n";
                    if (r[i].Contains("function") && !javascript)
                    {
                        if (CheckForMysqli_BeforeAnotherFunction(r, i))
                        {
                            up_cont += r[++i] + "\n\n    global $beta;\n\n";
                            Console.WriteLine(" - global $beta; added");
                        }
                    }
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
