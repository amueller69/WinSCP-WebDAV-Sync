namespace WinSCPSync
{
    class SynchronizerOptions
    {
        public string Username { get; set; }
        public byte[] Password { get; set; }
        public string Hostname { get; set; }
        public bool ArchiveFiles { get; set; } = false;
        public string LocalDirectory { get; set; }
        public string RemoteDirectory { get; set; }
    }
}
