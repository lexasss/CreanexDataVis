using System.IO;

namespace CreanexDataVis.Services;

internal interface ILogFileService
{
    string Folder { get; set; }

    string? SelectedCreanexFile { get; set; }

    /// <summary>
    /// In key-value pairs, key corresponds to study condition (ABC), and value corresponds to a filename
    /// </summary>
    /// <returns>Array of "study condition" - "filename" pairs</returns>
    KeyValuePair<string, string>[] GetCreanexFiles();

    string? GetVarjoLogFile(string? creanexLogFile);
}

internal class LogFileService : ILogFileService
{
    public string Folder
    {
        get => field;
        set
        {
            field = value;
            _filenames = Directory.GetFiles(field, "MixerEventLog*.csv");
        }
    } = "";

    public string? SelectedCreanexFile { get; set; } = null;

    public KeyValuePair<string, string>[] GetCreanexFiles()
    {
        if (string.IsNullOrEmpty(Folder) || _filenames.Length < 3)
            return [];

        var p = Folder.Split(Path.DirectorySeparatorChar);
        if (int.TryParse(p[^1], out int id))
        {
            var conditions = Conditions[(id - 1) % 3];
            return conditions
                .Select((c, i) => new KeyValuePair<string, string>(c.ToString(), Path.GetFileName(_filenames[i])))
                .ToArray();
        }

        return _filenames
            .Select(fn => new KeyValuePair<string, string>("", Path.GetFileName(fn)))
            .ToArray();
    }

    public string? GetVarjoLogFile(string? creanexLogFile)
    {
        if (creanexLogFile == null)
            return null;

        creanexLogFile = Path.GetFileNameWithoutExtension(creanexLogFile);
        var p = creanexLogFile.Split('_');
        if (p.Length != 3)
            return null;

        try
        {
            var dateComps = p[1].Split('-').Select(v => int.Parse(v)).ToArray();
            var hour = int.Parse(p[2][..2]);
            var min = int.Parse(p[2][^2..]);

            var datetime = new DateTime(dateComps[0], dateComps[1], dateComps[2], hour, min, 0, DateTimeKind.Local);
            datetime = datetime.ToUniversalTime();

            var timestamp = datetime.ToString("yyyy_MM_dd_HH_mm");
            var files = Directory.GetFiles(Folder, $"VarjoEyeTracking_{timestamp}_*.csv");
            if (files.Length > 0)
                return files[0];
        }
        catch { }

        return null;
    }

    #region Internal

    readonly static string[] Conditions = [
        "ABC",
        "CAB",
        "BCA",
    ];

    string[] _filenames = [];

    #endregion
}
