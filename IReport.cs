using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPDFDownloader
{
    //A report on the status of a particular download, which can be displayed by the IMonitor
    //It is EXPECTED to be multithreaded, so care need to be taken to avoid RACE CONDITIONS
    public interface IReport
    {
        public enum Status { Waiting, Downloading, Checking, Saving, Done, DownloadFailed, CheckingFailed };
        
        //No setters available by default, specific implementations may have, but this is to be used by the monitor class, which doesn't need to set


        //Get ALL at the same time, (this ensures that the state doesn't change midway through displaying or whatever)
        public void getAll(out string name, out ulong startMillis, out ulong stopMillis, out IReport.Status status, out int dataSize, out string error, out bool has_fallback);
    }
}
