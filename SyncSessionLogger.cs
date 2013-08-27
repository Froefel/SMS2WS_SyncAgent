using System;
using System.Data.OleDb;
using System.Reflection;

namespace SMS2WS_SyncAgent
{
    static class SyncSessionLogger
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        public static void WriteResult(int sessionId, Enums.UpdateActions action, int resultSuccess, int resultFailed)
        {
            WriteResult(sessionId, action, resultSuccess, resultFailed, null);

        }

        public static void WriteResult(int sessionId, Enums.UpdateActions action, int resultSuccess, int resultFailed, int? resultNew)
        {
            //check if anything needs to be logged
            if (resultSuccess + resultFailed == 0)
                return;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "insert into SyncAgentSessionResults " +
                                      "(" +
                                      "SyncSessionId, " +
                                      "[Timestamp], " +
                                      "SyncAction, " +
                                      "SyncResultSuccess, " +
                                      "SyncResultFailed, " +
                                      "SyncResultNew" +
                                      ") " +
                                      "values " +
                                      "(" +
                                      "@sync_session_id, " +
                                      "@timestamp, " +
                                      "@sync_action, " +
                                      "@sync_result_success, " +
                                      "@sync_result_failed, " +
                                      "@sync_result_new" +
                                      ")";
                    cmd.Parameters.AddWithValue("@sync_session_id", sessionId);
                    cmd.Parameters.AddWithValue("@timestamp", DateTime.Parse(DateTime.Now.ToString()));
                    cmd.Parameters.AddWithValue("@sync_action", action.ToString());
                    cmd.Parameters.AddWithValue("@sync_result_success", resultSuccess);
                    cmd.Parameters.AddWithValue("@sync_result_failed", resultFailed);
                    cmd.Parameters.AddWithValue("@sync_result_new", resultNew != 0 ? resultNew ?? (object)DBNull.Value : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, action, resultSuccess, resultFailed));
                throw;
            }
        }


        public static int GetNextSessionId()
        {
            int sessionId;
            string sql = "";

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    sql = "select max(SyncSessionId) as LastSessionId " +
                          "from  SyncAgentSessionResults";
                    cmd.CommandText = sql;
                    var tmp = cmd.ExecuteScalar();
                    sessionId = tmp.Equals(DBNull.Value) ? 0 : (int) tmp;
                    sessionId++;
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + sql, exception);
            }

            return sessionId;
        }
    }
}
