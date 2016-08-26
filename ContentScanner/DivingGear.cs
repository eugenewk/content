using System;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace ContentScanner
{
    public class DivingGear : IDisposable
    {
        private StreamWriter output;
        private StreamWriter errors;
        private string current_dir = Directory.GetCurrentDirectory();
        private string output_folder_name = @"\content-scanner-outputs";
        private string output_folder;

        //for dispose function
        public bool disposed;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        //initialized on startup
        private string timestamp;
        private int progress;
        private ProgressBar progress_bar;

        
        public DivingGear()
        {
            // create output folder
            output_folder = current_dir + output_folder_name;
            CreateOutputFolder(output_folder);

            //get timestamp
            timestamp = GetTimestamp(DateTime.Now);

            //initialize progress counter
            progress = 0;
        }

        public StreamWriter Output
        {
            get { return output; }
            set { output = value; }
        }

        public StreamWriter Errors
        {
            get { return errors; }
            set { errors = value; }
        }

        public string OutputFolder
        {
            get { return output_folder; }
        }

        public string TimeStamp
        {
            get { return timestamp; }
        }

        public int Progress
        {
            get { return progress; }
        }

        public ProgressBar ProgressBar
        {
            get { return progress_bar; }
            set { progress_bar = value; }
        }

        public string BaseDir
        {
            get { return current_dir; }
        }

        private static void CreateOutputFolder(string dir)
        {
            try
            {
                // Determine whether the directory exists.
                if (!Directory.Exists(dir))
                {
                    // Try to create the directory.
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to create output directory.");
                ErrorViewer(e.ToString());
            }
        }

        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        public static int GetProgressTotalFiles(StreamWriter errors, string path)
        {

            try // get file totals for progress bar
            {
                int total_files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length;
                string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

                int total_dirs = dirs.Length;

                foreach (string dir in dirs)
                {
                    total_dirs = total_dirs + GetProgressTotalFiles(errors, dir);
                }

                return total_dirs + total_files;
            }
            catch (Exception e)
            {
                //Console.WriteLine("Progress bar encountered an error: {0}", e.ToString());
                errors.WriteLine("FileCounter\t{0}", e.ToString());
                Console.WriteLine("Unable to access {0}", path);
                return 1;
            }
        }

        public static int GetProgressTotalFolders(StreamWriter errors, string path)
        {

            try // get file totals for progress bar
            {
                string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

                int total_dirs = dirs.Length;

                foreach (string dir in dirs)
                {
                    total_dirs = total_dirs + GetProgressTotalFolders(errors, dir);
                }

                return total_dirs;
            }
            catch (Exception e)
            {
                //Console.WriteLine("Progress bar encountered an error: {0}", e.ToString());
                errors.WriteLine("FileCounter\t{0}", e.ToString());
                Console.WriteLine("Unable to access {0}", path);
                return 1;
            }
        }

        /// <summary>
        /// optional viewer for full error messages. called on exception catches
        /// </summary>
        public static void ErrorViewer(string error_msg)
        {
            Console.Write("Press 'e' to see the error details. Press any other key to exit: ");

            if (Console.ReadKey().KeyChar == 'e')
            {
                Console.WriteLine("\n\n" + error_msg.ToString());
                Console.ReadKey();
            }
        }

        public static string FormatFilesize(long filesize_bytes) // not using this right now, keeping just in case. 
        {
            long filesize_kb = filesize_bytes / 1024;
            if (filesize_kb < 1000)
            {
                return filesize_kb.ToString() + "KB";
            }

            float filesize_mb = filesize_kb / 1000;
            if (filesize_mb < 1000)
            {
                return filesize_mb.ToString() + "MB";
            }

            float filesize_gb = filesize_mb / 1000;
            if (filesize_gb < 1000)
            {
                return filesize_mb.ToString() + "GB";
            }

            return ">1TB";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
    }
}
