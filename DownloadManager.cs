using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Diagnostics;

namespace BetterPDFDownloader
{
    //The class which manage and initiates download, check, and save tasks
    internal class DownloadManager
    {
        private uint MaxThreads;
        private uint MaxBandwidth;
        private int MaxDownloads;//-1 indicates all
        private string OutputFolder;

        //Set up basic user settings
        public DownloadManager(uint maxThreads, uint maxBandwidth, int maxDownloads, string outputFolder)
        {
            MaxThreads = maxThreads;
            MaxBandwidth = maxBandwidth;
            MaxDownloads = maxDownloads;
            OutputFolder = outputFolder;

            //Make sure the output folder exists
            if (!Directory.Exists(OutputFolder))
            {
                Console.WriteLine("Creating folder " + OutputFolder);
                Directory.CreateDirectory(OutputFolder);
            }
        }


        //A single document (which already has been downloaded) needs to be checked and saved, return another document if we have to try again because it failed
        private Task<PDFDocument?> CheckSave(PDFDocument document)
        {
            return Task.Run(
                () =>
                {

                    document.Status = IReport.Status.Checking;
                    try
                    {
                        //We are going to do two checks, one fast but unrealiable, and one slow but reliable

                        //Easy check (will catch almost all invalid result) is the header ok (first 4 bytes must be %PDF)
                        //WARNING: Malicious executables disguised as PDF, or PDF files which have been corrupted on the server side will get through
                        if (!(document.data.Length > 3 && document.data[0] == '%' && document.data[1] == 'P' && document.data[2] == 'D' && document.data[3] == 'F'))
                        {
                            document.Error = "Document invalid: No PDF header";
                            document.Status = IReport.Status.CheckingFailed;
                            return document.getFallback();//Returns null if this IS the fallback document
                        }
                        //If that worked, use PDFSharp (third party library) to verify that it is a pdf
                        //This takes longer, but should catch empty pdf files, and most malicious files masquerading as PDF
                        try
                        {
                            using (MemoryStream ms = new MemoryStream(document.data))
                            {
                                PdfDocument this_document = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
                                if (this_document.PageCount <= 0)
                                {
                                    document.Error = "Document invalid:contained 0 pages";
                                    document.Status = IReport.Status.CheckingFailed;
                                    return document.getFallback();//Returns null if this IS the fallback document
                                }
                            }
                        }
                        catch (Exception E)
                        {
                            document.Error = $"Document failed to open: {E.Message}";
                            document.Status = IReport.Status.CheckingFailed;
                            return document.getFallback();//Returns null if this IS the fallback document
                        }


                        //Null output may be used when debugging to not save
                        if (OutputFolder != null)
                        {
                            try
                            {
                                File.WriteAllBytes(Path.ChangeExtension(Path.Combine(OutputFolder,document.Name),".pdf"), document.data);
                            }
                            catch (Exception e)
                            {
                                document.Error = $"Document failed to save: {e.Message}";
                                document.Status = IReport.Status.CheckingFailed;
                                return document.getFallback();//Returns null if this IS the fallback document
                            }
                        }
                        //This setter by the way dumps data
                        document.Status = IReport.Status.Done;
                        return null;//Nothing new

                    }
                    catch (Exception e)
                    {
                        document.Error = "Check failed with exception: " + e.Message;
                        document.Status = IReport.Status.CheckingFailed;
                        return document.getFallback();//Returns null if this IS the fallback document
                    }

                }
            );
        }



        //A single document download, return another document if we have to try again because it failed
        private async Task<PDFDocument?> Download(SemaphoreSlim semaphore, PDFDocument document, HttpClient Client, Stopwatch sw, List<Task<PDFDocument?>> CheckAndSave)
        {
            semaphore.Wait();
            document.Status = IReport.Status.Downloading;
            document.StartMillis = (ulong)sw.ElapsedMilliseconds;
            try
            {

                //If source is not a valid URL this fails quickly
                document.data = await Client.GetByteArrayAsync(document.Url);
                document.Status = IReport.Status.Checking;
            }
            catch (Exception e)
            {
                document.Error = "Downloaderror : " + e.Message;
                document.Status = IReport.Status.DownloadFailed;
                document.StopMillis = (ulong)sw.ElapsedMilliseconds;
                semaphore.Release();
                return document.getFallback();//Returns null if this IS the fallback document
            }
            document.StopMillis = (ulong)sw.ElapsedMilliseconds;
            CheckAndSave.Add( CheckSave(document));
            semaphore.Release();
            return null;//Nothing new
        }

        public async Task Download(ITable url_table, ITable metadata_table, IMonitor monitor)
        {
            HttpClient Client = new HttpClient();
            //This clock is shared between us and the IMonitor
            Stopwatch sw = Stopwatch.StartNew();

            Console.WriteLine("Loading from Table... please wait");
            //Load names,urls and fallbacks
            string[] names = url_table.GetCol("BRnum");
            string[] urls = url_table.GetCol("Pdf_URL");
            string[] fallback_urls = url_table.GetCol("Database link");

            if (names.Length != urls.Length || names.Length != fallback_urls.Length)
            {
                Console.WriteLine("Urls, Database links and BRnumbers did not have matching lengths");
                return;
            }

            //Now load names from the metadata table, as a hash-set for easy lookup
            HashSet<string> existing_names = new HashSet<string>(metadata_table.GetCol("BRnum"));

            //Now create PDFDocuments for all the things we need to download
            List<PDFDocument> documents = new List<PDFDocument>();

            //Loop through until we reach the end of the list, or have downloaded we are allowed to (maxDownloads<0 means download the entire list)
            for (int i = 0, toDownload = MaxDownloads < 0 ? names.Length : MaxDownloads; toDownload > 0 && i < names.Length; i++)
                if (!existing_names.Contains(names[i]))
                {
                    --toDownload;//Decrement this counter, only when we actually have added something
                    documents.Add(new PDFDocument(names[i], urls[i], fallback_urls[i]));
                }

            monitor.setTitle("(step 1/3) Loading all from primary URL!");
            //Start the monitor display task now, since this MAY print to the console, I will not do any console prints until we stop it
            monitor.setReport(documents);
            Task display = monitor.Display(sw);

            //I keep MaxThreads as an uint, even though I have to cast it back to int, since I want to be sure I don't accidentally send in a negative number
            using (var semaphore = new SemaphoreSlim((int)MaxThreads))
            {

                //A list of fallback documents, which we may need to read after we are done with the first batch
                List<PDFDocument> FallbackDocs = new();

                //The return value of the tasks below are new documents, we need to add to the download task;

                //Download tasks, these ARE limited in number (to have mercy on the internet)
                List<Task<PDFDocument?>> Downloads = new();

                //Check validity and Save, there is NO LIMIT to how many of these there can be
                //Either CheckAndSave, or Download tasks may spawn new Download tasks if the primary URL fails
                List<Task<PDFDocument?>> CheckAndSave = new();

                //Upload download tasks
                foreach (var document in documents)
                    Downloads.Add(Download(semaphore, document, Client, sw, CheckAndSave));


                var resDownloads = await Task.WhenAll(Downloads);
                var resCheck = await Task.WhenAll(CheckAndSave);

                //Now do it again, I could use a do while statement, and check if downloads is empty ... but I KNOW there will only be at most 1 set of fallbacks
                monitor.setTitle("(step 2/3) Loading all from fallback URL");

                Downloads = new();
                CheckAndSave = new();

                foreach (var document in resDownloads)
                {
                    if (document!=null)
                    {
                        documents.Add(document);
                        Downloads.Add(Download(semaphore, document, Client, sw, CheckAndSave));
                    }
                }
                monitor.setReport(documents);
                
                await Task.WhenAll(Downloads);
                await Task.WhenAll(CheckAndSave);
            }



            monitor.setTitle("(step 3/3) Saving metadata table");

            //Convert everything to a list of names, statuses, and errors,
            //This writes fallback urls as separate entries, which is what I want

            //First sort everything (this places the fallbacks, next to the principle documents)
            documents.Sort();
            
            //Now write everything in a bunch of arrays
            var new_names    = new string[documents.Count];

            //This list simply says if we are fallback
            var new_is_fallback= new string[documents.Count];
            var new_statuses = new string[documents.Count];
            var new_errors   = new string[documents.Count];

            for (int i = 0; i < documents.Count; i++)
            {
                documents[i].getAll(out new_names[i], out ulong startMillis, out ulong stopMillis, out IReport.Status status, out int dataSize, out new_errors[i], out bool has_fallback);
                new_is_fallback[i] = (has_fallback ? "No" : "Yes");
                new_statuses[i] = (status == IReport.Status.Done ? "Yes" : "No");
            }
            
            metadata_table.AddCol("BRnum", new_names);
            metadata_table.AddCol("Fallback", new_is_fallback);
            metadata_table.AddCol("Downloaded", new_statuses);
            metadata_table.AddCol("Error", new_errors);
            metadata_table.Save();
            sw.Stop();
            monitor.Stop();
            await display;
            

            monitor.DisplaySingle(sw, documents);//One last time
        }
    }
}
