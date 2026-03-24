using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinSCP;

namespace WinSCPSync
{
    public class SyncService : BackgroundService
    {
        private readonly ILogger<SyncService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public SyncService(ILogger<SyncService> logger, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                string password = GetDecryptedPassword();
                var settings = _configuration.GetSection("AppSettings");

                var configDict = new Dictionary<string, string>
                {
                    ["Username"] = settings["Username"] ?? "",
                    ["Hostname"] = settings["Hostname"] ?? "",
                    ["Password"] = password,
                    ["LocalDirectory"] = settings["LocalDirectory"] ?? "",
                    ["RemoteDirectory"] = settings["RemoteDirectory"] ?? "/"
                };

                var sync = new Synchronizer(configDict, _loggerFactory.CreateLogger<Synchronizer>());
                var monitor = new DirectoryMonitor(configDict["LocalDirectory"], sync, _loggerFactory.CreateLogger<DirectoryMonitor>());
                _logger.LogDebug("Sync service initialized");

                await monitor.StartMonitoring(stoppingToken);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Could not decrypt password. Clear the Secret value and replace the Password value in appsettings.json to re-encrypt on next run.");
            }
            catch (SessionException)
            {
                _logger.LogError("Failed to initialize connection to remote host. Verify config information in appsettings.json.");
            }
        }

        private string GetDecryptedPassword()
        {
            var settings = _configuration.GetSection("AppSettings");
            string? secretEncrypted = settings["Secret"];

            if (string.IsNullOrEmpty(secretEncrypted))
            {
                // First run: encrypt the plaintext password and save it back to appsettings.json
                string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                string json = File.ReadAllText(configPath);
                var doc = JsonNode.Parse(json)!;
                var appSettings = doc["AppSettings"]!.AsObject();

                string plainPassword = appSettings["Password"]!.GetValue<string>();
                byte[] secret = RandomNumberGenerator.GetBytes(32);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(plainPassword);

                string encryptedPassword = EncryptValue(passwordBytes, secret);
                string encryptedSecret = EncryptValue(secret, null);
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
                Array.Clear(secret, 0, secret.Length);

                appSettings["Secret"] = encryptedSecret;
                appSettings["Password"] = encryptedPassword;
                File.WriteAllText(configPath, doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

                _logger.LogInformation("Password encrypted and saved to configuration.");
                return plainPassword;
            }

            byte[] entropy = DecryptValue(secretEncrypted, null);
            byte[] bytes = DecryptValue(settings["Password"]!, entropy);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string EncryptValue(byte[] unencrypted, byte[]? entropy)
        {
            byte[] encrypted = ProtectedData.Protect(unencrypted, entropy, DataProtectionScope.LocalMachine);
            return BitConverter.ToString(encrypted).Replace("-", string.Empty);
        }

        private static byte[] DecryptValue(string encrypted, byte[]? entropy)
        {
            byte[] byteArray = Enumerable.Range(0, encrypted.Length / 2)
                .Select(x => Convert.ToByte(encrypted.Substring(x * 2, 2), 16))
                .ToArray();
            return ProtectedData.Unprotect(byteArray, entropy, DataProtectionScope.LocalMachine);
        }
    }
}
