using System;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    internal static class BindingRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Returns a Binding object from the database
        /// </summary>
        /// <param name="bindingId">Id of the binding to be retrieved</param>
        /// <returns>Returns a Binding object populated with data</returns>
        internal static Binding GetBindingById(int bindingId)
        {
            Binding binding = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Bindings where BindingID = @bindingId";
                    cmd.Parameters.AddWithValue("@bindingId", bindingId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            List<Binding> bindings = LoadBindingListFromDataReader(dr);
                            if (bindings.Count >= 1)
                                binding = bindings[0];
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, bindingId));
                throw;
            }

            return binding;
        }


        /// <summary>
        /// Retrieve a list of bindings whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns></returns>
        private static List<Binding> GetBindingChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var bindings = new List<Binding>();

            //create a connection if none exists
            if (conn == null)
            {
                conn = DAL.GetConnection();
                bLocalConnection = true;
            }
            else if (conn.State == ConnectionState.Closed)
                conn.Open();

            string sqlWhere = "";
            switch (action)
            {
                case Enums.UpdateActions.binding_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.binding_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }
            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Bindings " +
                              "where " + sqlWhere +
                              "order by BindingID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    bindings = LoadBindingListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return bindings;
        }


        /// <summary>
        /// Retrieve a list of bindings whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns></returns>
        internal static List<Binding> GetUpdatedBindingsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetBindingChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.binding_update);
        }


        /// <summary>
        /// Retrieve a list of bindings that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns></returns>
        internal static List<Binding> GetDeletedBindingsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetBindingChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.binding_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after a binding object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="bindingId">Id of the binding that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetBindingSyncStatus(int bindingId, bool status)
        {
            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    string sql = "update Bindings set " +
                                 "SyncWS = @syncDttm " +
                                 "where BindingID = @bindingId";

                    //create and execute command
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                    cmd.Parameters.AddWithValue("@bindingId", bindingId);
                    int affectedRecords = cmd.ExecuteNonQuery();

                    if (affectedRecords > 0) Console.WriteLine("Binding synched: {0}", bindingId);
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, bindingId, status));
                throw;
            }

            return true;
        }


        private static List<Binding> LoadBindingListFromDataReader(OleDbDataReader reader)
        {
            var bindings = new List<Binding>();
            int bindingId = -1;

            try
            {
                while (reader.Read())
                {
                    var binding = new Binding();
                    binding.Id = reader.GetInt32(reader.GetOrdinal("BindingID"));
                    bindingId = binding.Id;
                    binding.Name_EN = reader.GetStringSafe(reader.GetOrdinal("Naam_EN"));
                    binding.Name_NL = reader.GetStringSafe(reader.GetOrdinal("Naam_NL"));
                    binding.Name_FR = reader.GetStringSafe(reader.GetOrdinal("Naam_FR"));
                    binding.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                    binding.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                    binding.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                    bindings.Add(binding);
                }
            }
            catch (Exception exception)
            {
                log.ErrorFormat("Error in {0} (data record with BindingID={1}, exception={2})", MethodBase.GetCurrentMethod().Name, bindingId, exception);
                throw;
            }

            return bindings;
        }

        
        /// <summary>
        /// Converts a binding represented in an Xml string to a Binding object
        /// </summary>
        /// <param name="xmlString">Xml definition of the binding</param>
        /// <returns>Returns a Binding object populated with data</returns>
        internal static Binding LoadBindingFromXml(string xmlString)
        {
            var binding = new Binding();

            try
            {
                XElement xml = XElement.Parse(xmlString);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                    binding.Id = Convert.ToInt32(xml.Element("id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_en")))
                    binding.Name_EN = xml.Element("name_en").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                    binding.Name_NL = xml.Element("name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_fr")))
                    binding.Name_FR = xml.Element("name_fr").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                    binding.Test = xml.Element("test").Value.Equals("1");

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                    binding.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                    binding.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                    binding.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, xmlString));
                throw;
            }

            return binding;
        }
    }
}
