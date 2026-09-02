using System.Text;

namespace PingFlud.Application;

public sealed class TargetListImporter
{
    public const long MaximumFileBytes = 5L * 1024 * 1024;

    public string Import(string path) =>
        ImportAsync(path).GetAwaiter().GetResult();

    public async Task<string> ImportAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("The target list was not found.", path);
        if (file.Length > MaximumFileBytes)
            throw new InvalidDataException("Target lists must be 5 MB or smaller.");

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        using var content = new MemoryStream((int)Math.Min(stream.Length, MaximumFileBytes));
        var buffer = new byte[64 * 1024];
        long bytesRead = 0;

        while (true)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (count == 0) break;

            bytesRead += count;
            if (bytesRead > MaximumFileBytes)
                throw new InvalidDataException("Target lists must be 5 MB or smaller.");

            await content.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
        }

        content.Position = 0;
        using var reader = new StreamReader(
            content,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 64 * 1024);
        var output = new StringBuilder();
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                if (output.Length > 0)
                    output.Append(Environment.NewLine);
                output.Append(trimmed);
            }
        }

        return output.ToString();
    }
}
