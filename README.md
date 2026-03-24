# WinSCP-WebDAV-Sync

A lightweight Windows Service that keeps a local directory in sync with a WebDAV server using the [WinSCP .NET assembly](https://winscp.net/eng/docs/library). It watches the local directory for changes and pushes them to the remote, and periodically pulls from the remote to keep the local copy up to date.

## How it works

- **Push:** A `FileSystemWatcher` monitors the configured local directory (including subdirectories). When any file or folder change is detected, the service queues a sync and pushes local changes to the remote server within the next 60 seconds.
- **Pull:** Every 30 minutes the service pulls from the remote server to refresh the local directory with any changes made elsewhere.
- **Credential security:** On first run, the service encrypts the plaintext password from the config file using Windows DPAPI and saves the encrypted value back to disk. The plaintext password is never stored after that. The encryption is tied to the local machine's key store, so the config file is useless if copied to another machine.

## Requirements

- Windows 10/11 or Windows Server
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8) (or SDK if building from source)

## Configuration

Copy `appsettings.json` to your deployment directory and fill in the following fields under `AppSettings`:

```json
{
  "AppSettings": {
    "Username": "your_webdav_username",
    "Hostname": "your.webdav.server.com",
    "Password": "your_plaintext_password",
    "Secret": "",
    "LocalDirectory": "C:\\path\\to\\local\\folder",
    "RemoteDirectory": "/remote/path"
  }
}
```

Leave `Secret` blank. On first run the service will encrypt `Password`, populate `Secret`, and rewrite the file automatically.

The service connects to the WebDAV server on **port 443 over HTTPS**. If your server uses a different port or plain HTTP, you will need to adjust `Synchronizer.cs` before building.

## Building

```
dotnet build
```

## Installing as a Windows Service

From an **elevated** (Administrator) command prompt, navigate to the published output directory and run:

```
sc create WinSCPSyncSvc binPath= "C:\path\to\WinSCPSync.exe" start= auto
sc description WinSCPSyncSvc "WinSCP WebDAV Synchronization Service"
sc start WinSCPSyncSvc
```

To uninstall:

```
sc stop WinSCPSyncSvc
sc delete WinSCPSyncSvc
```

## Logs

When running as a Windows Service, logs are written to the **Windows Event Log** under `Application > WinSCPSyncSvc`. Open Event Viewer (`eventvwr.msc`) to view them.

When running the executable directly (outside of the service host), logs are printed to the console — useful for testing your configuration before installing.

## Publishing a self-contained executable

To produce a single deployable folder that does not require .NET to be separately installed on the target machine:

```
dotnet publish -c Release -r win-x64 --self-contained
```

Output will be in `bin\Release\net8.0-windows\win-x64\publish\`.
