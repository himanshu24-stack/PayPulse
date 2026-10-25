using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PayPulse.Services;

public class CsvExportService
{
    public async Task ExportToCsvAsync(string filePath, string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();

        // Write Header
        sb.AppendLine(string.Join(",", EscapeRow(headers)));

        // Write Rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", EscapeRow(row)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private IEnumerable<string> EscapeRow(string[] items)
    {
        foreach (var item in items)
        {
            string val = item ?? string.Empty;
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                val = $"\"{val.Replace("\"", "\"\"")}\"";
            }
            yield return val;
        }
    }
}
