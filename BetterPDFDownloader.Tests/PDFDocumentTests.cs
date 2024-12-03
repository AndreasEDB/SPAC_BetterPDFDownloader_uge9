using System;
using Xunit;
using BetterPDFDownloader;

namespace BetterPDFDownloader.Tests
{
    public class PDFDocumentTests
    {
        [Fact]
        public void TestPDFDocumentCreation()
        {
            // Arrange
            string name = "TestDocument";
            string url = "http://example.com/test.pdf";
            string? fallback = "http://example.com/fallback.pdf";

            // Act
            var pdfDocument = new PDFDocument(name, url, fallback);

            // Assert
            Assert.NotNull(pdfDocument);
            Assert.Equal(name, pdfDocument.Name);
            Assert.Equal(url, pdfDocument.Url);
            Assert.Equal(fallback, pdfDocument.Fallback_url);
        }

        [Fact]
        public void TestGetFallbackReturnsNullIfNoFallback()
        {
            // Arrange
            var pdfDocument = new PDFDocument("TestDocument", "http://example.com/test.pdf", null);

            // Act
            var fallback = pdfDocument.getFallback();

            // Assert
            Assert.Null(fallback);
        }

        [Fact]
        public void TestGetFallbackReturnsNewPDFDocumentIfFallbackExists()
        {
            // Arrange
            var pdfDocument = new PDFDocument("TestDocument", "http://example.com/test.pdf", "http://example.com/fallback.pdf");

            // Act
            var fallback = pdfDocument.getFallback();

            // Assert
            Assert.NotNull(fallback);
            Assert.Equal("http://example.com/fallback.pdf", fallback.Url);
        }

        [Fact]
        public void TestCompareToReturnsZeroForEqualDocuments()
        {
            // Arrange
            var pdfDocument1 = new PDFDocument("TestDocument", "http://example.com/test.pdf", null);
            var pdfDocument2 = new PDFDocument("TestDocument", "http://example.com/test.pdf", null);

            // Act
            var result = pdfDocument1.CompareTo(pdfDocument2);

            // Assert
            Assert.Equal(0, result);
        }
    }
}
