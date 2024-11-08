

using BetterPDFDownloader;

class Program
{   
    /*The main function does 4 things:
     *  1 get commandline arguments (if any)
     *  2 Loads the ExcelTables the user asked for (creates them if need be), this takes quite a few lines, since we need to ask the user for permission
     *  3 Launches ConsoleMonitor and DownloadManager with user data (and quits when they are done
     */

    public static async Task Main(string[] args)
    {
        //Read all commandline arguments from the user, to set these variables, which we will pass onto PDFDownloadManager, and our ExcelTables
        
        //Defaults
        int MaxDownloads = -1;//-1 means download all
        uint MaxThreads = 400;//This only applies to DownloadThreads
        bool ForceReload = false;//Delete Metedata file
        uint MaxBandwidth = 300;//Largest number of Mbit per second allowed
        string Metadata_file = "Metadata2017_2020.xlsx";
        string Url_file = "GRI_2017_2020.xlsx";
        string Folder = "Downloads";

        for (int i = 0; i < args.Length; ++i)
        {
            //Look for any of our commands, I look case-insensitive
            switch (args[i].ToLower())
            {
                //Force and Help are single commands, they are easy to deal with
                case "force":
                    ForceReload = true;
                    break;
               
                case "help":
                    Console.WriteLine("Run with the following arguments:");
                    Console.WriteLine("threads int [sets max threads for Download task (default 50)]");
                    Console.WriteLine("bandwidth uint [sets max MBits/second (default 300)]");
                    Console.WriteLine("maxdownloads uint [only attempt this manye downloads (continue from where we left off) (default unlimited)]");
                    Console.WriteLine("force [do not skip already downloaded files, (default do not skip)]");
                    Console.WriteLine($"metadata filename [use another excel file as metadata (default { Metadata_file})]");
                    Console.WriteLine($"data filename [load urls from another excel file (default {Url_file})]");
                    return;//Stop the program, so the user has time to read this
                case "data":
                    //Test if the next argument is within range
                    if (i + 1 < args.Length)
                    {
                        //Then read it, and skip it
                        Url_file= args[i + 1];
                        ++i;
                    }
                    break;
                    //Same for these other commands which take a file as input
                case "metadata":
                    if (i + 1 < args.Length)
                    {
                        ++i;
                        Metadata_file = args[i + 1];
                    }
                    break;
                case "output":
                    if (i + 1 < args.Length)
                    {
                        ++i;
                        Folder = args[i + 1];
                    }
                    break;

                //The commands with integer argument is a little different
                case "maxdownloads":
                    // This is false if argument in range, only if that is so, we try to get the MaxDownloads as an int
                    if (i + 1 >= args.Length              || !int.TryParse(args[i + 1], out MaxDownloads))
                        //If we go here, we could not read the command, set to deafault (-1 means load everything)
                        MaxDownloads = -1;
                    else
                        //If we got here, MaxDownloads contains the target data, just skip the next command
                        ++i;
                    break;

                //Same for all other commands with integer argumetns
                case "bandwidth":
                    if (i + 1 >= args.Length || !uint.TryParse(args[i + 1], out MaxBandwidth) || MaxBandwidth < 50)
                        MaxBandwidth = 300;
                    else
                        ++i;
                    break;
                case "threads":
                    if (i + 1 >= args.Length || !uint.TryParse(args[i + 1], out MaxThreads) || MaxThreads < 1)
                        MaxThreads = 50;
                    else
                        ++i;
                    break;
                default://Skip invalid commands
                    break;
            }
        }



        ExcelTable urls;
        try
        {
            urls = new ExcelTable(Url_file);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open file \"{Url_file}\" with URLs:");
            Console.WriteLine(ex.Message);
            Console.WriteLine($"HINT: You can tell the program to load another file by running with argument \"data filename\"");
            return;//There is nothing we can do, the user need to supply a new file
        }

        ExcelTable metadata;
        
        //This is not particularly elegant, if we are not doing force-reload, the user gets the option to overwrite the file.
        if (!ForceReload)
        {

            try
            {
                metadata = new ExcelTable(Metadata_file,true,ForceReload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open file \"{Metadata_file}\" with metadata");
                Console.WriteLine(ex.Message);
                Console.WriteLine($"HINT: You can tell the program to load another file by running with argument \"metadata filename\"");
                Console.WriteLine();
                Console.WriteLine("Do you want to OVERWRITE and RECREATE it (This will force a total restart of the download?)");
                Console.WriteLine();

                Console.Write("Answer with y or n:");

                //Loop until the user gives me a working input
                while (true)
                {
                    char c = Console.ReadKey().KeyChar;

                    if (c == 'y')
                    {
                        Console.WriteLine($"\nCreating new metadata...");
                        try
                        {
                            metadata = new ExcelTable(Metadata_file,true,true);
                        }
                        catch (Exception ex1)
                        {
                            Console.WriteLine($"Could not create new file \"{Metadata_file}\" with metadata");
                            Console.WriteLine(ex1.Message);
                            Console.WriteLine($"HINT: You can tell the program to load another file by running with argument \"metadata filename\"");
                            return;//Nothing we can do
                        }

                        //Try again, with forceReload on, if this doesn't work there is nothing we can do
                        break;
                    }
                    else if (c == 'n')
                    {
                        Console.WriteLine($"\nAs {Metadata_file} could not be opened, we can not proceed");
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"\nWrite either y (create {Metadata_file}) or n (quit program)");
                    }
                }

            }
        }
        else
        {
            try
            {
                metadata = new ExcelTable(Metadata_file,true,true);
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"Could not create new file \"{Metadata_file}\" with metadata");
                Console.WriteLine(ex1.Message);
                Console.WriteLine($"HINT: You can tell the program to load another file by running with argument \"metadata filename\"");
                return;//Nothing we can do
            }
        }

        //Now start the manager and monitor
        ConsoleMonitor monitor = new ConsoleMonitor(1000);//Refresh roughly every second
        DownloadManager manager = new DownloadManager(MaxThreads, MaxBandwidth,MaxDownloads,Folder);

        await manager.Download(urls, metadata,monitor);

    }
}
