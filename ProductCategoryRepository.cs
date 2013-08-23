using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static class ProductCategoryRepository
    {
        /// <summary>
        /// Returns a ProductCategory object from the database
        /// </summary>
        /// <param name="productCategoryId">Id of the product category to be retrieved</param>
        /// <returns>Returns a ProductCategory object populated with data</returns>
        internal static ProductCategory GetProductCategoryById(int productCategoryId)
        {
            ProductCategory productCategory = null;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select * from ProductCategories where CategoryID = @productCategoryId";
                cmd.Parameters.AddWithValue("@productCategoryId", productCategoryId);

                try
                {
                    //execute a datareader, closing the connection when all the data is read from it
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        List<ProductCategory> categories = LoadProductCategoryListFromDataReader(dr);
                        if (categories.Count >= 1)
                            productCategory = categories[0];
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return productCategory;
        }


        /// <summary>
        /// Retrieve a list of product categories whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of product categories</returns>
        private static List<ProductCategory> GetProductCategoryChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var categories = new List<ProductCategory>();

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
                case Enums.UpdateActions.productCategory_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.productCategory_delete:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                        break;
                    }
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from ProductCategories " +
                              "where CategoryName <> \"[ Nieuwe categorie ]\" and " + sqlWhere +
                              "order by CategoryID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        categories = LoadProductCategoryListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return categories;
        }


        /// <summary>
        /// Retrieve a list of product categories whose data has changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of product categories</returns>
        internal static List<ProductCategory> GetUpdatedProductCategoriesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetProductCategoryChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.productCategory_update);
        }


        /// <summary>
        /// Retrieve a list of product categories that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of product categories</returns>
        internal static List<ProductCategory> GetDeletedProductCategoriesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetProductCategoryChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.productCategory_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after a product category object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="productCategoryId">Id of the product category that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetProductCategorySyncStatus(int productCategoryId, bool status)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "update ProductCategories set " +
                             "SyncWS = @syncDttm, " +
                             "PictureUpdated = false " +
                             "where CategoryID = @categoryId";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                cmd.Parameters.AddWithValue("@categoryId", productCategoryId);
                int affectedRecords = cmd.ExecuteNonQuery();

                if (affectedRecords > 0) Console.WriteLine("Product category synched: {0}", productCategoryId);
            }
            return true;
        }


        private static List<ProductCategory> LoadProductCategoryListFromDataReader(OleDbDataReader reader)
        {
            var categories = new List<ProductCategory>();

            while (reader.Read())
            {
                var category = new ProductCategory();
                category.Id = reader.GetInt32(reader.GetOrdinal("CategoryID"));
                category.ParentId = reader.GetNullableInt32(reader.GetOrdinal("ParentCategoryID"));
                category.Name = reader.GetStringSafe(reader.GetOrdinal("CategoryName"));
                category.SortOrder = reader.GetInt32(reader.GetOrdinal("Sequence"));
                category.PictureFilename = reader.GetStringSafe(reader.GetOrdinal("WS_PictureFile"));
                category.PictureUpdated = reader.GetBoolean(reader.GetOrdinal("PictureUpdated"));
                category.TargetUrl = reader.GetStringSafe(reader.GetOrdinal("TargetURL"));
                category.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                category.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                category.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                categories.Add(category);
            }

            return categories;
        }


        /// <summary>
        /// Converts a product category represented in an Xml string to a ProductCategory object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product category</param>
        /// <returns>Returns a ProductCategory object populated with data</returns>
        internal static ProductCategory LoadProductCategoryFromXml(string xmlString)
        {
            var category = new ProductCategory();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                category.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_nl")))
                category.Name = xml.Element("name_nl").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("parent_id")))
                category.ParentId = Convert.ToInt32(xml.Element("parent_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("path")))
                category.Path = xml.Element("path").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("product_count")))
                category.ProductCount = Convert.ToInt32(xml.Element("product_count").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("picture_filename")))
                category.PictureFilename = xml.Element("picture_filename").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("target_url")))
                category.TargetUrl = xml.Element("target_url").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("sort_order")))
                category.SortOrder = Convert.ToInt32(xml.Element("sort_order").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                category.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created"), "0000-00-00 00:00:00"))
                category.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated"), "0000-00-00 00:00:00"))
                category.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted"), "0000-00-00 00:00:00"))
                category.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return category;
        }


        /// <summary>
        /// Converts a list of product categories represented in an Xml string to a List<ProductCategory> object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product category list</param>
        /// <returns>Returns a List<ProductCategory> object populated with data</returns>
        internal static List<ProductCategory> LoadProductCategoriesFromXml(string xmlString)
        {
            XElement xml = XElement.Parse(xmlString);

            return xml.Nodes().Select(node => LoadProductCategoryFromXml(node.ToString())).ToList();
        }
    }
}
