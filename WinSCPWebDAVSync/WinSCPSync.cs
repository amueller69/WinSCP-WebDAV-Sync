using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using WinSCP;


namespace WinSCPSync
{
    class WinSCPSync
    {
        public WinSCPSyncOptions Options { get; set; }

        public WinSCPSync(IDictionary<string, string> options)
        {
            try
            {
                string temp;
                byte[] bytes = Encoding.UTF8.GetBytes(options["Password"]);
                ProtectedMemory.Protect(bytes, MemoryProtectionScope.SameProcess);
                Options = new WinSCPSyncOptions
                {
                    Username = options["Username"],
                    Password = bytes,
                    Hostname = options["Hostname"],
                    LocalDirectory = options["LocalDirectory"],
                    RemoteDirectory = (options.TryGetValue("RemoteDirectory", out temp) == true) ? temp : null
                };

                if (options.TryGetValue("ArchiveFiles", out temp))
                {
                    Options.ArchiveFiles = (temp.ToLower() == "true") ? true : false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Sync()
        {
            using (Session session = new Session())
            {
                SessionOptions options = new SessionOptions
                {
                    UserName = Options.Username,
                    Password = Encoding.UTF8.GetString(Options.Password),
                    HostName = Options.Hostname
                };

                session.Open(options);
                SynchronizationResult result = session.SynchronizeDirectories(SynchronizationMode.Remote,
                    Options.LocalDirectory, Options.RemoteDirectory, false);
            }
        }
    }
}
