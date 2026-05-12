using Microsoft.Win32;
using System.IO;

namespace CreanexDataVis.Services;

internal interface ILogFileService
{
    string? Folder { get; }

    string? CreanexLogFile { get; }
    string? VarjoLogFile { get; }
    string? VideoFile { get; }

    event EventHandler<string?> CreanexLogFileSelected;
    event EventHandler<string?> VarjoLogFileSelected;
    event EventHandler<string?> VideoFileSelected;

    /// <summary>
    /// Asks to choose a folder with participant's study (3 Creanex log files) or pilot (1 Creanex log file) data files
    /// </summary>
    void ChooseParticipantFolder();
}

internal class LogFileService : ILogFileService
{
    public string? Folder { get; private set; }

    public string? CreanexLogFile { get; set; }
    public string? VarjoLogFile { get; private set; }
    public string? VideoFile { get; private set; }

    public event EventHandler<string?>? CreanexLogFileSelected;
    public event EventHandler<string?>? VarjoLogFileSelected;
    public event EventHandler<string?>? VideoFileSelected;

    public void ChooseParticipantFolder()
    {
        var ofd = new OpenFolderDialog()
        {
            Title = "Select a folder with Creanex and Varjo log files, and a video recording",
        };

        if (ofd.ShowDialog() == true)
        {
            var folder = ofd.FolderName;

            var creanexLogFiles = Directory.GetFiles(folder, CreanexLogFilePattern);
            if (creanexLogFiles.Length == 3)    // study log file
            {
                LoadStudyData(folder);
            }
            else                                // pilot log data
            {
                LoadPilotData(folder);
            }
        }
    }

    #region Internal

    readonly static string CreanexLogFilePattern = "MixerEventLog_*.csv";
    readonly static string VarjoLogFilePattern = "VarjoEyeTracking_*.csv";
    readonly static string VideoFilePattern = "*.mp4";

    readonly static string[] Conditions = [
        "ABC",
        "CAB",
        "BCA",
    ];

    private void LoadStudyData(string folder)
    {
        var filenames = Directory.GetFiles(folder, CreanexLogFilePattern);
        var dialog = new Views.SelectCreanexLogFile(GetConditionsAndCreanexFiles(folder, filenames));
        if (dialog.ShowDialog() == true)
        {
            Folder = folder;

            CreanexLogFile = Path.Combine(folder, dialog.SelectedFilename);
            CreanexLogFileSelected?.Invoke(this, CreanexLogFile);

            VarjoLogFile = GetVarjoLogFile(folder, CreanexLogFile);
            VarjoLogFileSelected?.Invoke(this, VarjoLogFile);

            VideoFileSelected?.Invoke(this, null);
        }
    }

    private void LoadPilotData(string folder)
    {
        var creanexLogFiles = Directory.GetFiles(folder, CreanexLogFilePattern);
        CreanexLogFile = creanexLogFiles.Length > 0 ? creanexLogFiles[0] : null;
        CreanexLogFileSelected?.Invoke(this, CreanexLogFile);

        var varjoLogFiles = Directory.GetFiles(folder, VarjoLogFilePattern);
        VarjoLogFile = varjoLogFiles.Length > 0 ? varjoLogFiles[0] : null;
        VarjoLogFileSelected?.Invoke(this, VarjoLogFile);

        var videoFiles = Directory.GetFiles(folder, VideoFilePattern);
        VideoFile = videoFiles.Length > 0 ? videoFiles[0] : null;
        VideoFileSelected?.Invoke(this, VideoFile);
    }

    private static KeyValuePair<string, string>[] GetConditionsAndCreanexFiles(string folder, string[] filenames)
    {
        if (string.IsNullOrEmpty(folder) || filenames.Length < 3)
            return [];

        var p = folder.Split(Path.DirectorySeparatorChar);
        if (int.TryParse(p[^1], out int id))
        {
            var conditions = Conditions[(id - 1) % 3];
            return conditions
                .Select((c, i) => new KeyValuePair<string, string>(c.ToString(), Path.GetFileName(filenames[i])))
                .ToArray();
        }

        return filenames
            .Select(fn => new KeyValuePair<string, string>("", Path.GetFileName(fn)))
            .ToArray();
    }

    private static string? GetVarjoLogFile(string folder, string? creanexLogFile)
    {
        if (string.IsNullOrEmpty(folder) || creanexLogFile == null)
            return null;

        creanexLogFile = Path.GetFileNameWithoutExtension(creanexLogFile);
        var p = creanexLogFile.Split('_');
        if (p.Length != 3)
            return null;

        try
        {
            var dateComps = p[1].Split('-').Select(int.Parse).ToArray();
            var hour = int.Parse(p[2][..2]);
            var min = int.Parse(p[2][^2..]);

            var datetime = new DateTime(dateComps[0], dateComps[1], dateComps[2], hour, min, 0, DateTimeKind.Local);
            datetime = datetime.ToUniversalTime();

            var timestamp = datetime.ToString("yyyy_MM_dd_HH_mm");
            var patternComp = VarjoLogFilePattern.Split('*');
            var files = Directory.GetFiles(folder, $"{patternComp[0]}{timestamp}*{patternComp[1]}");
            if (files.Length > 0)
                return files[0];
        }
        catch { }

        return null;
    }

    #endregion
}
