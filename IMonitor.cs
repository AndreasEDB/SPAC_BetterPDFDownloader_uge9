using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace BetterPDFDownloader
{
    //An interface the PDF downloader can use to display, in my case it will print a histogram and some text to the console
    //But it could just as easily be a GUI
    internal interface IMonitor
    {
        //The display is a task, which runs in a thread in the backgroun
        //Initialize the display, by syncing up this stopwatch
        public Task Display(Stopwatch stopwatch);

        //Copy the data we are to display in the task
        public void setReport(IEnumerable<IReport> Reports);

        //A single display call
        public void DisplaySingle(Stopwatch stopwatch,IEnumerable<IReport> reports);
        public void Stop();

        public void setTitle(string title);
    }
}
