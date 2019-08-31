﻿using System;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Collections.Generic;
using System.Security.Cryptography;
using Topshelf.Logging;

namespace WinSCPSync
{
    class SyncService
    {
        private DirectoryMonitor _monitor;
        private Thread _thread;
        private LogWriter _logger;

        public SyncService()
        {
            _logger = HostLogger.Get<SyncService>();
            _logger.Debug("Sync Service object init start");
        }

        public bool Start()
        {
            _thread = new Thread(RunService);
            _thread.IsBackground = true;
            _thread.Start();
            _logger.Info("Starting service");
            return true;
        }

        public bool Stop()
        {
            _logger.Info("Stopping service");
            _monitor.StopMonitoring();
            return true;
        }

        private void RunService()
        {
            var hasSecret = ConfigurationManager.AppSettings
                .OfType<string>()
                .Any(x => x.Equals("Secret"));

            if (!hasSecret)
            {
                InitializeConfig();
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
        }

        private static void InitializeConfig()
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
            config.AppSettings.Settings.Add("Secret", encryptedSecret);
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
