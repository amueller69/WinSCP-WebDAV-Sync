using WinSCP;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<Synchronizer> _logger;

        public Synchronizer(IDictionary<string, string> options, ILogger<Synchronizer> logger)
        {
            _logger = logger;
            Options = new SynchronizerOptions
            {
                Username = options["Username"],
                Password = Encoding.UTF8.GetBytes(options["Password"]),
                Hostname = options["Hostname"],
                LocalDirectory = options["LocalDirectory"],
                RemoteDirectory = options.TryGetValue("RemoteDirectory", out string? temp) ? temp : "/"
            };
            _logger.LogInformation("Performing initial synchronization task");
            Push();
        }

        public void Push()
        {
            try
            {
                _logger.LogInformation("Beginning synchronization of {Directory}", Options.LocalDirectory);
                using Session session = new Session();
                byte[] pw = new byte[Options.Password.Length];
                Options.Password.CopyTo(pw, 0);
                SessionOptions sessionOptions = new SessionOptions
                {
                    UserName = Options.Username,
                    Password = Encoding.UTF8.GetString(pw),
                    HostName = Options.Hostname,
                    PortNumber = 443,
                    Protocol = Protocol.Webdav,
                    Secure = true
                };
                Array.Clear(pw, 0, pw.Length);

                session.FileTransferred += ResultHandler;
                session.Open(sessionOptions);
                session.SynchronizeDirectories(SynchronizationMode.Remote,
                    Options.LocalDirectory, Options.RemoteDirectory, false);
            }
            catch (SessionException e)
            {
                _logger.LogDebug(e, "Session exception during Push");
                throw;
            }
        }

        public void Pull()
        {
            try
            {
                _logger.LogInformation("Beginning periodic local directory refresh");
                using Session session = new Session();
                byte[] pw = new byte[Options.Password.Length];
                Options.Password.CopyTo(pw, 0);
                SessionOptions sessionOptions = new SessionOptions
                {
                    UserName = Options.Username,
                    Password = Encoding.UTF8.GetString(pw),
                    HostName = Options.Hostname,
                    PortNumber = 443,
                    Protocol = Protocol.Webdav,
                    Secure = true
                };
                Array.Clear(pw, 0, pw.Length);

                session.FileTransferred += ResultHandler;
                session.Open(sessionOptions);
                session.SynchronizeDirectories(SynchronizationMode.Local,
                    Options.LocalDirectory, Options.RemoteDirectory, false);
            }
            catch (SessionException e)
            {
                _logger.LogDebug(e, "Session exception during Pull");
                throw;
            }
        }

        private void ResultHandler(object? sender, TransferEventArgs e)
        {
            if (e.Error == null)
                _logger.LogInformation("Synchronization of file {FileName} from {Side} directory is complete!", e.FileName, e.Side);
            else
                _logger.LogError("Synchronization of {FileName} failed: {Error}", e.FileName, e.Error);
        }
    }
}
