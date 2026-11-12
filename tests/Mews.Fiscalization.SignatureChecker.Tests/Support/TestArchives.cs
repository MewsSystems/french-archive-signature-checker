using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using File = Mews.Fiscalization.SignatureChecker.Dto.File;

namespace Mews.Fiscalization.SignatureChecker.Tests.Support;

/// <summary>
/// Builds archive contents in the layout Mews produces, and signs them with a throwaway key.
/// The expected signature payloads are written out by hand in the tests, so they are independent of the code under test.
/// </summary>
internal static class TestArchives
{
    public const string Terminal = "TERMINAL-1";

    // RSA instances are not thread-safe and tests run in parallel, so each use gets its own instance of the same key.
    private static readonly Lazy<RSAParameters> TestKeyParameters = new(() =>
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportParameters(includePrivateParameters: true);
    });

    public static RSA CreateTestKey() => RSA.Create(TestKeyParameters.Value);

    public static string Metadata(string version, string archiveType = "DAY", string created = "2024-01-31T23:59:59", string previousRecordSignature = null)
    {
        var previous = previousRecordSignature is null ? "null" : $"\"{previousRecordSignature}\"";
        return $$"""{"TerminalIdentification":"{{Terminal}}","PreviousRecordSignature":{{previous}},"Created":"{{created}}","Version":"{{version}}","ArchiveType":"{{archiveType}}"}""";
    }

    /// <summary>
    /// An INVOICE_FOOTER csv with a header line. Column 1 is the net breakdown, column 2 the tax breakdown, column 18 the total.
    /// </summary>
    public static string InvoiceFooter(params (string Net, string Tax, string Total)[] rows)
    {
        var header = string.Join(";", Enumerable.Range(0, 19).Select(i => $"H{i}"));
        var lines = rows.Select(r =>
        {
            var values = Enumerable.Range(0, 19).Select(i => $"v{i}").ToArray();
            values[1] = r.Net;
            values[2] = r.Tax;
            values[18] = r.Total;
            return string.Join(";", values);
        });
        return string.Join("\n", lines.Prepend(header)) + "\n";
    }

    /// <summary>
    /// A version 1 TAX_TOTALS csv. Column 4 is the tax rate (as a fraction), column 10 the tax amount.
    /// </summary>
    public static string TaxTotals(params (string Rate, string Amount)[] rows)
    {
        var header = string.Join(";", Enumerable.Range(0, 11).Select(i => $"H{i}"));
        var lines = rows.Select(r =>
        {
            var values = Enumerable.Range(0, 11).Select(i => $"v{i}").ToArray();
            values[4] = r.Rate;
            values[10] = r.Amount;
            return string.Join(";", values);
        });
        return string.Join("\n", lines.Prepend(header)) + "\n";
    }

    /// <summary>
    /// A version 1 TOTALS csv. Column 3 is the total.
    /// </summary>
    public static string Totals(params string[] totals)
    {
        var lines = totals.Select(t => $"a;b;c;{t}");
        return string.Join("\n", lines.Prepend("H0;H1;H2;H3")) + "\n";
    }

    public static List<File> V4Files(string version, string archiveType = "DAY", string previousRecordSignature = null)
    {
        return
        [
            new File("INVOICE_HEADER-2024-01-31.csv", "FAC-NID;FAC-NUM\nid;1\n"),
            new File("INVOICE_FOOTER-2024-01-31.csv", InvoiceFooter(
                ("20,00 %:100,00 €|10,00 %:50,00 €", "20,00 %:20,00 €|10,00 %:5,00 €", "175,00 €"),
                ("5,50 %:10,00 €", "5,50 %:0,55 €", "10,55 €")
            )),
            new File("METADATA.json", Metadata(version, archiveType, previousRecordSignature: previousRecordSignature)),
        ];
    }

    public static List<File> V1Files(string previousRecordSignature = null)
    {
        return
        [
            new File("TAX_TOTALS-2015.csv", TaxTotals(("0,2", "20,00"), ("0,2", "5,00"), ("0,055", "1,10"))),
            new File("TOTALS-2015.csv", Totals("100,00", "26,10")),
            new File("METADATA.json", Metadata("1.0", archiveType: "IGNORED", previousRecordSignature: previousRecordSignature)),
        ];
    }

    public static string Sign(string payload, HashAlgorithmName hashAlgorithm, RSA key = null)
    {
        using var testKey = CreateTestKey();
        var signature = (key ?? testKey).SignData(Encoding.UTF8.GetBytes(payload), hashAlgorithm, RSASignaturePadding.Pkcs1);
        return ToBase64Url(signature);
    }

    public static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static List<File> WithSignature(this List<File> files, string signature)
    {
        return [.. files, new File("SIGNATURE.txt", signature)];
    }

    public static string WriteZip(IEnumerable<File> files, string directory = null)
    {
        var path = System.IO.Path.Combine(directory ?? System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
        using var stream = System.IO.File.Create(path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            var entry = zip.CreateEntry(file.Name);
            using var entryStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(file.Content);
            entryStream.Write(bytes, 0, bytes.Length);
        }
        return path;
    }

    /// <summary>
    /// A ZIP file whose directory is intact (so it opens) but whose only entry fails to decompress.
    /// </summary>
    public static string WriteCorruptZip(string directory)
    {
        var path = WriteZip([new File("METADATA.json", new string('x', 4096))], directory);
        var bytes = System.IO.File.ReadAllBytes(path);

        // Local file header: the file name length is at offset 26 and the extra field length at 28; the data follows the 30-byte header.
        var dataStart = 30 + BitConverter.ToUInt16(bytes, 26) + BitConverter.ToUInt16(bytes, 28);

        // A deflate block header with the reserved block type 11 is always invalid.
        bytes[dataStart] = 0xFF;
        System.IO.File.WriteAllBytes(path, bytes);
        return path;
    }
}
