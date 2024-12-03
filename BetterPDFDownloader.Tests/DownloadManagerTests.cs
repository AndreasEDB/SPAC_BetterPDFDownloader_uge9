using System;
using System.Threading.Tasks;
using Xunit;
using BetterPDFDownloader;
using System.Diagnostics;

namespace BetterPDFDownloader.Tests
{
    public class DownloadManagerTests
    {
        [Fact]
        public void TestDownloadManagerCreation()
        {
            // Arrange
            uint maxThreads = 50;
            uint maxBandwidth = 300;
            int maxDownloads = 100;
            string outputFolder = "Downloads";
            uint timeout = 100;

            // Act
            var downloadManager = new DownloadManager(maxThreads, maxBandwidth, maxDownloads, outputFolder, timeout);

            // Assert
            Assert.NotNull(downloadManager);
        }

        [Fact]
        public async Task TestDownloadHandlesInvalidUrlGracefully()
        {
            // Arrange
            var downloadManager = new DownloadManager(50, 300, 100, "Downloads", 100);
            var urlTable = new MockTable(new[] { "InvalidUrl" });
            var metadataTable = new MockTable(new string[0]);
            var monitor = new MockMonitor();

            // Act
            await downloadManager.Download(urlTable, metadataTable, monitor);

            // Assert
            // Check that the download manager handled the invalid URL without crashing
        }
    }

    // Mock classes for testing
    public class MockTable : ITable
    {
        private string[] data;

        public MockTable(string[] data)
        {
            this.data = data;
        }

        public string[] GetCol(string header) => data;

        public void AddCol(string header, string[] data) { }

        public void Save() { }
    }

    public class MockMonitor : IMonitor
    {
        public Task Display(Stopwatch stopwatch) => Task.CompletedTask;

        public void setReport(IEnumerable<IReport> Reports) { }

        public void DisplaySingle(Stopwatch stopwatch, IEnumerable<IReport> reports) { }

        public void Stop() { }

        public void setTitle(string title) { }

        
    }
}
