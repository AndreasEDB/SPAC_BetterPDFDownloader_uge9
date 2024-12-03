using System;

namespace BetterPDFDownloader.Tests;

public class ConsoleMonitorTests
{
    [Fact]
    public void TestConsoleMonitorCreation()
    {

        // Act
        var consoleMonitor = new ConsoleMonitor(1000);

        // Assert
        Assert.NotNull(consoleMonitor);
    }
}
