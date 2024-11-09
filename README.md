# BetterPDFDownloader
This Readme file is in English, even though the requirements were in Danish. This is done for 3 reasons: 1 The code is in English, 2 it is easier to explain software in English, 3 to allow me to share this project with people or companies outside Denmark.

This is the second attempt at solving this exercise, not because I failed, I just finished my [first attempt](https://github.com/nikolajRoager/SPAC_PDFDownloader) faster than I expected, so I made this second attempt.

This attempt is far better, and should be considered my final submission.

The requirement file (SPAC kravspecifikation.docx) are copied from the old project, and largely reflects the structure of the old project, based on my philosophy that requirements should not be changed. It also includes references to the requirement of NAS servers, which was actually not required.

Much of the code has been copied from the old project, the new project simply has a much better structure

## Lessons learned the exercise, and focus for people correcting this

The main lesson learned, is that C# is really good at handling multithreading.

Declaring `Async Task`s, and using `Task.AwaitAll(...)` or `Await` to sync up tasks means that the programmer doesn't need to worry about which threads should be assigned to what, and where computing resources may be wasted.

This is especially great when I do not know ahead of time which Download tasks will have to wait a long time, and which won't.

When you correct this, try to see if the structure of this new submission is reasonable. Especially focus on whether or not my classes follow SOLID principles.

My second attempt comes much closer to the ideals, especially of Dependency inversion and substitution (since the DownloadManager would still work, if I replaced the `ExcelTable` or `ConsoleMonitor` with any other `ITable` or `IMonitor`, if, for instance, I was told to get this to work with CSV files and printing to a GUI)

I still think there are some issues, for one thing, right now my DownloadManager depends directly on `PDFDocument`, and is responsible for checking the validity of the PDF, making it not quite *Single Responsibility* and making it impossible to substitute the `PDFDocument` class with any other class (for example `WORDDocument` or `HTMLDocument`). In retrospect, I should have made an `IDocument` interface for `PDFDocument`, with its own `public bool validate()` function, and used that in `DownloadManager` instead of `PDFDocument` itself.

Still, since the requirement SPECIFICALLY was for a PDF downloader, it is debatable if making the program general enough to handle other document formats even makes sense.

Please also focus on how readable this Readme file is, and how likely it is that someone who doesn't know my code could use my program based on it.

(Addition, after deadline): Also check if my code is thread-safe (against race-conditions), especially the PDFDocument which may need to be read by the monitor, while it is modified. I have protected all setters with locks, and I believe getters are atomic operations (which I believe to be thread-safe)

NOTE: DO remember to read the "How to start this program" section at in this document, to see what default arguments I use in Visual studio.


In the rest of the document, I will try to play the role of a Consultant hired by a company to make this program (as suggested by the exercise)

## To the customer
This program, written in C# and .NET, Downloads the documents from the Table you requested, and I believe it is not only faster and more stable, but at least as safe as what you had before.

The program was made in C#, because it offers better performance than Python, and has the same multithreading functionalities as Python, and also because C# packages for handling PDF files and Excel files are readily available.

During my testing, I found that all of the alternative urls you provided do not work (they redirect to your homepage, which is not a valid PDF), even so my Program still looks at the alternative url, and I trust that you can fix this problem (perhaps the links only work on your network, or perhaps you need to update the data, either way my program should be able to handle it)

### Speed and stability
Your old program was single-threaded, requesting only one document at the time. This is somewhat limiting, as a single slow download would halt the entire program.

The new program uses several threads, meaning that while we are waiting for response from a slow server, we can ask other servers for files.

The program is still limited by available bandwidths: Downloading too many PDFs at the same time can, and will, slow the internet down (rendering other online services unusable, and possibly causing all downloads to fail due to time-out).

For this reason the program gives you a number of variables, which allows you to trade speed of the program, for bandwidth usage or the number of succesful downloads.

Firstly, the program allows you to define how many threads can be used to download, by default the Program uses up to 50 which makes the program relatively slow, but works well on a Wireless connection.
To speed up the program, you should use a wired connection and run with more threads, for example using the command `threads 400` to run in 400 threads; (See How to start this program for more details).

You can also modify the timeout of connections using the, by default we wait 100 seconds for a slow download, you can set this number lower (for example  `timeout 10` for 10 seconds) if you want to skip pdf documents on slow hosts. Alternatively you can set it higher, if you are willing to wait longer for even the slowest hosts.  (WARNING, I do not recommend setting it higher than at the very most 300 seconds (5 minutes) since there will always be some hosts which do not reply at all)

You can also start only a select number of downloads using the `maxdownloads` command, doing so allows you to download the entire library over multiple runs.


I have carried out extensive tests of my program, and I believe it handles most errors and exceptions without crashing or stopping.
But **YOU MUST NEVER** open or edit the Excel Metadata document while starting or running the program. This is because having the files open in Excel prevents any saving of data from any other programs (on Windows at least).
The program will detect this and fail safely, but this may result in the loss of all Metadata from this run.

### Validity and Safety checks
You did not ask for validity and safety checks.

but your original solution did include a basic validity check (although they were commented out, maybe to speed up the program), and I consider checking that all downloaded files are valid PDFs as absolutely essential, so I have included the same checks in my program.

This program does two basic checks: first it checks that the file has a valid PDF header. This rules out all the cases where the link returns an html document instead (which is the vast majority of invalid documents). 

Finally, the program tries opening the pdf, and checking that it contains more than 0 pages. This rules out any pdf files, which have been corrupted on the hosts part, and may even catch some malicious files masquerading as PDFs.

More sophisticated malicious files may still slip through, so a proper anti-virus scan should never be neglected.

The time it takes to do these checks is much less than the downloads, and the program is able to run the checks in another thread while waiting for other documents to download, so these checks do not significantly hinder performance, and I consider these checks non-negotiable.

When you run the program, you can see how many pdf documents is going through the checking process at any time, and how many failed specifically due to these tests.

### How to read the output (while running)
While the program is running, a histogram will be printed to the console.

At the top of this histogram, you see the current time (of this refresh), and how many tasks (in our case documents) currently are running.

This Histogram updates every second, and shows how many downloads are currently awaiting start, downloading, or being checked and saved. It also shows how many are either done, or failed during download or checking.

Below the Histogram, you will see the most recent error messages.

There may be too many error messages to fit on the screen, and they may be too wide to fit on the screen (both are indicated by ...), you can try to resize the console, but you can also just wait for the program to finish, in which case the errors will all be saved to the metadata file

The program will first read all files from the primary url, then it will run again, this time with the alternative url of the documents (This will cause the total number of tasks to increase) which failed in the first case. (The Title of the histogram will reflect this)

Towards the end, the program may look like it is stuck, because the slowest connections tend to be the last left, this is likely at the hosts part and you will just have to wait.

### How to read the output (when the program is done)
The Output folder, and Metadata output can be specified by the user (see next section), or you can use defaults.

When the program runs, it continuously saves pdf documents which passed all validation checks to the output folder.

When the program is done, it updates the Metadata excel file.

The program writes to 4 columns in the Metadata file. These columns have header `BRnum`, and are the BR number of the report, `Fallback` are yes or no, depending if this is the primary or alternative url (This means BRnum may be duplicated, in cases where we did check the alternative url, if this is not acceptable I can easily remove that); `Downloaded` contains either `yes` or `no`, it will be no if the file could not be saved, either because it failed to download, or because it failed to validate; and the final column `Error` contains the error message if the download or validation failed, it is empty if there are no errors.

The error column was not required by you, but I saw no reason to not include it.

### How to start this program
This git project does not include a build executable, or downloaded documents (As that would be too much data to put on Github).
You have to compile and run the program on your own to get them. Simply press F5 in Visual studio.

If you run this Project in Visual Studio, do keep in mind that I use default command-line argument "maxdownloads 100" when building from Visual Studio, this means we only download 100 documents in a single go (to spare the internet connection).

You can delete this argument if you want to see the program loading all documents, but this may put serious strain on the.

You can also add any other arguments, to do this go to debug -> BetterPDFDownloaderProperties and change the text box with properties (Or, if you run from the terminal, add commands here).

You can run with the following commands:

		threads integer

Where `integer` is any whole number above 0, this controls how many download requests we can send simultaneously. More makes the program faster, but may overload your internet connection. Default is 50, which should work fine even for a wireless connection, for an ethernet connection you can use more (like 400). (Really you shouldn't use a wireless connection at all).

Ultimately you have to experiment with what works for you

		maxdownloads integer


Where `integer` is any whole number above 0, if this is set we will only attempt this many downloads (If the metadata file is not empty, and the `force` flag (below) is not set, we will continue from where we last left off), if no command is given ALL downloads will be attempted (In this Visual Studio project, I run with argument `maxdownloads 100`, you can change that under debug properties)

		force

This command forces the program to delete and overwrite the metadata file, and re-download all files we already have

		metadata filename

use another excel file as metadata (default `Metadata2017_2020.xlsx`)
WARNING, the program can not use a metadata file you have open in another program. If the file is not usable (or doesn't exist), you will be prompted if you want to create a new file with that name (this effectively sets `force` and OVERWRITES the file it it already exists).
The first time you run, you will likely be asked to create a new file (since it doesn't exist yet), just  answer `y` (meaning yes) to that.
However, if you DO have the file open in another program, you should answer `n` (meaning no), close the file, and try again.
If the file exist, but the program still fails, check that you have permission to read abd write

		data filename

load urls from another excel file (default `default GRI_2017_2020.xlsx`)
Again, you should not have the file open in another program.
The program will fail if you do not have read permission, or the file doesn't exist.

The file must contain columns with the header `BRnum`, `Pdf_URL` amd `Database link` in row 1 (The program does not care where the column is, only that it has the header in the row 1).

		output foldername

Save downloaded pdfs in this folder, if it doesn't exist yet, it will be created (provided that you have permission to do that)

		help

Print this list of commands;

## Structure of the program, Modularity and Future work
Here is an UML diagram of the class structure of the program (I simply draw it as text, because using an actual UML editor seemed too hard):











                                                                          |<<class PDFDocument>>                |
                                                                          |-------------------------------------|___implements____\ |<<.NET IComparable<PDFDocument>>>   |
                                                                          |+name : string                       |                 / |====================================|
                                                                          |+startMillis : ulong                 |                   |+CompareTo(PDFDocument or null):bool|
           |<<Interface: IReport>>                        |               |+stopMillis  : ulong                 |
           |==============================================|               |+status      : enum                  |
           |+getData(out string name,   :threadsafe getter|               |+ datasize   : int                   |
           |     out ulong startMillis,                   |/__implements__|+ data       : byte[]                |
           |     out ulong stopMillis                     |\              |+ url        : string                |
           |     out enum Status                          |               |- fallback   : string                |
           |     out int datasize                         |               |-------------------------------------|
           |     out string error)                        |               |+getFallback(): string or null       |
                           /:\                                                            /|\ *
                            :                                                              |
                            :                                                           Owns 0 or more
                            :                                                             /=\ 1
                            :                                                             \=/
                         depends on                             |<<Class: Download manager>>                                                               |
                         and uses                               |------------------------------------------------------------------------------------------|
                            :                                   |-MaxThreas    : positive int                                                              |
     |<<Interface: IMonitor>>                 |                 |-Timeout      : positive int                                                              |
     |========================================|                 |-MaxDownloads : int (negative for all)                                                    |
     |+Display(stopwatch) : Async display task|                 |-OutputFolder : filepath                                                                  |
     |+SetReport(List of IReport): Set data   |/..depends.on....|------------------------------------------------------------------------------------------|
     |                             for display|\  and uses      |+DownloadManager(MaxThreads,                                                              |
     |+Stop() : void                          |                 |                    Timeout,                                                              |
     |+setTitle: void                         |                 |               MaxDownloads,                                                              |               |<<Interface: ITable>>               |
                                   /|\                          |               Outputfolder) : constructor                                                |               |====================================|
                                    |                           |-CheckSave(PDFdocument) : Async task returning a new PDFDocument to load or null          |..Depends on..\|+getCol(string header): string[]    |
                                    |                           |-download(in: Semaphore,                                                                  |              /|+AddCol(string header,string[]): void
                                    |                           |in: PDFdocument,                                                                          |               |+Save()                        : void
                                    |                           |in: HttpClient,                                                                           |               
                                 Implements                     |out: List of Check and save tasks) : Async task returning a new PDFDocument to load or null                   /|\ 
                                    |                           |+Download(in ITable urls, inout ITable Metadata, inout IMonitor monitor)                  |                    |
                                    |                           |------------------------------------------------------------------------------------------|                  Implements
                                    |                                                                                                                                           |
                                    |
                      |<<Class: ConsoleMonitor>>               |                                                                                               |<<ExcelTable>>            |
                      |----------------------------------------|                                                                                               |--------------------------|
                      |+RefreshMillis      : int               |                                                                                               |-package : ExcelPackage   |
                      |-shouldstop         : bool              |                                                                                               |-address : filepath       |
                      |-Title              : string            |                                                                                               |-writable: bool           |
                      |-Reports            : List<IReport>     |                                                                                               |-headers : dictionary     |
                      |+setTitle: void                         |
                                     /+\
                                     \+/
                                      |
                                    Contains private class
                                      |
                                     \|/
                      |<<Class CompareErrors>>|
                      |=======================|
                                      :
                               Implements
                                     \:/
                      |<<.NET interface IComparer<(ulong,string)> >>                      |
                      |===================================================================|
                      |+Compare((ulong,string) TimeErrorA,(ulong,string) TimeErrorB ) : int






As you see, the IMonitor and ITable interfaces makes it easy to expand the program, if you should want to load other dataformats, like XML, CSV or anything else. Alternatively, the ConsoleMonitor could easily be replaced by a GUI or web-application. All this could be done without modifying the DownloadManager in any way.
