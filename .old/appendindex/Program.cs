using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace appendindex
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter("index.php", true);
            sw.Write("\n<?php mysqli_close($beta); ?>");
            sw.Close();
        }
    }
}
