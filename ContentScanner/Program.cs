using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentScanner
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Content Scanner v0.4");
            Console.WriteLine("Press any key to start the scan\n");

            Console.ReadKey();

            //get current directory
            string path = Directory.GetCurrentDirectory();

            // use for testing
            // string path = @"C:\Users\Eugene\Desktop\beats";

            string output_filename = "content-scan-results.txt";
            string output_file = path + @"\" + output_filename;

            string error_filename = "content-scan-errors.txt";
            string error_file = path + @"\" + error_filename;

            try
            {
                StreamWriter output = File.CreateText(output_file);
                StreamWriter errors = File.CreateText(error_file);

                output.WriteLine("Filename\tMIME Type\tPath\tSize\tLast Modified\tLast Accessed\tCreate Date\tOwner Account");
                errors.WriteLine("dir\terror");

                Console.WriteLine("Starting object count...\n");
                int progress_total = GetProgressTotal(errors, path);
                Console.WriteLine("\nObject count complete: {0} total objects.\n", progress_total);
                int progress = 0;

                Console.WriteLine("Starting scan...");
                var progress_bar = new ProgressBar();

                Scanner(output, errors, path, progress_total, ref progress, progress_bar);

                progress_bar.Dispose();

                Console.WriteLine("Scan complete. {0} objects processed.", progress_total);
                Console.WriteLine("Press any key to exit.");

                Console.Read();

                // clean up streamwriters. If these aren't here the last few lines tend to get cut off. 
                output.Flush();
                output.Close();

                errors.Flush();
                errors.Close();
            }
            catch (UnauthorizedAccessException uae)
            {
                Console.WriteLine("Insufficient permissions to write output files in this directory.\nTerminating scan.\n");
                Console.Write("Press e to see the error details. Press any other key to exit: ");

                if (Console.ReadKey().KeyChar == 'e')
                {
                    Console.WriteLine("\n" + uae.ToString());
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}\n", e.ToString());
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        static void Scanner(StreamWriter output, StreamWriter errors, string current_dir, int progress_total, ref int progress, ProgressBar progress_bar)
        {
            // write dir name
            output.WriteLine("Folder\t-\t{0}\t-\t-\t-\t-\t-", current_dir);

            // get files
            string[] files = Directory.GetFiles(current_dir, "*", SearchOption.TopDirectoryOnly);

            // write files in dir
            foreach (string file in files)
            {
                try // check for errors, output to error file if any
                {
                    progress++;
                    progress_bar.Report((double) progress / progress_total);

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
                    output.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", filename, mime_type, current_dir, filesize_bytes, last_modified.ToShortDateString(), last_accessed.ToShortDateString(), create_date.ToShortDateString(), owner_account);
                }
                catch (Exception e)
                {
                    progress++;
                    progress_bar.Report((double) progress / progress_total);

                    errors.WriteLine("{0}\t{1}", current_dir, e.ToString());
                }
            }

            // get subdirs
            string[] dirs = Directory.GetDirectories(current_dir, "*", SearchOption.TopDirectoryOnly);

            // recurse for each subdir
            foreach (string dir in dirs)
            {
                try // check for errors, output to error file if any
                {
                    progress++;
                    progress_bar.Report((double) progress / progress_total);

                    // recurse!
                    Scanner(output, errors, dir, progress_total, ref progress, progress_bar);
                }
                catch (Exception e)
                {
                    progress++;
                    progress_bar.Report((double) progress / progress_total);

                    errors.WriteLine("{0}\t{1}", dir, e.ToString());
                }
            }
        }
        
        static int GetProgressTotal(StreamWriter errors, string path)
        {
            
            try // get file totals for progress bar
            {
                int total_files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length;
                string[] dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

                int total_dirs = dirs.Length;

                foreach (string dir in dirs)
                {
                    total_dirs = total_dirs + GetProgressTotal(errors, dir);
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

        static string format_filesize(long filesize_bytes) // not using this right now, keeping just in case. 
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
    }

    /// <summary>
    /// An ASCII progress bar
    /// source: somewhere on stack overflow. Can't find the original comment with this code.
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int blockCount = 10;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const string animation = @"|/-\";

        private readonly Timer timer;

        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar()
        {
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                    percent,
                    animation[animationIndex++ % animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }

    }

    /// <summary>
    /// class to get MIME types of files based on their file extensions
    /// source: Stack overflow comment by Jalal Aldeen Saa'd (http://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature)
    /// </summary>
    public static class MIMEAssistant
    {
        private static readonly Dictionary<string, string> MIMETypesDictionary = new Dictionary<string, string>
          {
            {"ai", "application/postscript"},
            {"aif", "audio/x-aiff"},
            {"aifc", "audio/x-aiff"},
            {"aiff", "audio/x-aiff"},
            {"asc", "text/plain"},
            {"atom", "application/atom+xml"},
            {"au", "audio/basic"},
            {"avi", "video/x-msvideo"},
            {"bcpio", "application/x-bcpio"},
            {"bin", "application/octet-stream"},
            {"bmp", "image/bmp"},
            {"cdf", "application/x-netcdf"},
            {"cgm", "image/cgm"},
            {"class", "application/octet-stream"},
            {"cpio", "application/x-cpio"},
            {"cpt", "application/mac-compactpro"},
            {"csh", "application/x-csh"},
            {"css", "text/css"},
            {"dcr", "application/x-director"},
            {"dif", "video/x-dv"},
            {"dir", "application/x-director"},
            {"djv", "image/vnd.djvu"},
            {"djvu", "image/vnd.djvu"},
            {"dll", "application/octet-stream"},
            {"dmg", "application/octet-stream"},
            {"dms", "application/octet-stream"},
            {"doc", "application/msword"},
            {"docx","application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {"dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template"},
            {"docm","application/vnd.ms-word.document.macroEnabled.12"},
            {"dotm","application/vnd.ms-word.template.macroEnabled.12"},
            {"dtd", "application/xml-dtd"},
            {"dv", "video/x-dv"},
            {"dvi", "application/x-dvi"},
            {"dxr", "application/x-director"},
            {"eps", "application/postscript"},
            {"etx", "text/x-setext"},
            {"exe", "application/octet-stream"},
            {"ez", "application/andrew-inset"},
            {"gif", "image/gif"},
            {"gram", "application/srgs"},
            {"grxml", "application/srgs+xml"},
            {"gtar", "application/x-gtar"},
            {"hdf", "application/x-hdf"},
            {"hqx", "application/mac-binhex40"},
            {"htm", "text/html"},
            {"html", "text/html"},
            {"ice", "x-conference/x-cooltalk"},
            {"ico", "image/x-icon"},
            {"ics", "text/calendar"},
            {"ief", "image/ief"},
            {"ifb", "text/calendar"},
            {"iges", "model/iges"},
            {"igs", "model/iges"},
            {"jnlp", "application/x-java-jnlp-file"},
            {"jp2", "image/jp2"},
            {"jpe", "image/jpeg"},
            {"jpeg", "image/jpeg"},
            {"jpg", "image/jpeg"},
            {"js", "application/x-javascript"},
            {"kar", "audio/midi"},
            {"latex", "application/x-latex"},
            {"lha", "application/octet-stream"},
            {"lzh", "application/octet-stream"},
            {"m3u", "audio/x-mpegurl"},
            {"m4a", "audio/mp4a-latm"},
            {"m4b", "audio/mp4a-latm"},
            {"m4p", "audio/mp4a-latm"},
            {"m4u", "video/vnd.mpegurl"},
            {"m4v", "video/x-m4v"},
            {"mac", "image/x-macpaint"},
            {"man", "application/x-troff-man"},
            {"mathml", "application/mathml+xml"},
            {"me", "application/x-troff-me"},
            {"mesh", "model/mesh"},
            {"mid", "audio/midi"},
            {"midi", "audio/midi"},
            {"mif", "application/vnd.mif"},
            {"mov", "video/quicktime"},
            {"movie", "video/x-sgi-movie"},
            {"mp2", "audio/mpeg"},
            {"mp3", "audio/mpeg"},
            {"mp4", "video/mp4"},
            {"mpe", "video/mpeg"},
            {"mpeg", "video/mpeg"},
            {"mpg", "video/mpeg"},
            {"mpga", "audio/mpeg"},
            {"ms", "application/x-troff-ms"},
            {"msh", "model/mesh"},
            {"mxu", "video/vnd.mpegurl"},
            {"nc", "application/x-netcdf"},
            {"oda", "application/oda"},
            {"ogg", "application/ogg"},
            {"pbm", "image/x-portable-bitmap"},
            {"pct", "image/pict"},
            {"pdb", "chemical/x-pdb"},
            {"pdf", "application/pdf"},
            {"pgm", "image/x-portable-graymap"},
            {"pgn", "application/x-chess-pgn"},
            {"pic", "image/pict"},
            {"pict", "image/pict"},
            {"png", "image/png"},
            {"pnm", "image/x-portable-anymap"},
            {"pnt", "image/x-macpaint"},
            {"pntg", "image/x-macpaint"},
            {"ppm", "image/x-portable-pixmap"},
            {"ppt", "application/vnd.ms-powerpoint"},
            {"pptx","application/vnd.openxmlformats-officedocument.presentationml.presentation"},
            {"potx","application/vnd.openxmlformats-officedocument.presentationml.template"},
            {"ppsx","application/vnd.openxmlformats-officedocument.presentationml.slideshow"},
            {"ppam","application/vnd.ms-powerpoint.addin.macroEnabled.12"},
            {"pptm","application/vnd.ms-powerpoint.presentation.macroEnabled.12"},
            {"potm","application/vnd.ms-powerpoint.template.macroEnabled.12"},
            {"ppsm","application/vnd.ms-powerpoint.slideshow.macroEnabled.12"},
            {"ps", "application/postscript"},
            {"qt", "video/quicktime"},
            {"qti", "image/x-quicktime"},
            {"qtif", "image/x-quicktime"},
            {"ra", "audio/x-pn-realaudio"},
            {"ram", "audio/x-pn-realaudio"},
            {"ras", "image/x-cmu-raster"},
            {"rdf", "application/rdf+xml"},
            {"rgb", "image/x-rgb"},
            {"rm", "application/vnd.rn-realmedia"},
            {"roff", "application/x-troff"},
            {"rtf", "text/rtf"},
            {"rtx", "text/richtext"},
            {"sgm", "text/sgml"},
            {"sgml", "text/sgml"},
            {"sh", "application/x-sh"},
            {"shar", "application/x-shar"},
            {"silo", "model/mesh"},
            {"sit", "application/x-stuffit"},
            {"skd", "application/x-koan"},
            {"skm", "application/x-koan"},
            {"skp", "application/x-koan"},
            {"skt", "application/x-koan"},
            {"smi", "application/smil"},
            {"smil", "application/smil"},
            {"snd", "audio/basic"},
            {"so", "application/octet-stream"},
            {"spl", "application/x-futuresplash"},
            {"src", "application/x-wais-source"},
            {"sv4cpio", "application/x-sv4cpio"},
            {"sv4crc", "application/x-sv4crc"},
            {"svg", "image/svg+xml"},
            {"swf", "application/x-shockwave-flash"},
            {"t", "application/x-troff"},
            {"tar", "application/x-tar"},
            {"tcl", "application/x-tcl"},
            {"tex", "application/x-tex"},
            {"texi", "application/x-texinfo"},
            {"texinfo", "application/x-texinfo"},
            {"tif", "image/tiff"},
            {"tiff", "image/tiff"},
            {"tr", "application/x-troff"},
            {"tsv", "text/tab-separated-values"},
            {"txt", "text/plain"},
            {"ustar", "application/x-ustar"},
            {"vcd", "application/x-cdlink"},
            {"vrml", "model/vrml"},
            {"vxml", "application/voicexml+xml"},
            {"wav", "audio/x-wav"},
            {"wbmp", "image/vnd.wap.wbmp"},
            {"wbmxl", "application/vnd.wap.wbxml"},
            {"wml", "text/vnd.wap.wml"},
            {"wmlc", "application/vnd.wap.wmlc"},
            {"wmls", "text/vnd.wap.wmlscript"},
            {"wmlsc", "application/vnd.wap.wmlscriptc"},
            {"wrl", "model/vrml"},
            {"xbm", "image/x-xbitmap"},
            {"xht", "application/xhtml+xml"},
            {"xhtml", "application/xhtml+xml"},
            {"xls", "application/vnd.ms-excel"},
            {"xml", "application/xml"},
            {"xpm", "image/x-xpixmap"},
            {"xsl", "application/xml"},
            {"xlsx","application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {"xltx","application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
            {"xlsm","application/vnd.ms-excel.sheet.macroEnabled.12"},
            {"xltm","application/vnd.ms-excel.template.macroEnabled.12"},
            {"xlam","application/vnd.ms-excel.addin.macroEnabled.12"},
            {"xlsb","application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
            {"xslt", "application/xslt+xml"},
            {"xul", "application/vnd.mozilla.xul+xml"},
            {"xwd", "image/x-xwindowdump"},
            {"xyz", "chemical/x-xyz"},
            {"zip", "application/zip"}
          };

        public static string GetMIMEType(string fileName)
        {
            //get file extension
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (extension.Length > 0 &&
                MIMETypesDictionary.ContainsKey(extension.Remove(0, 1)))
            {
                return MIMETypesDictionary[extension.Remove(0, 1)];
            }
            return "unknown/unknown";
        }
    }
}
