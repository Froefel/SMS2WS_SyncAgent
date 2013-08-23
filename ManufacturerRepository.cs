using System;
using System.Data;
using System.Data.OleDb;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    internal static class ManufacturerRepository
    {
        /// <summary>
        /// Returns a Manufacturer object from the database
        /// </summary>
        /// <param name="manufacturerId">Id of the manufacturer to be retrieved</param>
        /// <returns>Returns an Manufacturer object populated with data</returns>
        internal static Manufacturer GetManufacturerById(int manufacturerId)
        {
            Manufacturer manufacturer = null;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"select * from Uitgevers where UitgeverID = @manufacturerId";
                cmd.Parameters.AddWithValue("@manufacturerId", manufacturerId);

                try
                {
                    //execute a datareader, closing the connection when all the data is read from it
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        List<Manufacturer> manufacturers = LoadManufacturerListFromDataReader(dr);
                        if (manufacturers.Count >= 1)
                            manufacturer = manufacturers[0];
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return manufacturer;
        }


        /// <summary>
        /// Retrieve a list of manufacturers whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of manufacturers</returns>
        private static List<Manufacturer> GetManufacturerChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var manufacturers = new List<Manufacturer>();

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
                case Enums.UpdateActions.manufacturer_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.manufacturer_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Uitgevers " +
                              "where " + sqlWhere +
                              "order by UitgeverID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        manufacturers = LoadManufacturerListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return manufacturers;
        }


        /// <summary>
        /// Retrieve a list of manufacturers whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of manufacturers</returns>
        internal static List<Manufacturer> GetUpdatedManufacturersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetManufacturerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.manufacturer_update);
        }


        /// <summary>
        /// Retrieve a list of manufacturers that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of manufacturers</returns>
        internal static List<Manufacturer> GetDeletedManufacturersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetManufacturerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.manufacturer_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after a manufacturer object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="manufacturerId">Id of the manufacturer that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetManufacturerSyncStatus(int manufacturerId, bool status)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "update Uitgevers set " +
                             "SyncWS = @syncDttm " +
                             "where UitgeverID = @manufacturerId";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                cmd.Parameters.AddWithValue("@manufacturerId", manufacturerId);
                int affectedRecords = cmd.ExecuteNonQuery();

                if (affectedRecords > 0) Console.WriteLine("Manufacturer synched: {0}", manufacturerId);
            }
            return true;
        }


        private static List<Manufacturer> LoadManufacturerListFromDataReader(OleDbDataReader reader)
        {
            var manufacturers = new List<Manufacturer>();

            while (reader.Read())
            {
                var manufacturer = new Manufacturer();
                manufacturer.Id = reader.GetInt32(reader.GetOrdinal("UitgeverID"));
                manufacturer.Name = reader.GetStringSafe(reader.GetOrdinal("Naam"));
                manufacturer.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                manufacturer.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                manufacturer.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                manufacturers.Add(manufacturer);
            }

            return manufacturers;
        }

        
        /// <summary>
        /// Converts a manufacturer represented in an Xml string to a Manufacturer object
        /// </summary>
        /// <param name="xmlString">Xml definition of the supplier</param>
        /// <returns>Returns a Manufacturer object populated with data</returns>
        internal static Manufacturer LoadManufacturerFromXml(string xmlString)
        {
            var manufacturer = new Manufacturer();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                manufacturer.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                manufacturer.Name = xml.Element("name").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                manufacturer.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                manufacturer.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                manufacturer.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                manufacturer.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return manufacturer;
        }
    }
}
