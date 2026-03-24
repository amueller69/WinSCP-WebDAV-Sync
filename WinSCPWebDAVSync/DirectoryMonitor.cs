using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WinSCP;

namespace WinSCPSync
{
    class DirectoryMonitor
    {
        public string Directory { get; }
        public bool HasChanged { get; set; } = false;
        private readonly ISynchronizer _synchronizer;
        private readonly FileSystemWatcher _monitor;
        private DateTime _last_refresh;
        private readonly ILogger<DirectoryMonitor> _logger;

        public DirectoryMonitor(string filepath, ISynchronizer sync, ILogger<DirectoryMonitor> logger)
        {
            Directory = filepath;
            _synchronizer = sync;
            _logger = logger;
            _last_refresh = DateTime.MinValue;
            _monitor = new FileSystemWatcher(filepath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
            };
            _monitor.Changed += OnChange;
        }

        private void OnChange(object source, FileSystemEventArgs e)
        {
            if (!HasChanged)
            {
                _logger.LogInformation("Change detected! Directory marked for immediate synchronization.");
                HasChanged = true;
            }
        }

        public async Task StartMonitoring(CancellationToken cancellationToken)
        {
            try
            {
                _monitor.EnableRaisingEvents = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (DateTime.Now - _last_refresh > TimeSpan.FromMinutes(30))
                    {
                        await Task.Run(_synchronizer.Pull, cancellationToken);
                        _last_refresh = DateTime.Now;
                    }

                    if (HasChanged)
                    {
                        await Task.Run(_synchronizer.Push, cancellationToken);
                        HasChanged = false;
                    }

                    await Task.Delay(60000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Monitoring task cancelled.");
            }
            catch (SessionException)
            {
                _logger.LogError("File sync task failed due to server connection or authentication issue.");
            }
            finally
            {
                _monitor.EnableRaisingEvents = false;
            }
        }
    }
}
