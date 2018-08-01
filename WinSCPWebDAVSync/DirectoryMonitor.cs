using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WinSCPSync
{
    class DirectoryMonitor {
        public string Directory { get; }
        public Boolean HasChanged { get; set; } = false;
        private ISynchronizer _synchronizer;
        private FileSystemWatcher _monitor;
        private CancellationTokenSource _canceler;
        private bool _watching = true; 
 

        public DirectoryMonitor(string filepath, ISynchronizer sync)
        {
            Directory = filepath;
            _synchronizer = sync;
            _canceler = new CancellationTokenSource();
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
            Console.Write("Change detected!");
            if (!HasChanged)
            {
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
                        Task.Run((Action)_synchronizer.Sync, _canceler.Token);
                        HasChanged = false;
                    }
                    await Task.Delay(3000, _canceler.Token);
                }
            } catch (TaskCanceledException)
            {
                Console.WriteLine("Task cancelled");
            }
        }

        public void StopMonitoring()
        {
            _canceler.Cancel();
            _watching = false;
            _monitor.EnableRaisingEvents = true;
        } 

    }
}
