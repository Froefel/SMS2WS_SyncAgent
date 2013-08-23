using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace SMS2WS_SyncAgent
{
    internal static class DAL
    {
        private static Log4NetWrapper log = LogManager.GetLogger();
        private static string _connectionString = "";
        
        public static string ConnectionString
        {
            get
            {
                if (_connectionString == "")
                {
                    //get the information out of the configuration file.
                    ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings["SMS"];

                    if (connectionStringSettings != null)
                        _connectionString = connectionStringSettings.ConnectionString;
                    else
                        throw new Exception("Missing 'ConnectionString' property in configuration file (app.config)");
                }

                return _connectionString;
            }
        }

        public static OleDbConnection GetConnection()
        {
            OleDbConnection conn = null;    

            try
            {
                //create and open connection
                conn = new OleDbConnection(ConnectionString);
                conn.Open();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Close();
                    log.Error("Could not open database connection");
                }
            }

            catch (Exception exception)
            {
                log.Error(String.Format("Could not create database connection with connection string \"{0}\"", _connectionString), exception);
            }

            return conn;
        }
    }
}
