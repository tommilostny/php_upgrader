using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace tinyajaxbehavior
{
    class Program
    {
        static void Main(string[] args)
        {
            File.Copy("file1.txt", "staroch.php", true);
        }
    }
}
