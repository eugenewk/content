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
                string filename = "ContentScanResults.txt";

                using (StreamWriter sw = File.CreateText(path + "\\" + filename))
                {
                    sw.WriteLine("Hello");
                    sw.WriteLine("And");
                    sw.WriteLine("Welcome");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                Console.ReadKey();
            }

            Console.WriteLine("Successfully executed");
            Console.ReadKey();
        }
    }
}
