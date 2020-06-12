using WinSCP;
using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Topshelf.Logging;

namespace WinSCPSync
{
    interface ISynchronizer
    {
        void Push();
        void Pull();
    }

    class Synchronizer : ISynchronizer
    {
        public SynchronizerOptions Options { get; set; }
        private LogWriter _logger;

        public Synchronizer(IDictionary<string, string> options)
        {
            _logger = HostLogger.Get<Synchronizer>();
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
            _logger.Info("Performing initial synchronization task");
            Push();
        }

        public void Push()
        {
            try
            {
                _logger.Info(string.Format("Beginning synchronization of {0}", Options.LocalDirectory));
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

                    session.FileTransferred += ResultHandler;
                    session.Open(options);
                    SynchronizationResult result = session.SynchronizeDirectories(SynchronizationMode.Remote,
                        Options.LocalDirectory, Options.RemoteDirectory, false);
                }
            }
            catch (SessionException e)
            {
                _logger.Debug(e.ToString());
                throw;
            }

        }

        public void Pull()
        {
            try
            {
                _logger.Info("Beginning periodic local directory refresh");
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

                    session.FileTransferred += ResultHandler;
                    session.Open(options);
                    SynchronizationResult result = session.SynchronizeDirectories(SynchronizationMode.Local,
                                                    Options.LocalDirectory, Options.RemoteDirectory, false);
                }
            } catch (SessionException e)
            {
                _logger.Debug(e.ToString());
                throw;
            }
        }
        private static void ResultHandler(object sender, TransferEventArgs e)
        {
            LogWriter logger = HostLogger.Get<Synchronizer>();

            if (e.Error == null)
            {
                logger.Info(String.Format("Synchronization of file {0} from {1} directory is complete!", 
                    e.FileName, e.Side.ToString()));
            }
            else
            {
                logger.Error(String.Format("Upload of {0} failed: {1}", e.FileName, e.Error));
            }
        }
    }
}
