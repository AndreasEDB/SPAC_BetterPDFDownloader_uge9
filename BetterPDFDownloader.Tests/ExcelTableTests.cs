using System;
using Xunit;
using BetterPDFDownloader;

namespace BetterPDFDownloader.Tests
{
    public class ExcelTableTests
    {
        private readonly string filename = "test.xlsx";
        private readonly bool writable = true;
        private readonly bool overwrite = true;
        [Fact]
        public void TestExcelTableCreation()
        {
            // Arrange


            // Act
            var excelTable = new ExcelTable(filename, writable, overwrite);

            // Assert
            Assert.NotNull(excelTable);
        }

        [Fact]
        public void TestGetColReturnsEmptyArrayForNonExistentHeader()
        {
            var excelTable = new ExcelTable(filename, writable, overwrite);

            // Act
            var result = excelTable.GetCol("NonExistentHeader");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void TestAddColAddsNewColumn()
        {
            // Arrange
            var excelTable = new ExcelTable(filename, writable, overwrite);
            string[] data = { "Data1", "Data2" };

            // Act
            excelTable.AddCol("NewHeader", data);

            // Assert
            var result = excelTable.GetCol("NewHeader");
            Assert.Equal(data, result);
        }

    
    }
}
