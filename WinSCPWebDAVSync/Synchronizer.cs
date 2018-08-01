using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using WinSCP;
using System.Threading.Tasks;

namespace WinSCPSync
{
    interface ISynchronizer
    {
        void Sync();
    }

    class Synchronizer : ISynchronizer
    {
        public SynchronizerOptions Options { get; set; }

        public Synchronizer(IDictionary<string, string> options)
        {
            try
            {
                string temp;
                byte[] bytes = Encoding.UTF8.GetBytes(options["Password"]);
                ProtectedMemory.Protect(bytes, MemoryProtectionScope.SameProcess);
                Options = new SynchronizerOptions
                {
                    Username = options["Username"],
                    Password = bytes,
                    Hostname = options["Hostname"],
                    LocalDirectory = options["LocalDirectory"],
                    RemoteDirectory = (options.TryGetValue("RemoteDirectory", out temp) == true) ? temp : "/"
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
                byte[] pw = new byte[Options.Password.Length];
                Options.Password.CopyTo(pw, 0);
                ProtectedMemory.Unprotect(pw, MemoryProtectionScope.SameProcess);
                SessionOptions options = new SessionOptions
                {
                    UserName = Options.Username,
                    Password = Encoding.UTF8.GetString(pw),
                    HostName = Options.Hostname,
                    PortNumber = 443,
                    Protocol = Protocol.Webdav,
                    WebdavSecure = true
                };

                session.Open(options);
                SynchronizationResult result = session.SynchronizeDirectories(SynchronizationMode.Remote,
                    Options.LocalDirectory, Options.RemoteDirectory, false);
            }

            Console.WriteLine("File sync complete!");
        }
    }
}
