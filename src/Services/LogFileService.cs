using Microsoft.Win32;
using System.IO;

namespace CreanexDataVis.Services;

internal interface ILogFileService
{
    string? Folder { get; }

    string? Participant { get; }
    string? Condition { get; }

    string? CreanexLogFile { get; }
    string? VarjoLogFile { get; }
    string? VideoFile { get; }

    string[] FolderCreanexFiles { get; }

    event EventHandler<string?> CreanexLogFileSelected;
    event EventHandler<string?> VarjoLogFileSelected;
    event EventHandler<string?> VideoFileSelected;

    /// <summary>
    /// Asks to choose a folder with participant's study (3 Creanex log files) or pilot (1 Creanex log file) data files
    /// </summary>
    bool ChooseParticipantFolder();

    void SetCreanexLogFile(string? filename);
}

internal class LogFileService : ILogFileService
{
    public string? Folder { get; private set; }

    public string? Participant { get; private set; }
    public string? Condition { get; private set; }

    public string? CreanexLogFile { get; set; }
    public string? VarjoLogFile { get; private set; }
    public string? VideoFile { get; private set; }

    public string[] FolderCreanexFiles { get; private set; } = [];

    public event EventHandler<string?>? CreanexLogFileSelected;
    public event EventHandler<string?>? VarjoLogFileSelected;
    public event EventHandler<string?>? VideoFileSelected;

    public bool ChooseParticipantFolder()
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
                LoadStudyData(folder, creanexLogFiles);
            }
            else                                // pilot log data
            {
                LoadPilotData(folder);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Use this method to change the currently selected Creanex log file when a study folder was selected
    /// (e.g. when user selects another log file from the dropdown list)
    /// </summary>
    /// <param name="filename">the file name without the folder path</param>
    public void SetCreanexLogFile(string? filename)
    {
        if (Folder == null || filename == null)
        {
            Condition = null;
            CreanexLogFile = null;
            VarjoLogFile = null;
            VideoFile = null;
        }
        else
        {
            int index = Array.IndexOf(FolderCreanexFiles, filename);
            if (index >= 0)
            {
                var p = Folder.Split(Path.DirectorySeparatorChar);
                if (int.TryParse(p[^1], out int id))
                {
                    var conditions = Conditions[(id - 1) % 3];
                    Condition = conditions[index].ToString();
                }
            }

            CreanexLogFile = Path.Combine(Folder, filename);
            VarjoLogFile = GetVarjoLogFile(Folder, CreanexLogFile);
        }

        CreanexLogFileSelected?.Invoke(this, CreanexLogFile == null || Condition == null ? null : CreanexLogFile);
        VarjoLogFileSelected?.Invoke(this, VarjoLogFile == null || Condition == null ? null : VarjoLogFile);
        VideoFileSelected?.Invoke(this, null);
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

    private void LoadStudyData(string folder, string[] filenames)
    {
        Folder = folder;
        FolderCreanexFiles = filenames
            .Select(fn => Path.GetFileName(fn))
            .ToArray();

        Participant = folder.Split(Path.DirectorySeparatorChar)[^1];

        SetCreanexLogFile(FolderCreanexFiles.Length > 0 ? FolderCreanexFiles[0] : null);
    }

    private void LoadPilotData(string folder)
    {
        FolderCreanexFiles = [];

        var creanexLogFiles = Directory.GetFiles(folder, CreanexLogFilePattern);
        CreanexLogFile = creanexLogFiles.Length > 0 ? creanexLogFiles[0] : null;
        CreanexLogFileSelected?.Invoke(this, CreanexLogFile == null ? null : CreanexLogFile);

        var varjoLogFiles = Directory.GetFiles(folder, VarjoLogFilePattern);
        VarjoLogFile = varjoLogFiles.Length > 0 ? varjoLogFiles[0] : null;
        VarjoLogFileSelected?.Invoke(this, VarjoLogFile == null ? null : VarjoLogFile);

        var videoFiles = Directory.GetFiles(folder, VideoFilePattern);
        VideoFile = videoFiles.Length > 0 ? videoFiles[0] : null;
        VideoFileSelected?.Invoke(this, VideoFile);
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
