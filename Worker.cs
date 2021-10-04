using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPdf;
using IronPdf.Imaging;
using IronPdf.Rendering;

namespace ironpdf_threading_issue_aspnet
{
    public class Worker
    {
        private readonly int _iterations = 10; // increase if no deadlock occurs
        private readonly int _minDelay = 100;
        private readonly int _maxDelay = 1000;

        public Worker()
        {
            IronPdf.Logging.Logger.EnableDebugging = true;
            IronPdf.Logging.Logger.LogFilePath = "Default.log"; //May be set to a direction name or full file name
            IronPdf.Logging.Logger.LoggingMode = IronPdf.Logging.Logger.LoggingModes.All;
        }

        // this deadlocks
        public async Task DoSimpleWorkAsync()
        {
            Console.WriteLine($"Started simple work...");
            IEnumerable<Task<PdfDocument>> tasks = Enumerable.Range(1, _iterations).Select(i => HtmlToDocumentAsync("hello", i));
            PdfDocument[] pdfs = await Task.WhenAll(tasks);

            Console.WriteLine($"Merging PDFs: Started...");
            using PdfDocument pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            _ = pdf.SaveAs("output.pdf");
        }

        // this will work but it'll generate the PDFs one at a time 😒
        public async Task DoSequentialWorkAsync()
        {
            Console.WriteLine($"Started sequential work...");
            List<PdfDocument> pdfs = new();
            foreach (int i in Enumerable.Range(1, _iterations))
            {
                pdfs.Add(await HtmlToDocumentAsync("hello", i));
            }

            Console.WriteLine($"Merging PDFs: Started...");
            using PdfDocument pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            _ = pdf.SaveAs("output.pdf");
        }

        // this deadlocks
        public async Task DoAdvancedWorkAsync()
        {
            Console.WriteLine($"Started advanced work...");
            IEnumerable<Task<PdfDocument>> tasks = Enumerable.Range(1, _iterations).Select(i => GetPdfAsync(i));
            PdfDocument[] pdfs = await Task.WhenAll(tasks);
            Console.WriteLine($"Merging PDFs: Started...");
            using PdfDocument pdf = PdfDocument.Merge(pdfs);
            Console.WriteLine($"Merging PDFs: Done!");
            _ = pdf.SaveAs("output.pdf");
        }

        public async Task DoTableBreakWorkAsync()
        {
            StringBuilder tableRowsBuilder = new();
            for (int i = 0; i < 100; i++)
            {
                _ = tableRowsBuilder.Append(@"<tr><td>TEST</td></tr>");
            }
            string html = @$"
                <table>
                    <thead>
                        <tr>
                            <th>HEADER</th>
                        </tr>
                    </thead>
                    <tbody>
                        {tableRowsBuilder}
                    </thead>
                </table>
            ";

            ChromePdfRenderer renderer = new();
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(html);

            Console.WriteLine($"Generated table break pdf");
            _ = pdf.SaveAs("output.pdf");
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
            string html = await GetHtmlAsync(i);
            PdfDocument pdf = await HtmlToDocumentAsync(html, i);

            return pdf;
        }

        public async Task<PdfDocument> GetImagePdfAsync(int i)
        {
            using Stream image = await GetImageAsync();
            PdfDocument pdf = await ImageToDocumentAsync(image, i);

            return pdf;
        }

        public async Task<PdfDocument> GetPdfPdfAsync(int i)
        {
            Stream stream = await GetPdfStreamAsync();
            PdfDocument pdf = PdfToDocument(stream, i);

            HtmlHeaderFooter htmlHeader = new()
            {
                HtmlFragment = "Test header",
            };
            PdfDocument pdfWithHeader = pdf.AddHTMLHeaders(htmlHeader, 50, 50, 50);

            return pdfWithHeader;
        }

        public async Task<string> GetHtmlAsync(int i)
        {
            int rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            return $"<h1>Hello from {i} - {rand}</h1>";
        }

        public async Task<Stream> GetImageAsync()
        {
            int rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            FileStream stream = File.OpenRead("monkeybusiness.jpg");
            return stream;
        }

        public async Task<Stream> GetPdfStreamAsync()
        {
            int rand = new Random().Next(_minDelay, _maxDelay);
            await Task.Delay(rand); // simulate download time

            FileStream stream = File.OpenRead("testpdf.pdf");
            return stream;
        }

        public async Task<PdfDocument> HtmlToDocumentAsync(string html, int i)
        {
            string headerHtml = await GetHtmlAsync(i);
            ChromePdfRenderer renderer = new();
            renderer.RenderingOptions.HtmlHeader = new HtmlHeaderFooter { HtmlFragment = headerHtml };
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(html);
            Console.WriteLine($"Generated html for: {i}");

            return pdf;
        }

        public async Task<PdfDocument> ImageToDocumentAsync(Stream imageStream, int i)
        {
            string imageDataURL = ImageUtilities.ImageToDataUri(Image.FromStream(imageStream));
            string html = $@"<h1>{i}</h1><img style=""max-width: 100%; max-height: 60%;"" src=""{imageDataURL}"">";
            string headerHtml = await GetHtmlAsync(i);
            ChromePdfRenderer renderer = new();
            renderer.RenderingOptions.HtmlHeader = new HtmlHeaderFooter { HtmlFragment = headerHtml };
            PdfDocument pdf = await renderer.RenderHtmlAsPdfAsync(html);
            Console.WriteLine($"Generated image for: {i}");

            return pdf;
        }

        public PdfDocument PdfToDocument(Stream pdfStreamReader, int i)
        {
            PdfDocument pdf = new(pdfStreamReader);
            Console.WriteLine($"Generated pdf for: {i}");

            return pdf;
        }
    }
}
