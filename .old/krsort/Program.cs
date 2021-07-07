using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace krsort
{
    class Program
    {
        static void Main(string[] args)
        {
            /*

            //při pouziti funkci ocekavajici pole je nutne mit nadefinovanou promennou ($pole = array()) napr. krsort, foreach .. >>> hledat (foreach, krsort, )
                if (Regex.IsMatch(up_cont, "krsort|foreach"))
                {
                    MatchCollection matches = Regex.Matches(up_cont, "krsort|foreach");
                    List<string> vars = new List<string>();
                    foreach (Match match in matches)
                    {
                        string variable = "";
                        bool w = false;
                        for (int i = match.Index; match.Index < up_cont.Length; i++)
                        {
                            if ((up_cont[i] == ')' || up_cont[i] == ' ') && w) break;
                            if (up_cont[i] == '$') w = true;
                            if (w) variable += up_cont[i];
                        }
                        if (!vars.Contains(variable) && !variable.Contains("$this") && !up_cont.Contains("isset(" + variable) && !up_cont.Contains(variable + " = array(") && !up_cont.Contains(variable + ";"))
                        {
                            vars.Add(variable);
                            Console.WriteLine(" - krsort or foreach: " + variable);
                        }
                    }
                    if (vars.Count() > 0)
                    {
                        string declare = "<?php\n";
                        foreach (string variable in vars) declare += $"if (!isset({variable}))\n{{\n    {variable} = array();\n}}\n";
                        up_cont = declare + "?>\n\n" + up_cont;
                    }
                }


            */

            /*
                if (up_cont.Contains("krsort"))
                {
                    MatchCollection matches = Regex.Matches(up_cont, "krsort");
                    List<string> vars = new List<string>();
                    foreach (Match match in matches)
                    {
                        string variable = "";
                        bool w = false;
                        for (int i = match.Index; up_cont[i] != ')'; i++)
                        {
                            if (w) variable += up_cont[i];
                            if (up_cont[i] == '(') w = true;
                        }
                        if (!vars.Contains(variable))
                        {
                            vars.Add(variable);
                            Console.WriteLine(" - krsort: " + variable);
                        }
                    }
                    string declare = "<?php\n";
                    foreach (string variable in vars) declare += variable + " = array();\n";
                    up_cont = declare + "?>\n\n" + up_cont;
                }*/


            string file = "home_admin.php";
            string up_cont = File.ReadAllText(file);
            if (up_cont.Contains("krsort"))
            {
                MatchCollection matches = Regex.Matches(up_cont, "krsort");
                List<string> vars = new List<string>();
                foreach (Match match in matches)
                {
                    string variable = "";
                    bool w = false;
                    for (int i = match.Index; up_cont[i] != ')'; i++)
                    {
                        if (w) variable += up_cont[i];
                        if (up_cont[i] == '(') w = true;
                    }
                    vars.Add(variable);
                }
                string declare = "<?php\n";
                foreach (string variable in vars) declare += variable + " = array();\n";
                up_cont = declare + "?>\n\n" + up_cont;

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
