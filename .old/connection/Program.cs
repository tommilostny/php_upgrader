using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace connection
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader("connection.php");
            string connection = "";
            bool poznamka = false;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                if (line.Contains("/*")) poznamka = true;
                if (line.Contains("*/")) poznamka = false;

                connection += line + "\n";
                if (line.Contains("$password_beta") && !poznamka) break;
            }
            sr.Close();
            connection += File.ReadAllText("connection.txt");
            File.WriteAllText("connection.php", connection);
        }
    }
}
