using System.Globalization;
using System.Text;
using PingFlud.Core;

namespace PingFlud.Application;

internal static partial class PngReport
{
    private const int RowsPerImage = 100;
    private const int Width = 1600;
    private const int RowHeight = 24;
    private const int HeaderHeight = 34;

    public static void Write(string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken) =>
        WriteImpl(path, rows, cancellationToken);
}
