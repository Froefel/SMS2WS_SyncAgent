using System;
using System.Data;
using System.Data.OleDb;
using System.Xml.Linq;
using System.Collections.Generic;


namespace SMS2WS_SyncAgent
{
    internal static class SupplierRepository
    {
        /// <summary>
        /// Returns a Supplier object from the database
        /// </summary>
        /// <param name="supplierId">Id of the supplier to be retrieved</param>
        /// <returns>Returns a Supplier object populated with data</returns>
        internal static Supplier GetSupplierById(int supplierId)
        {
            Supplier supplier = null;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = @"select * from Verdelers where VerdelerID = @supplierId";
                cmd.Parameters.AddWithValue("@supplierId", supplierId);

                try
                {
                    //execute a datareader, closing the connection when all the data is read from it
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        List<Supplier> suppliers = LoadSupplierListFromDataReader(dr);
                        if (suppliers.Count >= 1)
                            supplier = suppliers[0];
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return supplier;
        }


        /// <summary>
        /// Retrieve a list of suppliers whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of suppliers</returns>
        private static List<Supplier> GetSupplierChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var suppliers = new List<Supplier>();

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
                case Enums.UpdateActions.supplier_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.supplier_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }
            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Verdelers " +
                              "where " + sqlWhere +
                              "order by VerdelerID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        suppliers = LoadSupplierListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return suppliers;
        }


        /// <summary>
        /// Retrieve a list of suppliers whose data has changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of suppliers</returns>
        internal static List<Supplier> GetUpdatedSuppliersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetSupplierChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.supplier_update);
        }


        /// <summary>
        /// Retrieve a list of suppliers that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of suppliers</returns>
        internal static List<Supplier> GetDeletedSuppliersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetSupplierChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.supplier_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after a supplier object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="supplierId">Id of the supplier that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetSupplierSyncStatus(int supplierId, bool status)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "update Verdelers set " +
                             "SyncWS = @syncDttm " +
                             "where VerdelerID = @supplierId";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                cmd.Parameters.AddWithValue("@supplierId", supplierId);
                int affectedRecords = cmd.ExecuteNonQuery();

                if (affectedRecords > 0) Console.WriteLine("Supplier synched: {0}", supplierId);
            }
            return true;
        }


        private static List<Supplier> LoadSupplierListFromDataReader(OleDbDataReader reader)
        {
            var suppliers = new List<Supplier>();

            while (reader.Read())
            {
                var supplier = new Supplier();
                supplier.Id = reader.GetInt32(reader.GetOrdinal("VerdelerID"));
                supplier.Name = reader.GetStringSafe(reader.GetOrdinal("Naam"));
                supplier.MinimumDeliveryDays = reader.GetNullableInt16(reader.GetOrdinal("WS_min_delivery_days"));
                supplier.MaximumDeliveryDays = reader.GetNullableInt16(reader.GetOrdinal("WS_max_delivery_days"));
                supplier.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                supplier.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                supplier.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                suppliers.Add(supplier);
            }

            return suppliers;
        }

        
        /// <summary>
        /// Converts a supplier represented in an Xml string to a Supplier object
        /// </summary>
        /// <param name="xmlString">Xml definition of the supplier</param>
        /// <returns>Returns a Supplier object populated with data</returns>
        internal static Supplier LoadSupplierFromXml(string xmlString)
        {
            var supplier = new Supplier();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                supplier.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                supplier.Name = xml.Element("name").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("min_delivery_days")))
                supplier.MinimumDeliveryDays = Convert.ToInt32(xml.Element("min_delivery_days").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("max_delivery_days")))
                supplier.MaximumDeliveryDays = Convert.ToInt32(xml.Element("max_delivery_days").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                supplier.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                supplier.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                supplier.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                supplier.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return supplier;
        }
    }
}
