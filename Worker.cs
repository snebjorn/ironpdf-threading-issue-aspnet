using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IronPdf;

namespace ironpdf_threading_issue_aspnet
{
    public class Worker
    {
        private readonly int _iterations = 10; // increase if no deadlock occurs
        private readonly int _minDelay = 100;
        private readonly int _maxDelay = 1000;

        // this deadlocks
        public async Task DoSimpleWorkAsync()
        {
            Console.WriteLine($"Started simple work...");
            var tasks = Enumerable.Range(1, _iterations).Select(i => HtmlToDocumentAsync("hello", i));
            var pdfs = await Task.WhenAll(tasks);

            Console.WriteLine($"Merging PDFs: Started...");
            using var pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            pdf.SaveAs("output.pdf");
        }

        // this will work but it'll generate the PDFs one at a time 😒
        public async Task DoSequentialWorkAsync()
        {
            Console.WriteLine($"Started sequential work...");
            var pdfs = new List<PdfDocument>();
            foreach (var i in Enumerable.Range(1, _iterations))
            {
                pdfs.Add(await HtmlToDocumentAsync("hello", i));
            }

            Console.WriteLine($"Merging PDFs: Started...");
            using var pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            pdf.SaveAs("output.pdf");
        }

        // this deadlocks
        public async Task DoAdvancedWorkAsync()
        {
            Console.WriteLine($"Started advanced work...");
            var tasks = Enumerable.Range(1, _iterations).Select(i => GetPdfAsync(i));
            var pdfs = await Task.WhenAll(tasks);
            Console.WriteLine($"Merging PDFs: Started...");
            using var pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            pdf.SaveAs("output.pdf");
        }

        public async Task<PdfDocument> GetPdfAsync(int i)
        {
            return (i % 3) switch
            {
                0 => await GetHtmlPdfAsync(i),
                1 => await GetImagePdfAsync(i),
                2 => await GetPdfPdfAsync(i),
                _ => throw new Exception("You fucked up modulus"),
            };
        }

        public async Task<PdfDocument> GetHtmlPdfAsync(int i)
        {
            var html = await GetHtmlAsync(i);
            var pdf = await HtmlToDocumentAsync(html, i);

            return pdf;
        }

        public async Task<PdfDocument> GetImagePdfAsync(int i)
        {
            using var image = await GetImageAsync();
            var pdf = await ImageToDocumentAsync(image, i);

            return pdf;
        }

        public async Task<PdfDocument> GetPdfPdfAsync(int i)
        {
            using var stream = await GetPdfStreamAsync();
            var pdf = PdfToDocument(stream, i);

            return pdf;
        }

        public async Task<string> GetHtmlAsync(int i)
        {
            var rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            return $"<h1>Hello from {i} - {rand}</h1>";
        }

        public async Task<Stream> GetImageAsync()
        {
            var rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            var stream = File.OpenRead("monkeybusiness.jpg");
            return stream;
        }

        public async Task<Stream> GetPdfStreamAsync()
        {
            var rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            var stream = File.OpenRead("testpdf.pdf");
            return stream;
        }

        public async Task<PdfDocument> HtmlToDocumentAsync(string html, int i)
        {
            var headerHtml = await GetHtmlAsync(i);
            using var renderer = new HtmlToPdf(new PdfPrintOptions { Header = new HtmlHeaderFooter { HtmlFragment = headerHtml } });
            var pdf = await renderer.RenderHtmlAsPdfAsync(html);
            Console.WriteLine($"Generated html for: {i}");

            return pdf;
        }

        public async Task<PdfDocument> ImageToDocumentAsync(Stream imageStream, int i)
        {
            var imageDataURL = Util.ImageToDataUri(Image.FromStream(imageStream));
            var html = $@"<h1>{i}</h1><img style=""max-width: 100%; max-height: 60%;"" src=""{imageDataURL}"">";
            var headerHtml = await GetHtmlAsync(i);
            using var renderer = new HtmlToPdf(new PdfPrintOptions { Header = new HtmlHeaderFooter { HtmlFragment = headerHtml } });
            var pdf = await renderer.RenderHtmlAsPdfAsync(html);
            Console.WriteLine($"Generated image for: {i}");

            return pdf;
        }

        public PdfDocument PdfToDocument(Stream pdfStreamReader, int i)
        {
            var pdf = new PdfDocument(pdfStreamReader);
            Console.WriteLine($"Generated pdf for: {i}");

            return pdf;
        }
    }
}
