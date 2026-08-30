#if !NET8_0_OR_GREATER || !WINDOWS

using PingFlud.Core;

namespace PingFlud.Application;

internal static partial class PngReport
{
    private static void WriteImpl(string path, IReadOnlyList<ScanResult> rows, CancellationToken cancellationToken) =>
        throw new PlatformNotSupportedException("PNG export requires Windows.");
}

#endif
