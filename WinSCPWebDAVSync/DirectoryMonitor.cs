using System;
using WinSCP;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Topshelf.Logging;

namespace WinSCPSync
{
    class DirectoryMonitor {
        public string Directory { get; }
        public Boolean HasChanged { get; set; } = false;
        private ISynchronizer _synchronizer;
        private FileSystemWatcher _monitor;
        private CancellationTokenSource _canceler;
        private bool _watching = true;
        private readonly LogWriter _logger;


        public DirectoryMonitor(string filepath, ISynchronizer sync)
        {
            Directory = filepath;
            _synchronizer = sync;
            _canceler = new CancellationTokenSource();
            _logger = HostLogger.Get<DirectoryMonitor>();
            _monitor = new FileSystemWatcher(filepath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = (
                    NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
                )
            };

            _monitor.Changed += new FileSystemEventHandler(OnChange);
        }

        private void OnChange(object source, FileSystemEventArgs e)
        {
            if (!HasChanged)
            {
                _logger.Info("Change detected! Directory marked for immediate synchronization.");
                HasChanged = true;
            }
        }

        public async void StartMonitoring()
        {
            try
            {
                _monitor.EnableRaisingEvents = true;
                while (_watching)
                {
                    if (HasChanged)
                    {
                        await Task.Run(_synchronizer.Sync, _canceler.Token);
                        HasChanged = false;
                    }
                    await Task.Delay(60000, _canceler.Token);
                }
            } catch (TaskCanceledException)
            {
                _logger.Warn("Task cancelled!");
            } catch (SessionException)
            {
                _logger.Error("File sync task failed due to server connection or authentication issue");
            }
        }

        public void StopMonitoring()
        {
            _canceler.Cancel();
            _watching = false;
            _monitor.EnableRaisingEvents = false;
        } 

    }
}
