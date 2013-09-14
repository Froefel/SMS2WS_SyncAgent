using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Data.OleDb;

namespace SMS2WS_SyncAgent
{
    internal static class ProductRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Retrieve a list of products whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <param name="dictLogIds">Dictionary containing the Ids of the affected log records (later used to flag as synchronized)</param>
        /// <returns>Returns a list of authors</returns>
        private static List<Product> GetProductChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action, Dictionary<int, string> dictLogIds)
        {
            var products = new List<Product>();

            bool bLocalConnection = false;

            //create a connection if none exists
            if (conn == null)
            {
                conn = DAL.GetConnection();
                bLocalConnection = true;
            }
            else if (conn.State == ConnectionState.Closed)
                conn.Open();

            //construct the query statement to get log records for updated products
            //note: the query will look in InputLog and optionally the previous year's TxLog_xxxx table
            string sqlProductUpdates = GetUpdatedProductsQueryText(timestampStart, conn);

            if (action == Enums.UpdateActions.product_delete)
                sqlProductUpdates = sqlProductUpdates.Replace("WHERE (", "WHERE Flag = 'D' and (");
            else
                sqlProductUpdates = sqlProductUpdates.Replace("WHERE (", "WHERE Flag <> 'D' and (");

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlProductUpdates;
            cmd.Parameters.AddWithValue("@timestampStart", DateTime.Parse(timestampStart.ToString()));
            cmd.Parameters.AddWithValue("@timestampEnd", DateTime.Parse(timestampEnd.ToString()));

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    products = LoadProductListFromDataReader(dr, dictLogIds);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            if (products.Count > 0)
            {
                string msg = String.Format("{0} product{1} to be updated between {2} and {3}", products.Count, products.Count == 1 ? "" : "s", timestampStart, timestampEnd);
                Console.WriteLine(msg);
                log.Info(msg);
            }

            return products;
        }


        /// <summary>
        /// Retrieve a list of products whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="dictLogIds">Dictionary containing the Ids of the affected log records (later used to flag as synchronized)</param>
        /// <returns>Returns a list of products</returns>
        internal static List<Product> GetUpdatedProductsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Dictionary<int, string> dictLogIds)
        {
            return GetProductChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.product_update, dictLogIds);
        }


        /// <summary>
        /// Retrieve a list of products that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="dictLogIds">Dictionary containing the Ids of the affected log records (later used to flag as synchronized)</param>
        /// <returns>Returns a list of products</returns>
        internal static List<Product> GetDeletedProductsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Dictionary<int, string> dictLogIds)
        {
            return GetProductChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.product_delete, dictLogIds);
        }


        /// <summary>
        /// Marks one or more product log records as synchronized
        /// </summary>
        /// <param name="productId">Product Id whose log records to consider</param>
        /// <param name="dictLogIds">Dictionary with unique Ids corresponding to log entries</param>
        /// <returns>Boolean value indicating success or failure of this method</returns>
        public static bool SetProductSyncStatus(int productId, Dictionary<int, string> dictLogIds)
        {
            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                if (dictLogIds.Count > 0)
                {
                    string commaSeparatedLogIds = string.Join(",", (from i in dictLogIds where i.Key == productId select i.Value).ToList());

                    if (commaSeparatedLogIds.Length > 0)
                    {
                        string sql = string.Format("update InputLog " +
                                                   "set SyncWS = @syncDttm " +
                                                   "where Artikelnummer = @productId " +
                                                   "and [Count] in ({0})", commaSeparatedLogIds);

                        //create and execute command
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                        cmd.Parameters.AddWithValue("@productId", productId);
                        cmd.Parameters.AddWithValue("@logIds", commaSeparatedLogIds);
                        int affectedRecords = cmd.ExecuteNonQuery();

                        if (affectedRecords > 0)
                        {
                            string msg = String.Format("{0} log record{1} synched for product {2}", affectedRecords, affectedRecords == 1 ? "" : "s", productId.ToString("D6"));
                            Console.WriteLine(msg);
                            log.Info(msg);
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Retrieve a list of unique products where each product definition contains the consolidated changes from the logging
        /// </summary>
        /// <param name="reader">OleDbDataReader containing the log records</param>
        /// <param name="dictLogIds">Dictionary object with Log IDs for each product</param>
        /// <returns>A list of products</returns>
        private static List<Product> LoadProductListFromDataReader(OleDbDataReader reader, Dictionary<int, string> dictLogIds)
        {
            var products = new List<Product>();
            int lastProductId = 0;
            var product = new Product();
            var consolidatedLogBits = new ProductLogBits();
            string lastConsolidatedLogBits = consolidatedLogBits.BitData;
            var logIds = new List<int>();
            int logId = 0;
            bool skipRemainingLogRecordsForProduct = false;

            try
            {
                while (reader.Read())
                {
                    logId = reader.GetInt32(reader.GetOrdinal("LogId"));
                    var logBits = new ProductLogBits(reader.GetStringSafe(reader.GetOrdinal("LogBits")));
                    int currentProductId = reader.GetInt32(reader.GetOrdinal("Id"));
                    string flag = reader.GetStringSafe(reader.GetOrdinal("Flag"));
                    bool newOnlineProduct = logBits.BitTest(Enums.Logfield.ActiveInWebshop) && reader.GetBoolean(reader.GetOrdinal("ActiveInWebshop"));

                    string msg = String.Format("Reading log entry {0}", logId);
                    Console.WriteLine(msg);
                    log.Info(msg);

                    //create new product
                    if (newOnlineProduct)
                    {
                        product = GetProductById(currentProductId);
                        foreach (ProductPicture pic in product.ProductPictures)
                        {
                            pic.ToBeUploaded = true;
                        } //mark all product pictures for upload
                        consolidatedLogBits = product.LogBits;
                        skipRemainingLogRecordsForProduct = true;
                    }
                    else
                    {
                        if (currentProductId != lastProductId)
                            skipRemainingLogRecordsForProduct = false;
                    }

                    if (currentProductId != lastProductId)
                    {
                        //write consolidated LogBits and LogIds of previous product
                        if (lastProductId != 0)
                        {
                            product.LogBits = consolidatedLogBits;
                            if (logIds.Count > 0)
                                dictLogIds.Add(lastProductId, string.Join(",", logIds));
                        }

                        //create new product
                        if (!newOnlineProduct)
                        {
                            product = new Product { Id = currentProductId };
                            consolidatedLogBits = new ProductLogBits();
                            lastConsolidatedLogBits = consolidatedLogBits.BitData;
                            skipRemainingLogRecordsForProduct = false;
                        }

                        logIds = new List<int>();
                    }

                    if (!skipRemainingLogRecordsForProduct)
                    {
                        if (logBits.BitTest(Enums.Logfield.PublicProductName_NL))
                        {
                            product.Name_NL = reader.GetStringSafe(reader.GetOrdinal("Name_NL"));
                            consolidatedLogBits.BitSet(Enums.Logfield.PublicProductName_NL, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.PublicProductName_EN))
                        {
                            product.Name_EN = reader.GetStringSafe(reader.GetOrdinal("Name_EN"));
                            consolidatedLogBits.BitSet(Enums.Logfield.PublicProductName_EN, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Subtitle_NL))
                        {
                            product.Subtitle_NL = reader.GetStringSafe(reader.GetOrdinal("Subtitle_NL"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Subtitle_NL, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.AuthorId) && flag != "STN" && flag != "STU" && flag != "STD")
                        {
                            product.AuthorId = reader.GetNullableInt32(reader.GetOrdinal("AuthorId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.AuthorId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.ArrangerId))
                        {
                            product.ArrangerId = reader.GetNullableInt32(reader.GetOrdinal("ArrangerId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.ArrangerId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.ManufacturerId))
                        {
                            product.ManufacturerId = reader.GetNullableInt32(reader.GetOrdinal("ManufacturerId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.ManufacturerId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.InstrumentId))
                        {
                            product.InstrumentId = reader.GetNullableInt32(reader.GetOrdinal("InstrumentId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.InstrumentId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.ReferenceNumber))
                        {
                            product.ReferenceNumber = reader.GetStringSafe(reader.GetOrdinal("ReferenceNumber"));
                            consolidatedLogBits.BitSet(Enums.Logfield.ReferenceNumber, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Isbn))
                        {
                            product.Isbn = reader.GetStringSafe(reader.GetOrdinal("Isbn"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Isbn, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Ismn))
                        {
                            product.Ismn = reader.GetStringSafe(reader.GetOrdinal("Ismn"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Ismn, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Upc))
                        {
                            product.Upc = reader.GetStringSafe(reader.GetOrdinal("Upc"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Upc, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Ean))
                        {
                            product.Ean = reader.GetStringSafe(reader.GetOrdinal("Ean"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Ean, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.LanguageId))
                        {
                            object tmp = reader.GetNullableInt32(reader.GetOrdinal("LanguageID"));
                            product.LanguageId = (tmp != null) ? (Enums.Language?)(int)tmp : null;
                            consolidatedLogBits.BitSet(Enums.Logfield.LanguageId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.BindingId))
                        {
                            product.BindingId = reader.GetNullableInt32(reader.GetOrdinal("BindingId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.BindingId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.GradeLevel))
                        {
                            product.GradeLevel = reader.GetStringSafe(reader.GetOrdinal("GradeLevel"));
                            consolidatedLogBits.BitSet(Enums.Logfield.GradeLevel, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.SeriesId))
                        {
                            product.SeriesId = reader.GetNullableInt32(reader.GetOrdinal("SeriesId"));
                            consolidatedLogBits.BitSet(Enums.Logfield.SeriesId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Pages))
                        {
                            product.Pages = reader.GetNullableInt32(reader.GetOrdinal("Pages"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Pages, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.SalesPrice))
                        {
                            product.SalesPrice = reader.GetNullableDecimal(reader.GetOrdinal("SalesPrice"));
                            consolidatedLogBits.BitSet(Enums.Logfield.SalesPrice, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Description_NL))
                        {
                            product.Description_NL = reader.GetStringSafe(reader.GetOrdinal("Description_NL"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Description_NL, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Weight))
                        {
                            product.Weight = reader.GetNullableDecimal(reader.GetOrdinal("Weight"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Weight, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Length))
                        {
                            product.Length = reader.GetNullableDecimal(reader.GetOrdinal("Length"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Length, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Width))
                        {
                            product.Width = reader.GetNullableDecimal(reader.GetOrdinal("Width"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Width, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Height))
                        {
                            product.Height = reader.GetNullableDecimal(reader.GetOrdinal("Height"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Height, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.InternalStock) ||
                            logBits.BitTest(Enums.Logfield.StockQty_G) ||
                            logBits.BitTest(Enums.Logfield.StockQty_A) ||
                            logBits.BitTest(Enums.Logfield.StockQty_Ext))
                        {
                            product.InternalStock = reader.GetNullableInt32(reader.GetOrdinal("InternalStock"));
                            consolidatedLogBits.BitSet(Enums.Logfield.InternalStock, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.ExternalStock))
                        {
                            product.ExternalStock = reader.GetNullableInt32(reader.GetOrdinal("ExternalStock"));
                            consolidatedLogBits.BitSet(Enums.Logfield.ExternalStock, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.SupplierId))
                        {
                            product.SupplierId = reader.GetNullableInt32(reader.GetOrdinal("SupplierId")) != null ? (int)reader.GetNullableInt32(reader.GetOrdinal("SupplierId")) : 0;
                            consolidatedLogBits.BitSet(Enums.Logfield.SupplierId, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.Promotion))
                        {
                            product.Promotion = reader.GetBoolean(reader.GetOrdinal("Promotion"));
                            consolidatedLogBits.BitSet(Enums.Logfield.Promotion, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.HighlightOnHome))
                        {
                            product.HighlightOnHome = reader.GetBoolean(reader.GetOrdinal("HighlightOnHome"));
                            consolidatedLogBits.BitSet(Enums.Logfield.HighlightOnHome, true);
                        }

                        product.ActiveInWebshop = reader.GetBoolean(reader.GetOrdinal("ActiveInWebshop"));   //we always set this value so we can later optimize the product pictures to be uploaded
                        if (logBits.BitTest(Enums.Logfield.ActiveInWebshop))
                        {
                            consolidatedLogBits.BitSet(Enums.Logfield.ActiveInWebshop, true);
                        }

                        //product.BestSeller = reader.GetBoolean(reader.GetOrdinal("BestSeller"));


                        if (logBits.BitTest(Enums.Logfield.MinimumOrderQty))
                        {
                            product.MinimumOrderQuantity = reader.GetNullableInt32(reader.GetOrdinal("MinimumOrderQty")) != null ? (int)reader.GetNullableInt32(reader.GetOrdinal("MinimumOrderQty")) : 1;
                            consolidatedLogBits.BitSet(Enums.Logfield.MinimumOrderQty, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.WebshopTeacherDiscount))
                        {
                            product.TeacherDiscount = reader.GetNullableByte(reader.GetOrdinal("TeacherDiscount"));
                            consolidatedLogBits.BitSet(Enums.Logfield.WebshopTeacherDiscount, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.WebshopResellerDiscount))
                        {
                            product.ResellerDiscount = reader.GetNullableByte(reader.GetOrdinal("ResellerDiscount"));
                            consolidatedLogBits.BitSet(Enums.Logfield.WebshopResellerDiscount, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.SearchKeywords))
                        {
                            product.SearchKeywords = reader.GetStringSafe(reader.GetOrdinal("SearchKeywords"));
                            consolidatedLogBits.BitSet(Enums.Logfield.SearchKeywords, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.StorePickupOnly))
                        {
                            product.StorePickupOnly = reader.GetBoolean(reader.GetOrdinal("StorePickupOnly"));
                            consolidatedLogBits.BitSet(Enums.Logfield.StorePickupOnly, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.CreateDttm) && flag != "STN" && flag != "STU" && flag != "STD")
                        {
                            product.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                            consolidatedLogBits.BitSet(Enums.Logfield.CreateDttm, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.UpdateDttm))
                        {
                            product.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                            consolidatedLogBits.BitSet(Enums.Logfield.UpdateDttm, true);
                        }

                        if (logBits.BitTest(Enums.Logfield.InputDttm) && product.UpdatedDttm == null)
                        {
                            product.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("InputDttm"));
                            consolidatedLogBits.BitSet(Enums.Logfield.UpdateDttm, true);
                        }

                        switch (flag)
                        {
                            //handle songs
                            case "STN":
                            case "STU":
                            case "STD":
                                //retrieve the current songlist for the product
                                product.Songs = GetSongsForProduct(product.Id);
                                consolidatedLogBits.BitSet(Enums.Logfield.SongSortOrder, true);
                                break;
                            case "D":
                                product.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("Timestamp"));
                                break;
                        }

                        //get product categories
                        if (logBits.BitTest(Enums.Logfield.ProductCategory) && product.ProductCategories.Count == 0)
                        {
                            product.ProductCategories = GetCategoriesForProduct(product.Id);
                            consolidatedLogBits.BitSet(Enums.Logfield.ProductCategory, true);
                        }

                        //get product pictures
                        if (logBits.BitTest(Enums.Logfield.ProductPictureFilename))
                        {
                            //retrieve all pictures only on the first log record for this product
                            if (product.ProductPictures.Count == 0)
                            {
                                product.ProductPictures = GetPicturesForProduct(product.Id);
                                consolidatedLogBits.BitSet(Enums.Logfield.ProductPictureFilename, true);
                            }

                            string pictureFilename = reader.GetStringSafe(reader.GetOrdinal("ProductPictureFilename"));
                            ProductPicture picture = product.ProductPictures.Find(p => p.FileName == pictureFilename);
                            if (picture != null)
                                picture.ToBeUploaded = logBits.BitTest(Enums.Logfield.ProductPictureFileChanged);
                        }
                    }

                    bool productFound = false;
                    for (int i = 0; i < products.Count; i++)
                    {
                        if (products[i].Id == product.Id)
                        {
                            products[i] = product;
                            productFound = true;
                            break;
                        }
                    }

                    if (!productFound)
                        products.Add(product);

                    if (lastConsolidatedLogBits != consolidatedLogBits.BitData || skipRemainingLogRecordsForProduct)
                        logIds.Add(logId);

                    lastProductId = product.Id;
                }

                //optimize products collection:
                //- remove product pictures marked for upload if the product's current status is "inactive in webshop"
                foreach (var prod in products)
                {
                    if (prod.ActiveInWebshop == false && (prod.ProductPictures.FindAll(p => p.ToBeUploaded == true)).Count > 0)
                    {
                        foreach (var pic in prod.ProductPictures.FindAll(p => p.ToBeUploaded == true))
                            pic.ToBeUploaded = false;
                    }
                }

                if (lastProductId > 0 && logIds.Count > 0)
                    dictLogIds.Add(lastProductId, string.Join(",", logIds));

                return products;
            }

            catch (Exception exception)
            {
                throw new Exception("Error while processing product change for LogId " + logId, exception);
            }

        }


        /// <summary>
        /// Returns a Product object from the database
        /// </summary>
        /// <param name="productId">Id of the product to be loaded</param>
        /// <returns></returns>
        internal static Product GetProductById(int productId)
        {
            Product product = null;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = string.Format("select * from [{0}] where Id = @productId", string.Format("ProductDataForWebshop_{0}%", Utility.GetProductType(productId) == Enums.ProductType.Book ? 6 : 21));
                cmd.Parameters.AddWithValue("@productId", productId);

                try
                {
                    //execute a datareader, closing the connection when all the data is read from it
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            object tmp;

                            product = new Product(productId);

                            product.Name_NL = dr.GetStringSafe(dr.GetOrdinal("Name_NL"));
                            product.Name_EN = dr.GetStringSafe(dr.GetOrdinal("Name_EN"));
                            product.Subtitle_NL = dr.GetStringSafe(dr.GetOrdinal("Subtitle_NL"));
                            product.AuthorId = dr.GetNullableInt32(dr.GetOrdinal("AuthorId"));
                            product.ArrangerId = dr.GetNullableInt32(dr.GetOrdinal("ArrangerId"));
                            product.ManufacturerId = dr.GetNullableInt32(dr.GetOrdinal("ManufacturerId"));
                            product.InstrumentId = dr.GetNullableInt32(dr.GetOrdinal("InstrumentId"));
                            tmp = dr.GetNullableInt32(dr.GetOrdinal("LanguageID"));
                            product.LanguageId = (tmp != null) ? (Enums.Language?)(int)tmp : null;
                            product.SupplierId = dr.GetInt32(dr.GetOrdinal("SupplierId"));
                            product.SeriesId = dr.GetNullableInt32(dr.GetOrdinal("SeriesId"));
                            product.BindingId = dr.GetNullableInt32(dr.GetOrdinal("BindingId"));
                            product.ReferenceNumber = dr.GetStringSafe(dr.GetOrdinal("ReferenceNumber"));
                            product.Isbn = dr.GetStringSafe(dr.GetOrdinal("Isbn"));
                            product.Ismn = dr.GetStringSafe(dr.GetOrdinal("Ismn"));
                            product.Upc = dr.GetStringSafe(dr.GetOrdinal("Upc"));
                            product.Ean = dr.GetStringSafe(dr.GetOrdinal("Ean"));
                            product.SalesPrice = dr.GetNullableDecimal(dr.GetOrdinal("SalesPrice"));
                            product.InternalStock = dr.GetNullableInt32(dr.GetOrdinal("InternalStock"));
                            product.ExternalStock = dr.GetNullableInt32(dr.GetOrdinal("ExternalStock"));
                            product.Description_NL = dr.GetStringSafe(dr.GetOrdinal("Description_NL"));
                            product.ActiveInWebshop = dr.GetBoolean(dr.GetOrdinal("ActiveInWebshop"));
                            product.HighlightOnHome = dr.GetBoolean(dr.GetOrdinal("HighlightOnHome"));
                            product.Promotion = dr.GetBoolean(dr.GetOrdinal("Promotion"));
                            product.TeacherDiscount = dr.GetNullableByte(dr.GetOrdinal("TeacherDiscount"));
                            product.ResellerDiscount = dr.GetNullableByte(dr.GetOrdinal("ResellerDiscount"));
                            product.SearchKeywords = dr.GetStringSafe(dr.GetOrdinal("SearchKeywords"));
                            product.StorePickupOnly = dr.GetBoolean(dr.GetOrdinal("StorePickupOnly"));
                            product.Length = dr.GetNullableDecimal(dr.GetOrdinal("Length"));
                            product.Width = dr.GetNullableDecimal(dr.GetOrdinal("Width"));
                            product.Height = dr.GetNullableDecimal(dr.GetOrdinal("Height"));
                            product.Weight = dr.GetNullableDecimal(dr.GetOrdinal("Weight"));
                            product.GradeLevel = dr.GetStringSafe(dr.GetOrdinal("GradeLevel"));
                            product.Pages = dr.GetNullableInt32(dr.GetOrdinal("Pages"));
                            product.MinimumOrderQuantity = dr.GetInt32(dr.GetOrdinal("MinimumOrderQuantity"));
                            product.CreatedDttm = dr.GetNullableDateTime(dr.GetOrdinal("CreateDttm"));
                            product.UpdatedDttm = dr.GetNullableDateTime(dr.GetOrdinal("UpdateDttm"));
                            //the following 2 statements force the LogBits to be set for those 2 properties
                            product.ProductTypeId = product.ProductTypeId;
                            product.TaxRateId = product.TaxRateId;

                            //retrieve songs
                            product.Songs = GetSongsForProduct(productId);

                            //retrieve product categories
                            product.ProductCategories = GetCategoriesForProduct(productId);

                            //retrieve product pictures
                            product.ProductPictures = GetPicturesForProduct(productId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return product;
        }


        /// <summary>
        /// Get the complete list of songs for a given product
        /// </summary>
        /// <param name="productId">Product Id for which to retrieve the songs</param>
        /// <returns></returns>
        private static List<Song> GetSongsForProduct(int productId)
        {
            var songs = new List<Song>();

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "select CLng(SubtitelID) as SubtitelID, Sequence, ST_Titel, ST_ComponistID, CreatieDatum " +
                             "from Subtitels " +
                             "where Artikelnummer = @productId " +
                             "order by Sequence";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@productId", productId);

                try
                {
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {

                            var song = new Song();
                            song.Id = dr.GetInt32(dr.GetOrdinal("SubtitelID"));
                            song.Sequence = (short)dr.GetValue(dr.GetOrdinal("Sequence"));
                            song.Title = dr.GetStringSafe(dr.GetOrdinal("ST_Titel"));
                            song.AuthorId = dr.GetNullableInt32(dr.GetOrdinal("ST_ComponistID"));
                            song.CreatedDttm = dr.GetNullableDateTime(dr.GetOrdinal("CreatieDatum"));

                            songs.Add(song);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return songs;
        }


        /// <summary>
        /// Get the complete list of product categories for a given product
        /// </summary>
        /// <param name="productId">Product Id for which to retrieve the product categories</param>
        /// <returns></returns>
        private static List<ProductCategory> GetCategoriesForProduct(int productId)
        {
            var categories = new List<ProductCategory>();

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                string sql = "select Artikelnummer, ProductCategoryID " +
                             "from Artikels_ProductCategories " +
                             "where Artikelnummer = @productId " +
                             "order by ProductCategoryID";

                //create and execute command
                var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@productId", productId);

                try
                {
                    using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            var category = new ProductCategory();
                            category.Id = dr.GetInt32(dr.GetOrdinal("ProductCategoryID"));

                            categories.Add(category);
                        }
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                }
            }

            return categories;
        }


        /// <summary>
        /// Get a list of product pictures for a given product
        /// </summary>
        /// <param name="productId">Product Id for which to retrieve the product pictures</param>
        /// <returns></returns>
        private static List<ProductPicture> GetPicturesForProduct(int productId)
        {
            var pictures = new List<ProductPicture>();
            string productPicturesPath = ConfigurationManager.AppSettings["ProductPicturesPath"];

            string[] fileNames = Directory.GetFiles(productPicturesPath, productId.ToString("D6") + "*.jpg");

            foreach (var fileName in fileNames)
            {
                var picture = new ProductPicture();
                //picture.FileName = fileName;
                picture.FilePath = fileName;

                pictures.Add(picture);
            }

            return pictures;
        }


        /// <summary>
        /// Converts a product represented in an Xml string to a Product object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product</param>
        /// <returns>Returns a Product object populated with data</returns>
        internal static Product LoadProductFromXml(string xmlString)
        {
            var product = new Product();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                product.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("product_type_id")))
                product.ProductTypeId = (Enums.ProductType)Convert.ToInt32(xml.Element("product_type_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_nl")))
                product.Name_NL = xml.Element("name_nl").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name_en")))
                product.Name_EN = xml.Element("name_en").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("subtitle_nl")))
                product.Subtitle_NL = xml.Element("subtitle_nl").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("songs")))
            {
                foreach (XNode node in xml.Element("songs").Nodes())
                    product.Songs.Add(LoadSongFromXml(node.ToString()));
            }

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("author_id")))
                product.AuthorId = Convert.ToInt32(xml.Element("author_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("arranger_id")))
                product.ArrangerId = Convert.ToInt32(xml.Element("arranger_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("manufacturer_id")))
                product.ManufacturerId = Convert.ToInt32(xml.Element("manufacturer_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("instrument_id")))
                product.InstrumentId = Convert.ToInt32(xml.Element("instrument_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("reference")))
                product.ReferenceNumber = xml.Element("reference").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("isbn")))
                product.Isbn = xml.Element("isbn").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("ismn")))
                product.Ismn = xml.Element("ismn").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("ean")))
                product.Ean = xml.Element("ean").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("upc")))
                product.Upc = xml.Element("upc").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("language_id")))
                product.LanguageId = (Enums.Language)Convert.ToInt32(xml.Element("language_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("binding_id")))
                product.BindingId = Convert.ToInt32(xml.Element("binding_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("grade_level")))
                product.GradeLevel = xml.Element("grade_level").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("product_series_id")))
                product.SeriesId = Convert.ToInt32(xml.Element("product_series_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("nr_of_pages")))
                product.Pages = Convert.ToInt32(xml.Element("nr_of_pages").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("sell_price")))
                product.SalesPrice = Convert.ToDecimal(xml.Element("sell_price").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("tax_rate_id")))
                product.TaxRateId = (Enums.TaxRate)Convert.ToInt32(xml.Element("tax_rate_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("product_pictures")))
                foreach (XNode node in xml.Element("product_pictures").Nodes())
                    product.ProductPictures.Add(LoadProductPictureFromXml(node.ToString()));

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("description_nl")))
                product.Description_NL = xml.Element("description_nl").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("weight")))
                product.Weight = Convert.ToDecimal(xml.Element("weight").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("length")))
                product.Length = Convert.ToDecimal(xml.Element("length").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("width")))
                product.Width = Convert.ToDecimal(xml.Element("width").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("height")))
                product.Height = Convert.ToDecimal(xml.Element("height").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("product_categories")))
            {
                foreach (XNode node in xml.Element("product_categories").Nodes())
                {
                    product.ProductCategories.Add(ProductCategoryRepository.LoadProductCategoryFromXml(node.ToString()));
                }
            }

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("internal_stock_qty")))
                product.InternalStock = Convert.ToInt32(xml.Element("internal_stock_qty").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("external_stock_qty")))
                product.ExternalStock = Convert.ToInt32(xml.Element("external_stock_qty").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("supplier_id")))
                product.SupplierId = Convert.ToInt32(xml.Element("supplier_id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("promotion")))
                product.Promotion = xml.Element("promotion").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("highlight_on_home")))
                product.HighlightOnHome = xml.Element("highlight_on_home").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("available")))
                product.ActiveInWebshop = xml.Element("available").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("bestseller")))
                product.BestSeller = xml.Element("bestseller").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("minimum_order_qty")))
                product.MinimumOrderQuantity = Convert.ToInt32(xml.Element("minimum_order_qty").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("teacher_discount")))
                product.TeacherDiscount = Convert.ToByte(xml.Element("teacher_discount").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("reseller_discount")))
                product.ResellerDiscount = Convert.ToByte(xml.Element("reseller_discount").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("search_keywords")))
                product.SearchKeywords = xml.Element("search_keywords").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("store_pickup_only")))
                product.StorePickupOnly = xml.Element("store_pickup_only").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                product.Test = xml.Element("test").Value.Equals("1");

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created"), "0000-00-00 00:00:00"))
                product.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated"), "0000-00-00 00:00:00"))
                product.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted"), "0000-00-00 00:00:00"))
                product.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            return product;
        }


        /// <summary>
        /// Converts a list of products represented in an Xml string to a List<Product> object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product list</param>
        /// <returns>Returns a List<Product> object populated with data</returns>
        internal static List<Product> LoadProductListFromXml(string xmlString)
        {
            var products = new List<Product>();

            XElement xml = XElement.Parse(xmlString);
            foreach (XNode node in xml.Nodes())
            {
                Product product = LoadProductFromXml(node.ToString());
                products.Add(product);
            }

            return products;
        }


        /// <summary>
        /// Converts a song represented in an Xml string to a Song object
        /// </summary>
        /// <param name="xmlString">Xml definition of the song</param>
        /// <returns>Returns a Song object populated with data</returns>
        internal static Song LoadSongFromXml(string xmlString)
        {
            var song = new Song();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                song.Id = Convert.ToInt32(xml.Element("id").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("sort_order")))
                song.Sequence = Convert.ToInt32(xml.Element("sort_order").Value);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("title")))
                song.Title = xml.Element("title").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("author_id")))
                song.AuthorId = Convert.ToInt32(xml.Element("author_id").Value);

            return song;
        }



        /// <summary>
        /// Converts a product picture represented in an Xml string to a ProductPicture object
        /// </summary>
        /// <param name="xmlString">Xml definition of the product picture</param>
        /// <returns>Returns a ProductPicture object populated with data</returns>
        internal static ProductPicture LoadProductPictureFromXml(string xmlString)
        {
            var productPicture = new ProductPicture();
            XElement xml = XElement.Parse(xmlString);

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("picture_file_name")))
                productPicture.FileName = xml.Element("picture_file_name").Value;

            if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                productPicture.Test = xml.Element("test").Value.Equals("1");

            return productPicture;
        }



        /// <summary>
        /// Check if a given product exists in the DB
        /// </summary>
        /// <param name="productId">Id of the product to be checked</param>
        internal static bool ProductExists(int productId)
        {
            bool result = false;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from " + Utility.GetProductTableName(productId) + " where Artikelnummer = @productId";
                cmd.Parameters.AddWithValue("@productId", productId);

                //execute command
                result = cmd.ExecuteScalar().Equals(1);
            }

            return result;
        }


        internal static Enums.ProductType GetProductType(int productId)
        {
            return (productId % 2 == 0) ? Enums.ProductType.Book : Enums.ProductType.NonBook;
        }


        /// <summary>
        /// Constructs the sql statement to retrieve log records for updated products in a given time frame.
        /// An existing MS-Access query that references the InputLog table serves as the basis.
        /// This method optionally adds a UNION with the previous year's TxLog_xxxx table.
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>A sql string</returns>
        private static string GetUpdatedProductsQueryText(DateTime timestampStart, OleDbConnection conn)
        {
            string sqlProductUpdates = "";

            //get a list of parameter queries
            DataTable queries = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Procedures, null);
            foreach (DataRow row in queries.Rows)
            {
                //get the sql text from the 'UpdatedProductsFromLog' query
                if (row["PROCEDURE_NAME"].ToString() == "UpdatedProductsFromLog")
                {
                    sqlProductUpdates = row["PROCEDURE_DEFINITION"].ToString();
                    break;
                }
            }

            if (sqlProductUpdates.Length > 0)
            {
                //fix a parsing error originating from Access 
                sqlProductUpdates = sqlProductUpdates.Replace("[SELECT", "(SELECT").Replace("]. AS", ") AS");

                //check if we need to modify the query to include a union to the previous year
                if (timestampStart.Year < DateTime.Now.Year)
                {
                    string sqlPreviousYear = sqlProductUpdates.Substring(sqlProductUpdates.IndexOf("SELECT InputLog"), sqlProductUpdates.IndexOf(") AS UnionLog"));
                    sqlPreviousYear = sqlPreviousYear.Replace("InputLog", "TxLog_" + timestampStart.Year);
                    sqlProductUpdates = sqlProductUpdates.Substring(0, sqlProductUpdates.IndexOf(") AS UnionLog")) +
                                        " UNION " +
                                        sqlPreviousYear;
                }
            }

            return sqlProductUpdates;
        }
    }
}
