﻿using WinSCP;
using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Topshelf.Logging;

namespace WinSCPSync
{
    interface ISynchronizer
    {
        void Sync();
    }

    class Synchronizer : ISynchronizer
    {
        public SynchronizerOptions Options { get; set; }
        private LogWriter _logger;

        public Synchronizer(IDictionary<string, string> options)
        {
            _logger = HostLogger.Get<Synchronizer>();

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
                _logger.Error(e.ToString());
            }
        }

        public void Sync()
        {
            try
            {
                _logger.Info(String.Format("Beginning synchronization of {0}", Options.LocalDirectory));
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
            catch (Exception e)
            {
                _logger.Error(e.ToString());
            }

        }
        private static void ResultHandler(object sender, TransferEventArgs e)
        {
            LogWriter logger = HostLogger.Get<Synchronizer>();

            if (e.Error == null)
            {
                logger.Info("File sync complete!");
            }
            else
            {
                logger.Error(String.Format("Upload of {0} failed: {1}", e.FileName, e.Error));
            }
        }
    }
}
