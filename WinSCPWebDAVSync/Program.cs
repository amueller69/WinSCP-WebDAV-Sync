using WinSCPSync;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "WinSCPSyncSvc";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<SyncService>();
    })
    .Build();

await host.RunAsync();
