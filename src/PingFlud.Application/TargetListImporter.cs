namespace PingFlud.Application;

public sealed class TargetListImporter
{
    public const long MaximumFileBytes = 5L * 1024 * 1024;

    public string Import(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var file = new FileInfo(path);
        if (!file.Exists) throw new FileNotFoundException("The target list was not found.", path);
        if (file.Length > MaximumFileBytes)
            throw new InvalidDataException("Target lists must be 5 MB or smaller.");

        return string.Join(Environment.NewLine,
            File.ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#')));
    }
}
