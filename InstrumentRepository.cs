using System;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    internal static class InstrumentRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Returns an Instrument object from the database
        /// </summary>
        /// <param name="instrumentId">Id of the instrument to be retrieved</param>
        /// <returns>Returns an Instrument object populated with data</returns>
        internal static Instrument GetInstrumentById(int instrumentId)
        {
            Instrument instrument = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Instrumenten where InstrumentID = @instrumentId";
                    cmd.Parameters.AddWithValue("@instrumentId", instrumentId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            List<Instrument> instruments = LoadInstrumentListFromDataReader(dr);
                            if (instruments.Count >= 1)
                                instrument = instruments[0];
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
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, instrumentId));
                throw;
            }
            return instrument;
        }


        /// <summary>
        /// Retrieve a list of instruments whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of instruments</returns>
        private static List<Instrument> GetInstrumentChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var instruments = new List<Instrument>();

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
                case Enums.UpdateActions.instrument_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.instrument_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Instrumenten " +
                              "where " + sqlWhere +
                              "order by InstrumentID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        instruments = LoadInstrumentListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return instruments;
        }


        /// <summary>
        /// Retrieve a list of instruments whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of instruments</returns>
        internal static List<Instrument> GetUpdatedInstrumentsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetInstrumentChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.instrument_update);
        }


        /// <summary>
        /// Retrieve a list of instruments that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of instruments</returns>
        internal static List<Instrument> GetDeletedInstrumentsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetInstrumentChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.instrument_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after an instrument object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="instrumentId">Id of the instrument that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetInstrumentSyncStatus(int instrumentId, bool status)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "update Instrumenten set " +
                             "SyncWS = @syncDttm " +
                             "where InstrumentID = @instrumentId";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                cmd.Parameters.AddWithValue("@instrumentId", instrumentId);
                int affectedRecords = cmd.ExecuteNonQuery();

                if (affectedRecords > 0) Console.WriteLine("Instrument synched: {0}", instrumentId);
            }
            return true;
        }


        private static List<Instrument> LoadInstrumentListFromDataReader(OleDbDataReader reader)
        {
            var instruments = new List<Instrument>();

            while (reader.Read())
            {
                var instrument = new Instrument();
                instrument.Id = reader.GetInt32(reader.GetOrdinal("InstrumentID"));
                instrument.Name_EN = reader.GetStringSafe(reader.GetOrdinal("Naam_EN"));
                instrument.Name_NL = reader.GetStringSafe(reader.GetOrdinal("Naam_NL"));
                instrument.Name_FR = reader.GetStringSafe(reader.GetOrdinal("Naam_FR"));
                instrument.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                instrument.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                instrument.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                instruments.Add(instrument);
            }

            return instruments;
        }


        /// <summary>
        /// Converts an instrument represented in an Xml string to an Author object
        /// </summary>
        /// <param name="xmlString">Xml definition of the instrument</param>
        /// <returns>Returns an Instrument object populated with data</returns>
        internal static Instrument LoadInstrumentFromXml(string xmlString)
        {
            var instrument = new Instrument();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                instrument.Id = Convert.ToInt32(xml.Element("id").Value);

            //if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_en")))
            //    instrument.Name_EN = xml.Element("name_en").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                instrument.Name_NL = xml.Element("name").Value;

            //if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_fr")))
            //    instrument.Name_FR = xml.Element("name_fr").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                instrument.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                instrument.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                instrument.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                instrument.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return instrument;
        }
    }
}
