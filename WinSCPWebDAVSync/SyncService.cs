using System;
using WinSCP;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using Topshelf.Logging;
using Topshelf;

namespace WinSCPSync
{
    class SyncService
    {
        private DirectoryMonitor _monitor;
        private Thread _thread;
        private LogWriter _logger;
        private HostControl _control;

        public SyncService()
        {
            _logger = HostLogger.Get<SyncService>();
            _logger.Debug("Began SyncService init");
        }

        public bool Start(HostControl control)
        {
            _control = control;
            _thread = new Thread(RunService);
            _thread.IsBackground = true;
            _thread.Start();
            _logger.Info("Starting service");
            return true;
        }

        public bool Stop()
        {
            _logger.Info("Stopping service");
            if (_monitor != null)
            {
            _monitor.StopMonitoring();
            }
            return true;
        }

        private void RunService()
        {
            try
            {
                var hasSecret = ConfigurationManager.AppSettings
                    .OfType<string>()
                    .Any(x => x.Equals("Secret"));

                if (!hasSecret || string.IsNullOrEmpty(ConfigurationManager.AppSettings["Secret"]))
                {
                    InitializeConfig(hasSecret);
                }

                var configDict = ConfigurationManager.AppSettings.AllKeys
                    .ToDictionary(key => key, key => ConfigurationManager.AppSettings[key]);
                byte[] entropy = DecryptValue(configDict["Secret"], null);
                byte[] bytes = DecryptValue(configDict["Password"], entropy);
                configDict["Password"] = Encoding.UTF8.GetString(bytes);
                var _sync = new Synchronizer(configDict);
                _monitor = new DirectoryMonitor(configDict["LocalDirectory"], _sync);
                _logger.Debug("Sync Service object init end");
                _monitor.StartMonitoring();
            } catch (CryptographicException ex)
            {
                _logger.Debug(ex);
                _logger.Error("Could not decrypt password. Clear the secret value and replace the password value in " +
                    "the config file in order to re-encrpyt password on next run.");
                _control.Stop();
            } catch (SessionException)
            {
                _logger.Error("Failed to initialize connection to remote host. Verify config information");
                _control.Stop();
            }
        }

        private static void InitializeConfig(bool hasSecret)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var secret = new byte[32];
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetBytes(secret);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings.Get("Password"));
            string encrypted = EncryptValue(bytes, secret);
            Array.Clear(bytes, 0, bytes.Length);
            string encryptedSecret = EncryptValue(secret, null);
            if (!hasSecret)
            {
                config.AppSettings.Settings.Add("Secret", encryptedSecret);
            } else
            {
                config.AppSettings.Settings["Secret"].Value = encryptedSecret;
            }
            config.AppSettings.Settings["Password"].Value = encrypted;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private static string EncryptValue(byte[] unencrypted, byte[] entropy)
        {
            byte[] encrypted = ProtectedData.Protect(unencrypted, entropy, DataProtectionScope.LocalMachine);
            return BitConverter.ToString(encrypted).Replace("-", string.Empty);
        }


        private static byte[] DecryptValue(string encrypted, byte[] entropy)
        {
            IEnumerable<int> range = Enumerable.Range(0, encrypted.Length / 2);
            byte[] byteArray = range.Select(x => Convert.ToByte(encrypted.Substring(x * 2, 2), 16)).ToArray();
            return ProtectedData.Unprotect(byteArray, entropy, DataProtectionScope.LocalMachine);
        }


    }
}
