using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentScanner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Content Scanner v0.1");
            Console.WriteLine("Press any key to start the scan");

            Console.ReadKey();

            try
            {
                //get current directory
                string path = Directory.GetCurrentDirectory();
                // string path = @"C:\Users\Eugene\Desktop\beats";

                string filename = "ContentScanResults.txt";

                string file = path + @"\" + filename;

                using (StreamWriter sw = File.CreateText(path + "\\" + filename))
                {
                    sw.WriteLine("Filename\tPath\tSize");
                    Scanner(sw, path);
                }

                Console.WriteLine("Scan complete");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                Console.ReadKey();
            }
        }

        static void Scanner(StreamWriter sw, string current_dir)
        {
            // note file is already open for writing

            // get all subdirectories in current path, store for later
            string[] dirs = Directory.GetDirectories(current_dir, "*", SearchOption.TopDirectoryOnly);
            foreach (string dir in dirs)
            {
                sw.WriteLine("Folder\tdir\t-");

                Console.WriteLine(dir);
                Scanner(sw, dir);
            }
            
            // get all files
            string[] files = Directory.GetFiles(current_dir, "*", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                long filesize = new System.IO.FileInfo(file).Length;
                sw.WriteLine("{0}\t{1}\t{2}", filename, current_dir, filesize);

                Console.WriteLine(file);
            }

            // for each file, write name, path, and size
           /* foreach (string file in files)
            {
                
            }*/


            // for each subdirectory, recurse
        }
    }
}
