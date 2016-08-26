using System;
using System.IO;

namespace ContentScanner
{
    public class Mapper : DivingGear
    {
        int depth;
        int progress_total;
        string output_file;
        string error_file;
        string output_filename;
        string error_filename;
        string s_format = "content-map-results-";
        string e_format = "content-map-errors-";
        
        public Mapper()
        {
            depth = 0;

            output_filename = s_format + TimeStamp + ".txt";
            error_filename = e_format + TimeStamp + ".txt";
            output_file = OutputFolder + @"\" + output_filename;
            error_file = OutputFolder + @"\" + error_filename;

            try
            {
                // create output files
                Output = File.CreateText(output_file);
                Errors = File.CreateText(error_file);
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.WriteLine("Insufficient permissions to write output files in this directory.\nTerminating scan.\n");
                ErrorViewer(uae.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed.");
                ErrorViewer(e.ToString());
            }

            //count objects
            Console.WriteLine("\nStarting object count...");
            progress_total = GetProgressTotalFolders(Errors, BaseDir);
            Console.WriteLine("\nObject count complete: {0} total objects.\n", progress_total);

            //map
            Console.WriteLine("Starting mapping...");
            ProgressBar = new ProgressBar();
            Output.WriteLine("L1\tL2\tL3\tL4\tL5\tL6\tL7\tL8\t");
            Map(depth, BaseDir, Progress);

            ProgressBar.Dispose();

            Console.WriteLine("Mapping complete. {0} objects processed. Your output files can be found in the 'content-scanner-outputs' folder:", progress_total);
            Console.WriteLine("\n\tMapping results: {0}\n\tErrors (if any): {1}", output_filename, error_filename);

            // clean up streamwriters. If these aren't here the last few lines tend to get cut off. 
            Output.Flush();
            Output.Close();

            Errors.Flush();
            Errors.Close();

            Dispose();
        }

        private void Map(int depth, string path, int progress)
        {
            string depth_tabs = "";
            for (int i = 0; i < depth; i++)
            {
                depth_tabs = depth_tabs + "\t";
            }

            depth++;

            string dir_name = new DirectoryInfo(path).Name;

            // write dir name
            Output.WriteLine("{0}{1}", depth_tabs, path);


            // get subdirs
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

            // recurse for each subdir
            foreach (string dir in dirs)
            {
                try // check for errors, output to error file if any
                {
                    progress++;
                    ProgressBar.Report((double)Progress / progress_total);

                    // recurse!
                    Map(depth, dir, progress);
                }
                catch (Exception e)
                {
                    progress++;
                    ProgressBar.Report((double)progress / progress_total);

                    Errors.WriteLine("{0}\t{1}", dir, e.ToString());
                }
            }
        }
    }
}
