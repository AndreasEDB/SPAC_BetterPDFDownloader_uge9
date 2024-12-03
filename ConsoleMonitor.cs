using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace BetterPDFDownloader
{
    //Print a histogram of all the statuses to console once every few milliseconds
    public class ConsoleMonitor : IMonitor
    {
        //Delay between refreshes
        public int RefreshMillis { get; set; }
        private bool ShouldStop;

        private object _lock = new();
        private string Title;

        //A Frozen list of reports, as they were when I received them I DO NOT store a reference to the reports, as more may be added WHILE we loop through, which can result in crashes during foreach
        private List<IReport> myReports;//The INDIVIDUAL reports may still be modified in other threads, that is ok, they are meant to be thread-safe

        public void setReport(IEnumerable<IReport> Reports)
        {
            lock (_lock)
            {
                //This does a shallow copy
                myReports = Reports.ToList();
            }
        }


        public void setTitle(string title)
        {
            lock (_lock)
            {
                Title = title;
            }
        }

        //DO NOT USE TOO SMALL REFRESH
        //That will cause annoying flickering AND drain resources from the downloader
        public ConsoleMonitor(int refreshMillis = 500/*Anything below 500 ms is borderling unwatchable due to flickering*/)
        {
            myReports= new List<IReport>();
            Title = "Title";
            RefreshMillis = refreshMillis;
            ShouldStop = false;
        }

        //A custom comparer for sorting errorlogs by ms they occured, and by the name of the things causing the error
        //I ASSUME names are practically unqiue each mss (fallbacks do have the same name, but they CAN NOT fail on the same ms as the original)
        private class CompareErrors : IComparer<(ulong, string)>
        {
            public int Compare((ulong, string) A, (ulong, string) B)
            {
                //Newest first
                int result = B.Item1.CompareTo(A.Item1);

                if (result == 0)
                    return B.Item2.CompareTo(A.Item2);
                else
                    return result;
            }

        }


        //A single display call
        public void DisplaySingle(Stopwatch stopwatch, IEnumerable<IReport> reports)
        {

            lock (_lock)
            {
                long framestart = stopwatch.ElapsedMilliseconds;
                uint TasksWaiting = 0;
                uint TasksDownloading = 0;
                uint TasksCheckingSaving = 0;
                uint TasksDone = 0;
                uint TasksFailedDownload = 0;
                uint TasksFailedCheck = 0;

                //Logs of all things which failed, sorted by when they occured, using a custom comparer for sorting errors at the same ms by name

                SortedList<(ulong, string), string> RecentfailedLogs = new SortedList<(ulong, string), string>(new CompareErrors());
                //In this case, we ASSUME that reports may not be changed by other threads will running
                foreach (IReport report in reports)
                {
                    report.getAll(out string name, out ulong startMillis, out ulong stopMillis, out IReport.Status status, out int dataSize, out string error, out bool has_fallback);
                    switch (status)
                    {
                        default:
                        case IReport.Status.Waiting:
                            ++TasksWaiting;
                            break;
                        case IReport.Status.Downloading:
                            ++TasksDownloading;
                            break;
                        case IReport.Status.Checking:
                            ++TasksCheckingSaving;
                            break;
                        case IReport.Status.DownloadFailed:
                            RecentfailedLogs.Add((stopMillis, name), error);
                            ++TasksFailedDownload;
                            break;
                        case IReport.Status.CheckingFailed:
                            RecentfailedLogs.Add((stopMillis, name), error);
                            ++TasksFailedCheck;
                            break;
                        case IReport.Status.Done:
                            ++TasksDone;
                            break;
                    }

                }

                uint sum = TasksWaiting + TasksDownloading + TasksCheckingSaving + TasksDone + TasksFailedDownload + TasksFailedCheck;

                //Print the task name, how many, how many percent, and a bar ===| indicating how big a fraction of total it is

                //A quick little functions to help me do that
                var descripe = (string action, uint number) =>
                {
                    string Text = $"{action}:{number} ({((100 * number) < sum ? (number == 0 ? "0" : "<1%") : $"{(uint)(100 * number) / sum}%")})";

                    if (Text.Length + 1 < Console.WindowWidth)
                    {
                        int barlength = (int)((number * (Console.WindowWidth - Text.Length - 1)) / sum);
                        Text = Text + new string('=', (barlength)) + "|";

                    }
                    return Text;
                };


                //For cleaner display, load everything we want to display as as few strings as possible before printing
                string output = $"T={framestart / 1000}.{framestart % 1000} total tasks ({sum})\n" +
                descripe("Waiting", TasksWaiting) + "\n" +
                descripe("Downloading", TasksDownloading) + "\n" +
                descripe("Checking and Saving", TasksCheckingSaving) + "\n" +
                descripe("Saved", TasksDone) + "\n" +
                descripe("Failed during download", TasksFailedDownload) + "\n" +
                descripe("Failed to save or check", TasksFailedCheck) + "\n";
                
                
                string error_title = "##Most recent errors"+new string('#',Math.Max(0,Console.WindowWidth-21))+"\n";

                string error_list="";
                int height = 10;
                foreach (var log in RecentfailedLogs)
                {
                    if (height + 2 > Console.WindowHeight)
                    {
                        error_list += "... You can see the rest in the Metadata file when we are done ...";

                        break;
                    }

                    //It is possible for this to be after the framestart, due to the slight delay between calling the stopwatch and loading the data
                    long time_since = framestart - (long)log.Key.Item1;

                    //Print the error, if we get outside the screen print 3 dots (The user must enlarge the display)
                    string errortext = $"{(time_since<0?"now":$"{time_since} ms ago")} {log.Key.Item2}:{log.Value}";

                    if (errortext.Length > Console.WindowWidth)
                    {
                        errortext = errortext.Substring(0, Console.WindowWidth - 3) + "...";
                    }
                    error_list += errortext+="\n";
                    ++height;

                }


                //This does not ELIMINATE the flicker, as both Clear and Write refreshes, a double-buffer (similar to most 3D or 2D graphics) is better, but not natively supported
                //A longer refreshrate helps keep the flickering manageble
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(Title,ConsoleColor.Green);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(output,ConsoleColor.White);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(error_title,ConsoleColor.Yellow);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(error_list,ConsoleColor.Red);

                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        //The display function is a void function, wrapped inside a Task runner, that way we can easily have it run in one of our threads
        //And since it sleeps most of the time, it will not be a huge performance drain
        public Task Display(Stopwatch stopwatch)
        {
            ShouldStop = false;
            return Task.Run(async () =>
            {
                //Reading a boolean is an atomic operation, so there is no risk of race conditions
                do
                {
                    //Put this thread on hold for as long as it takes, until we are an integer number of RefreshMillis from the start
                    long framestart = stopwatch.ElapsedMilliseconds;

                    DisplaySingle(stopwatch, myReports);

                    //Needless to say, the Delay function is HILLARIOUSLY bad at hitting these intervals
                    int waitTime = (int)((framestart + RefreshMillis) - framestart % RefreshMillis - framestart);

                    if (waitTime > 0)
                        await Task.Delay((int)(RefreshMillis - stopwatch.ElapsedMilliseconds + framestart));
                } while (!ShouldStop);
            });
        }

        public void Stop()
        {
            //No need to wrap this in a Lock, assigning a boolean is an atomic operation
            ShouldStop = true;
        }
    }
}
