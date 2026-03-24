namespace WinSCPSync
{
    class SynchronizerOptions
    {
        public required string Username { get; set; }
        public required byte[] Password { get; set; }
        public required string Hostname { get; set; }
        public bool ArchiveFiles { get; set; } = false;
        public required string LocalDirectory { get; set; }
        public required string RemoteDirectory { get; set; }
    }
}
