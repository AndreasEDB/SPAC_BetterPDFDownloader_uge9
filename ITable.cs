using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BetterPDFDownloader
{
    //Any table, which can contain columns of strings with headers
    public interface ITable
    {
        //Read the entire column
        public string[] GetCol(string header);

        //Append to an existing column, or create a new, data may be empty
        public void AddCol(string header, string[] data);

        //Saves data to computer, MAY be a very time-consuming process
        public void Save();
    }
}
