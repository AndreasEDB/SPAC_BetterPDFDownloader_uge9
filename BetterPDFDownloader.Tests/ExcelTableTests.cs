using System;
using Xunit;
using BetterPDFDownloader;

namespace BetterPDFDownloader.Tests
{
    public class ExcelTableTests
    {
        [Fact]
        public void TestExcelTableCreation()
        {
            // Arrange
            string filename = "test.xlsx";
            bool writable = false;

            // Act
            var excelTable = new ExcelTable(filename, writable);

            // Assert
            Assert.NotNull(excelTable);
        }

        [Fact]
        public void TestGetColReturnsEmptyArrayForNonExistentHeader()
        {
            // Arrange
            string filename = "test.xlsx";
            var excelTable = new ExcelTable(filename);

            // Act
            var result = excelTable.GetCol("NonExistentHeader");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void TestAddColAddsNewColumn()
        {
            // Arrange
            string filename = "test.xlsx";
            var excelTable = new ExcelTable(filename, true);
            string[] data = { "Data1", "Data2" };

            // Act
            excelTable.AddCol("NewHeader", data);

            // Assert
            var result = excelTable.GetCol("NewHeader");
            Assert.Equal(data, result);
        }

        [Fact]
        public void TestSaveThrowsExceptionIfNotWritable()
        {
            // Arrange
            string filename = "test.xlsx";
            var excelTable = new ExcelTable(filename);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => excelTable.Save());
        }
    }
}
