using System;
using System.Configuration;

namespace SMS2WS_SyncAgent
{
    internal static class AppSettings
    {
        private static int _waitBetweenSyncSessionsInSeconds = 60;
        private static DateTime _lastSyncTimestamp = DateTime.Now;
        private static DateTime _lastSyncTimestamp_customer_ws2sms = DateTime.Now;
        private static string _objectToBeProcessed = "";
        public static string ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static string SiteBaseUrl { get; private set; }
        public static string FtpHost { get; private set; }
        public static string FtpLogonImages { get; private set; }
        public static string FtpPasswordImages { get; private set; }
        public static string ApiBaseUri { get; private set; }
        public static string ApiMethodBaseUri { get; private set; }
        public static string ApiSecurityKey { get; private set; }
        public static string ApiOutputFormat { get; private set; }
        private static Configuration oConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        static AppSettings()
        {
            SiteBaseUrl = ConfigurationManager.AppSettings["SiteBaseUrl"];
            FtpHost = ConfigurationManager.AppSettings["FtpHost"];
            FtpLogonImages = ConfigurationManager.AppSettings["FtpLogonImages"];
            FtpPasswordImages = ConfigurationManager.AppSettings["FtpPasswordImages"];
            ApiBaseUri = ConfigurationManager.AppSettings["ApiBaseUri"];
            ApiMethodBaseUri = ConfigurationManager.AppSettings["ApiMethodBaseUri"];
            ApiSecurityKey = ConfigurationManager.AppSettings["ApiSecurityKey"];
            ApiOutputFormat = ConfigurationManager.AppSettings["ApiOutputFormat"];
        }

        public static int WaitBetweenSyncSessionsInSeconds
        {
            get { return _waitBetweenSyncSessionsInSeconds; }
            set { _waitBetweenSyncSessionsInSeconds = value; }
        }

        public static DateTime LastSyncTimestamp
        {
            get { return _lastSyncTimestamp; }
            set
            {
                _lastSyncTimestamp = value;
                oConfig.AppSettings.Settings["LastSyncTimestamp"].Value = value.ToString();
                oConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static DateTime LastSyncTimestamp_customer_ws2sms
        {
            get { return _lastSyncTimestamp_customer_ws2sms; }
            set
            {
                _lastSyncTimestamp_customer_ws2sms = value;
                oConfig.AppSettings.Settings["LastSyncTimestamp_customer_ws2sms"].Value = value.ToString();
                oConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static string ObjectToBeProcessed
        {
            get { return _objectToBeProcessed; }
            set { _objectToBeProcessed = value; }
        }
    }
}
