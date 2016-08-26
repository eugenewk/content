using System;
using System.IO;

namespace ContentScanner
{
    class Scanner : DivingGear
    {
        int progress_total;
        int cur_progress;
        string output_file;
        string error_file;
        string output_filename;
        string error_filename;
        string s_format = "content-scan-results-";
        string e_format = "content-scan-errors-";

        public Scanner()
        {
            output_filename = s_format + TimeStamp + ".txt";
            error_filename = e_format + TimeStamp + ".txt";
            output_file = OutputFolder + @"\" + output_filename;
            error_file = OutputFolder + @"\" + error_filename;

            cur_progress = progress;

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
            progress_total = GetProgressTotalFiles(Errors, BaseDir);
            Console.WriteLine("\nObject count complete: {0} total objects.\n", progress_total);

            //map
            Console.WriteLine("Starting scan...");
            ProgressBar = new ProgressBar();
            Output.WriteLine("Filename\tMIME Type\tPath\tSize\tLast Modified\tLast Accessed\tCreate Date\tOwner Account");
            Errors.WriteLine("dir\terror");
            Scan(BaseDir, ref cur_progress);

            ProgressBar.Dispose();

            Console.WriteLine("Scan complete. {0} objects processed. Your output files can be found in the 'content-scanner-outputs' folder:", progress_total);
            Console.WriteLine("\n\tScan results: {0}\n\tErrors (if any): {1}", output_filename, error_filename);

            // clean up streamwriters. If these aren't here the last few lines tend to get cut off. 
            Output.Flush();
            Output.Close();

            Errors.Flush();
            Errors.Close();

            Dispose();
        }

        private void Scan(string path, ref int progress)
        {
            string dir_name = new DirectoryInfo(path).Name;

            // write dir name
            Output.WriteLine("Folder\t-\t{0}\t-\t-\t-\t-\t-", dir_name);

            // get files
            string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);

            // write files in dir
            foreach (string file in files)
            {
                try // check for errors, output to error file if any
                {
                    progress++;
                    ProgressBar.Report((double)progress / progress_total);

                    // get file attributes

                    // name / size
                    string filename = Path.GetFileName(file);
                    long filesize_bytes = new System.IO.FileInfo(file).Length;
                    //string filesize = format_filesize(filesize_bytes); // probably better to leave in bytes for crunching purposes

                    // owner account (as string)
                    var owner = System.IO.File.GetAccessControl(file).GetOwner(typeof(System.Security.Principal.SecurityIdentifier));
                    string owner_account = owner.Translate(typeof(System.Security.Principal.NTAccount)).ToString();

                    // create date, last modified, last accessed
                    DateTime last_modified = System.IO.File.GetLastWriteTime(file);
                    DateTime last_accessed = System.IO.File.GetLastAccessTime(file); // new
                    DateTime create_date = System.IO.File.GetCreationTime(file); // new

                    // MIME type
                    string mime_type = MIMEAssistant.GetMIMEType(file);

                    //"Filename\tMIME Type\tPath\tSize\tLast Modified\tLast Accessed\tCreate Date\tOwner Account"

                    // write attributes to putput file
                    Output.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", filename, mime_type, path, filesize_bytes, last_modified.ToShortDateString(), last_accessed.ToShortDateString(), create_date.ToShortDateString(), owner_account);
                }
                catch (Exception e)
                {
                    progress++;
                    ProgressBar.Report((double)progress / progress_total);

                    Errors.WriteLine("{0}\t{1}", path, e.ToString());
                }
            }

            // get subdirs
            string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

            // recurse for each subdir
            foreach (string dir in dirs)
            {
                try // check for errors, output to error file if any
                {
                    progress++;
                    ProgressBar.Report((double)progress / progress_total);

                    // recurse!
                    Scan(dir, ref progress);
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
