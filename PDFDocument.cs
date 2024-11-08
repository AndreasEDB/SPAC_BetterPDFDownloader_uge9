using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPDFDownloader
{
    //Container class for a single ongoing or finished PDF download
    //Also contains the URL and fallback
    internal class PDFDocument : IReport, IComparable<PDFDocument>
    {

        //We do not want anybody reading and writing at the same time
        private object _lock = new object();

        //All the individual getters and setters below either use a lock, or are atomic setters (and thus Safe to use in thread)
        //But the values may change between multiple get statements
        //If you want to make sure the variables are read at the same time, use getAll instead.

        //In all cases, I use private data fields (marked with _Name), and public getters and setters (with Name), that way I can wrap the getters and setters in locks, whenever need be

        private ulong _StartMillis;
        public ulong StartMillis {
            get
            {
                //ulong, int and enum have atomic getters, and can safely be read directly
                return _StartMillis;
            }
            set
            {
                //But setting requires checking the lock (To guarantee that getAll works)
                lock (_lock)
                {
                    _StartMillis=value;
                }
            }
        }
        //This works the same way as above
        private ulong _StopMillis;
        public ulong StopMillis {
            get
            {
                return _StopMillis;
            }
            set
            {
                lock (_lock)
                {
                    _StopMillis=value;
                }
            }
        }
        //The size of the downloaded document 
        private int _DataSize;
        public int DataSize
        {
            //Atomic getter, but no setter
            get { return _DataSize; }
            //It is deduced from data or status when they are set
        }
        //The actual PDF data; it is only expected to be stored when the Document has not been saved
        private byte[] _data;
        public byte[] data
        {
            get
            {
                //NON ATOMIC getter
                lock (_lock)
                    return _data;
            }
            set
            {
                lock (_lock)
                {
                    _data=value;
                    _DataSize = _data.Length;
                }
            }
        }
        
        //Enums are secretly int, so they have atomic getters
        private IReport.Status _Status;
        public IReport.Status Status {
            get
            {
                return _Status;
            }
            set
            {
                lock (_lock)
                {
                    //Dump data if we are done with it
                    if (Status==IReport.Status.Waiting || Status==IReport.Status.Done || Status==IReport.Status.DownloadFailed || Status==IReport.Status.CheckingFailed)
                        _data=new byte[0];
                    _Status=value;
                }
            }
        }
        //String has non-atomic getter and setter and must be protected to avoid raceconditions
        //Otherwise they work the same as ints
        private string _Name;
        public string Name
        {
            get
            {
                lock (_lock)
                {
                    return _Name;
                }
            }
            set
            {
                lock (_lock)
                {
                    _Name = value;
                }
            }
        }
        private string _Error;
        public string Error
        {
            get
            {
                lock (_lock)
                {
                    return _Error;
                }
            }
            set
            {
                lock (_lock)
                {
                    _Error = value;
                }
            }
        }
        private string _Url;
        public string Url
        {
            get
            {
                lock (_lock)
                {
                    return _Url;
                }
            }
            set
            {
                lock (_lock)
                {
                    _Url = value;
                }
            }
        }

        //The fallback url may be null, if this is the fallback
        private string? _Fallback_url;
        public string? Fallback_url
        {
            get
            {
                lock (_lock)
                {
                    return _Fallback_url;
                }
            }
            set
            {
                lock (_lock)
                {
                    _Fallback_url = value;
                }
            }
        }

        //This function guarantees that everything was read at the same time,
        public void getAll(out string name, out ulong startMillis, out ulong stopMillis, out IReport.Status status, out int dataSize, out string error, out bool hasFallback)
        {
            lock (_lock)
            {
                name = _Name;
                startMillis = _StartMillis;
                stopMillis = _StopMillis;
                status = _Status;
                dataSize = _DataSize;
                error = _Error;
                hasFallback = _Fallback_url != null;
            }
        }

        //Create a new document to start waiting 
        public PDFDocument(string name, string url, string? fallback)
        {
            _Name=name;
            _Url=url;
            _Fallback_url=fallback;

            //Everything starts empty and waiting
            _DataSize = 0;
            _data = new byte[0];
            _Status = IReport.Status.Waiting;
            _StartMillis = 0;//Not yet started
            _StopMillis = 0;//Not yet started
            _Error = "No error message";//If this ends up printed something has gone wrong
        }
        
        //Create a fallback download of this with its fallback link
        //Return null if this IS the fallback
        public PDFDocument? getFallback()
        {
            lock (_lock)
            {
                if (_Fallback_url != null)
                    return new PDFDocument(_Name, _Fallback_url, null);
                else
                    return null;
            }
        }

        public int CompareTo(PDFDocument? other)
        {
            if (other==null)
                return 1;

            int result = _Name.CompareTo(other._Name);

            if (result != 0) return result;
            else if (other._Fallback_url == null && _Fallback_url != null)
                return -1;
            else if (other._Fallback_url != null && _Fallback_url == null)
                return 1;
            else
                return 0;
        }
    }
}
