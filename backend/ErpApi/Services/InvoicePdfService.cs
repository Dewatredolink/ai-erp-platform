using System.Globalization;
using System.Text;
using ErpApi.Models;

namespace ErpApi.Services
{
    /// <summary>
    /// Generates simple, dependency-free PDF documents for invoices.
    /// Produces a minimal valid single-page PDF (no external library required).
    /// </summary>
    public class InvoicePdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice, Company? company)
        {
            var lines = BuildContentLines(invoice, company);
            return BuildPdf(lines);
        }

        private static List<string> BuildContentLines(Invoice invoice, Company? company)
        {
            var culture = CultureInfo.InvariantCulture;
            var lines = new List<string>
            {
                $"Invoice {invoice.InvoiceNumber}",
                $"Company: {company?.CompanyName ?? "N/A"}",
                $"Invoice Date: {invoice.InvoiceDate.ToString("yyyy-MM-dd", culture)}",
                $"Due Date: {invoice.DueDate.ToString("yyyy-MM-dd", culture)}",
                $"Status: {invoice.Status}",
                string.Empty,
                "Description | Qty | Unit Price | Tax | Amount",
            };

            foreach (var item in invoice.Items)
            {
                lines.Add(
                    $"{item.Description} | {item.Quantity.ToString(culture)} | " +
                    $"{item.UnitPrice.ToString("F2", culture)} | {item.TaxAmount.ToString("F2", culture)} | " +
                    $"{item.Amount.ToString("F2", culture)}");
            }

            lines.Add(string.Empty);
            lines.Add($"Subtotal: {invoice.TotalAmount.ToString("F2", culture)}");
            lines.Add($"Tax: {invoice.TaxAmount.ToString("F2", culture)}");
            lines.Add($"Grand Total: {invoice.GrandTotal.ToString("F2", culture)}");

            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                lines.Add(string.Empty);
                lines.Add($"Notes: {invoice.Notes}");
            }

            return lines;
        }

        private static byte[] BuildPdf(List<string> textLines)
        {
            var contentBuilder = new StringBuilder();
            contentBuilder.Append("BT\n/F1 11 Tf\n50 780 Td\n14 TL\n");

            foreach (var line in textLines)
            {
                contentBuilder.Append('(').Append(EscapePdfText(line)).Append(") Tj\n");
                contentBuilder.Append("T*\n");
            }

            contentBuilder.Append("ET");

            var contentBytes = Encoding.ASCII.GetBytes(contentBuilder.ToString());

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> " +
                    "/MediaBox [0 0 612 792] /Contents 5 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            };

            using var stream = new MemoryStream();
            void Write(string text) => stream.Write(Encoding.ASCII.GetBytes(text));

            Write("%PDF-1.4\n");

            var offsets = new List<long> { 0 };

            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(stream.Position);
                Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            offsets.Add(stream.Position);
            Write($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            stream.Write(contentBytes);
            Write("\nendstream\nendobj\n");

            var xrefOffset = stream.Position;
            var objectCount = objects.Count + 2; // +1 for content stream, +1 for the free object
            Write($"xref\n0 {objectCount}\n");
            Write("0000000000 65535 f \n");
            for (var i = 1; i < objects.Count + 1; i++)
            {
                Write($"{offsets[i]:D10} 00000 n \n");
            }
            Write($"{offsets[objects.Count + 1]:D10} 00000 n \n");

            Write($"trailer\n<< /Size {objectCount} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

            return stream.ToArray();
        }

        private static string EscapePdfText(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
        }
    }
}
