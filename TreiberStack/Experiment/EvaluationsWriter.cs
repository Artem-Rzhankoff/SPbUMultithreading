using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using TextAlignment = iText.Layout.Properties.TextAlignment;

namespace Experiment;

public static class EvaluationsWriter
{
    public static void WriteTable(ExperimentData firstStack, ExperimentData secondStack)
    {
        var fileNameSuffix = firstStack.IsRandomPadding ? "Random" : "";
        var fileName = $"performanceResults{fileNameSuffix}";
        var writer = new PdfWriter($"./{fileName}.pdf");
        var document = new Document(new PdfDocument(writer));

        var table = new Table(3, false);

        var cells = new List<Cell>
        {
            new Cell(1, 1)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("# of threads")),
            new Cell(1, 1)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph(firstStack.StackImplementation == ExperimentData.TypeOfStack.TreiberStack 
                    ? "Treiber stack" : "Elimination-backoff stack")),
            new Cell(1, 1)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph(firstStack.StackImplementation == ExperimentData.TypeOfStack.TreiberStack 
                    ? "Treiber stack" : "Elimination-backoff stack")),
        };

        for (var i = 0; i < Environment.ProcessorCount; ++i)
        {
            var treiberResult = firstStack.Data[i].OperationsPerSecond;
            var eliminaitonResult = secondStack.Data[i].OperationsPerSecond;
            
            cells.Add(new Cell(1, 1).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph($"{i+1}")));
            cells.Add(new Cell(1, 1).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph($"{treiberResult}")));
            cells.Add(new Cell(1, 1).SetTextAlignment(TextAlignment.CENTER).Add(new Paragraph($"{eliminaitonResult}")));
        }

        foreach (var cell in cells)
        {
            table.AddCell(cell);
        }

        document.Add(table);
        
        document.Close();

        var pdf = IronPdf.PdfDocument.FromFile(fileName);

        pdf.RasterizeToImageFiles($"{fileName}.png");
        
        File.Delete($"{fileName}.pdf");
    }
}