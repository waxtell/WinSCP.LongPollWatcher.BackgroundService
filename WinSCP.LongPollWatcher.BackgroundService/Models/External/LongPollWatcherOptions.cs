namespace WinSCP.LongPollWatcher.BackgroundService.Models.External
{
    public class LongPollWatcherOptions
    {
        public int SleepIntervalMilliseconds { get; set; } = 10000;
        public string RemotePath { get; set; } = "/";
        public string Mask { get; set; } = "*.*";
        public bool IncludeSubdirectories { get; set; } = true;
        public string SessionExecutablePath { get; set; }
        public bool TreatExistingFilesAsNew { get; set; } = false;
    }
}
