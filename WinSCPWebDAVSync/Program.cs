using Topshelf;
using NLog;
using System;
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
            var logger = factory.GetCurrentClassLogger();
            logger.Debug("Loaded Config");
            var hf = HostFactory.Run(x =>
            {
                x.Service<SyncService>(service =>
                {
                    service.ConstructUsing(name => new SyncService());
                    service.WhenStarted(svc => svc.Start());
                    service.WhenStopped(svc => svc.Stop());
                    logger.Debug("Init service");
                });

                x.RunAsLocalSystem();
                x.SetDescription("WinSCP WebDAV Synchronization Service");
                x.SetDisplayName("WinSCPSyncSvc");
                x.SetServiceName("WinSCPSyncSvc");
                x.UseNLog(factory);
                logger.Debug("Configured service");
            });

            var exitCode = (int)Convert.ChangeType(hf, hf.GetTypeCode());
            Environment.ExitCode = exitCode;
        }
    }
}

