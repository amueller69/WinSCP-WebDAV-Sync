using Topshelf;
using NLog;
using System.Configuration;

namespace WinSCPSync
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
                .FilePath;
            LogManager.LoadConfiguration(path);
            var factory = new LogFactory(LogManager.Configuration);
            HostFactory.Run(x =>
            {
                x.Service<SyncService>(service =>
                {
                    service.ConstructUsing(_ => new SyncService());
                    service.WhenStarted(svc => svc.Start());
                    service.WhenStopped(svc => svc.Stop());
                    service.WhenShutdown(svc => svc.Stop());
                });

                x.SetDescription("WinSCP WebDAV Synchronization Service");
                x.SetDisplayName("WinSCPSyncSvc");
                x.SetServiceName("WinSCPSyncSvc");
                x.RunAsNetworkService();
                x.StartAutomatically();
                x.UseNLog(factory);
            });
        }
    }
}

