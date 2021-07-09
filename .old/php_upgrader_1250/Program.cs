using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace php_upgrader
{
    class Program
    {
        private static string base_folder = @"C:\McRAI\";
        private static string web_name;
        private static List<string> files_containing_mysql_ = new List<string>();

        private static void UpgradeFiles(string dir)
        {
            foreach (string file in Directory.GetFiles(dir, "*.php"))
            {
                string up_cont = File.ReadAllText(file, Encoding.GetEncoding(1250));
                if (up_cont.Contains("1250") && !up_cont.Contains("<?php header(\"Content-Type: text/html; charset=windows-1250\"); ?>"))
                {
                    Console.WriteLine(file);
                    up_cont = "<?php header(\"Content-Type: text/html; charset=windows-1250\"); ?>\n\n" + up_cont;
                    File.WriteAllText(file, up_cont, Encoding.GetEncoding(1250));
                }
                else Console.WriteLine("ne: " + file);
                if (up_cont.ToLower().Contains("mysql_")) files_containing_mysql_.Add(file);
            }
        }

        private static void GetFolders(string dir)
        {
            foreach (string subdir in Directory.GetDirectories(dir))
            {
                if (Directory.GetDirectories(subdir).Count() > 0 && !subdir.Contains("tiny_mce")) GetFolders(subdir);
                UpgradeFiles(subdir);
            }
        }

        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                web_name = args[0];
                string dir = base_folder + "weby\\" + web_name;
                if (Directory.Exists(dir))
                {
                    Console.WriteLine("\nProcessed files:\n");
                    GetFolders(dir);
                    UpgradeFiles(dir);

                    Console.WriteLine("\nAutomatic PHP upgrade of " + web_name + " is complete!");
                    Console.WriteLine("Files containing mysql_: " + files_containing_mysql_.Count().ToString());
                    foreach (string item in files_containing_mysql_) Console.WriteLine(item);
                }
                else Console.WriteLine("Folder " + dir + " does not exist.");
            }
            else Console.WriteLine("php_upgrader [WEB_FOLDER_NAME]\n\nweb folder name from " + base_folder + "weby\\.");
        }
    }
}