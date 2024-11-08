using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace BetterPDFDownloader
{
    internal class ExcelTable : ITable, IDisposable
    {
        private FileInfo address;
        //The underlying package
        private ExcelPackage? package;
        private bool writable;

        private Dictionary<string, int> Headers;//For fast lookup, this dictionary stores the column with this header

        //Throws exceptions if you try opening a file you can, or marking a file as writable, which you can't write to
        public ExcelTable(string filename,bool _writable=false,bool overwrite=false)
        {
            //REALISTICALLY, this should be set to commercial
            ExcelPackage.LicenseContext=LicenseContext.NonCommercial;
            writable = _writable;
            address = new FileInfo(filename);
            package = null;//If something went wrong during loading, the package may not have been loaded
            

            if (overwrite)
            {
                //Create an empty package, with a front worksheet (which is all we want)
                package = new ExcelPackage();
                ExcelWorksheet newWorksheet = package.Workbook.Worksheets.Add("worksheet0");
            }
            else
            {   
                //Throws an exception if the file can not be opened
                //I will NOT default to overwriting a file which couldn't 
                package = new ExcelPackage(filename);
                ExcelWorksheet loadedWorksheet = package.Workbook.Worksheets[0];
            }

            //Either way, let us loop through the top row and list our headers
            //this throws an exception if the file isn't working, which is 100% intended
            Headers = new Dictionary<string, int>();

            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension!=null)
            {
                //Loop through the columns, until we find the header
                int cols = worksheet.Dimension.Columns;
                int rows = worksheet.Dimension.Rows;

                var Cells = worksheet.Cells;
                //BASE 1 indexing because Excel is stupid;
                for (int i =1; i <= cols; i++)
                {
                    if (Cells[1, i].Text.Length>0)
                    {
                        if (Headers.ContainsKey(Cells[1, i].Text))
                            throw new ArgumentException($"The Excel table has duplicate header entry {Cells[1, i].Text}!");
                        Headers[Cells[1, i].Text]=i;
                    }
                }
            }


            if (writable)
            {
                //Try saving it, this will throw an exception if we do not have permission to save
                //Better to do that NOW than after we have put data in here
                Save();

            }
        }

        public void AddCol(string header, string[] data)
        {
            if (package == null)
                return;

            //Append to existing column
            if (Headers.TryGetValue(header, out int i))
            {
                //IF we get here, we know the worksheet is not null! (because otherwise the Headers list would be empty)
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rows = worksheet.Dimension.Rows;
                
                for (int j = 1; j <= data.Length; j++)
                    //Base 1 indexing, which excel uses, is annoying to combine with base 0 which C# uses, but I have no choice
                    worksheet.Cells[j+rows, i].Value=data[j-1];
            }
            else
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                int newCol = 1;//Stupid 1 based index
                if (worksheet.Dimension!=null)
                    newCol = worksheet.Dimension.Columns+1;
                Headers.Add(header, newCol);

                worksheet.Cells[1,newCol].Value=header;
                for (int j = 1; j <= data.Length; j++)
                    //Base 1 indexing, which excel uses, is annoying to combine with base 0 which C# uses, but I have no choice
                    worksheet.Cells[j+1/*Skip header*/, newCol].Value=data[j-1];
            }

            //No save, that would be too slow
        }

        public void Dispose()
        {
            package?.Dispose();
        }
        
        //Get the entire column under this header, return an empty array if it is not there
        public string[] GetCol(string header)
        {
            if (package == null)
                return new string[0];
            if (Headers.TryGetValue(header, out int i))
            {
                //IF we get here, we know the worksheet is not null! (because otherwise the Headers list would be empty)
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rows = worksheet.Dimension.Rows;
                var Out = new string[rows-1];

                for (int j = 2; j <= rows; j++)//Skip header
                    Out[j-2] = worksheet.Cells[j, i].Text;
                return Out;
            }
            else
                return new string[0];
        }

        public void Save()
        {
            if (writable)
                package?.SaveAs(address);
        }
    }
}
