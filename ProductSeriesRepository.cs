using System;
using System.Data;
using System.Data.OleDb;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    internal static class ProductSeriesRepository
    {
        /// <summary>
        /// Returns a ProductSeries object from the database
        /// </summary>
        /// <param name="seriesId">Id of the product series to be retrieved</param>
        /// <returns>Returns a ProductSeries object populated with data</returns>
        internal static ProductSeries GetProductSeriesById(int seriesId)
        {
            ProductSeries series = null;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select * from ProductSeries where SeriesID = @seriesId";
                cmd.Parameters.AddWithValue("@seriesId", seriesId);

                try
                {
                    //execute a datareader, closing the connection when all the data is read from it
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        List<ProductSeries> productSeries = LoadProductSeriesListFromDataReader(dr);
                        if (productSeries.Count >= 1)
                            series = productSeries[0];
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return series;
        }


        /// <summary>
        /// Retrieve a list of product series whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of product series</returns>
        private static List<ProductSeries> GetProductSeriesChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var productSeries = new List<ProductSeries>();

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
                case Enums.UpdateActions.productSeries_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.productSeries_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from ProductSeries " +
                              "where " + sqlWhere +
                              "order by SeriesID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        productSeries = LoadProductSeriesListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return productSeries;
        }


        /// <summary>
        /// Retrieve a list of product series whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of product series</returns>
        internal static List<ProductSeries> GetUpdatedProductSeriesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetProductSeriesChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.productSeries_update);
        }


        /// <summary>
        /// Retrieve a list of product series that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of product series</returns>
        internal static List<ProductSeries> GetDeletedProductSeriesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetProductSeriesChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.productSeries_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after a product series object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="productSeriesId">Id of the product series that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetProductSeriesSyncStatus(int productSeriesId, bool status)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "update ProductSeries set " +
                             "SyncWS = @syncDttm " +
                             "where ProductSeriesID = @productSeriesId";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                cmd.Parameters.AddWithValue("@productSeriesId", productSeriesId);
                int affectedRecords = cmd.ExecuteNonQuery();

                if (affectedRecords > 0) Console.WriteLine("Product series synched: {0}", productSeriesId);
            }
            return true;
        }


        private static List<ProductSeries> LoadProductSeriesListFromDataReader(OleDbDataReader reader)
        {
            var productSeries = new List<ProductSeries>();

            while (reader.Read())
            {
                var series = new ProductSeries();
                series.Id = reader.GetInt32(reader.GetOrdinal("SeriesID"));
                series.Name = reader.GetStringSafe(reader.GetOrdinal("Naam"));
                series.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                series.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                series.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                productSeries.Add(series);
            }

            return productSeries;
        }


        /// <summary>
        /// Converts a product series represented in an Xml string to a ProductSeries object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product series</param>
        /// <returns>Returns a ProductSeries object populated with data</returns>
        internal static ProductSeries LoadProductSeriesFromXml(string xmlString)
        {
            var series = new ProductSeries();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                series.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                series.Name = xml.Element("name").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                series.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                series.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                series.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                series.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return series;
        }
    }
}
