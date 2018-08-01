using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Security.Cryptography;
using System.ServiceProcess;
using WinSCP;
using Topshelf;
using System.IO;
using System.Collections.Specialized;
using System.Threading;

namespace WinSCPSync
{
    class Program
    {
        static void Main(string[] args)
        {
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
                x.DependsOnEventLog();
            });
        }
    }
}

