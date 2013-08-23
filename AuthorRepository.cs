using System;
using System.Data;
using System.Data.OleDb;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    internal static class AuthorRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Returns an Author object from the database
        /// </summary>
        /// <param name="authorId">Id of the author to be retrieved</param>
        /// <returns>Returns an Author object populated with data</returns>
        internal static Author GetAuthorById(int authorId)
        {
            Author author = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Componisten where ComponistID = @authorId";
                    cmd.Parameters.AddWithValue("@authorId", authorId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            List<Author> authors = LoadAuthorListFromDataReader(dr);
                            if (authors.Count >= 1)
                                author = authors[0];
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
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, authorId));
                throw;
            }

            return author;
        }


        /// <summary>
        /// Retrieve a list of authors whose data has changed or been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>Returns a list of authors</returns>
        private static List<Author> GetAuthorChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var authors = new List<Author>();

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
                case Enums.UpdateActions.author_update:
                    {
                        sqlWhere = "SyncWS Is Null " +
                                   "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                                   "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                        break;
                    }
                case Enums.UpdateActions.author_delete:
                {
                    sqlWhere = "SyncWS Is Null " +
                               "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                    break;
                }
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Componisten " +
                              "where " + sqlWhere +
                              "order by ComponistID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            //execute a datareader, closing the connection when all the data is read from it
            try
            {
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        authors = LoadAuthorListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return authors;
        }


        /// <summary>
        /// Retrieve a list of authors whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of authors</returns>
        internal static List<Author> GetUpdatedAuthorsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetAuthorChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.author_update);
        }


        /// <summary>
        /// Retrieve a list of authors that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns a list of authors</returns>
        internal static List<Author> GetDeletedAuthorsByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetAuthorChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.author_delete);
        }


        /// <summary>
        /// Set the Synchronization status in the SMS after an author object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="authorId">Id of the author that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetAuthorSyncStatus(int authorId, bool status)
        {
            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    if (conn != null)
                    {
                        string sql = "update Componisten set " +
                                     "SyncWS = @syncDttm " +
                                     "where ComponistID = @authorId";

                        //create and execute command
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                        cmd.Parameters.AddWithValue("@authorId", authorId);
                        int affectedRecords = cmd.ExecuteNonQuery();

                        if (affectedRecords > 0) Console.WriteLine("Author synched: {0}", authorId);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, authorId, status));
                throw;
            }

            return true;
        }


        private static List<Author> LoadAuthorListFromDataReader(OleDbDataReader reader)
        {
            var authors = new List<Author>();
            int authorId = -1;

            try
            {
                while (reader.Read())
                {
                    var author = new Author();
                    author.Id = reader.GetInt32(reader.GetOrdinal("ComponistID"));
                    authorId = author.Id;
                    author.Name = reader.GetStringSafe(reader.GetOrdinal("Naam"));
                    author.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                    author.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                    author.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));

                    authors.Add(author);
                }
            }
            catch (Exception exception)
            {
                log.ErrorFormat("Error in {0} (data record with ComponistID={1}, exception={2})", MethodBase.GetCurrentMethod().Name, authorId, exception);
                throw;
            }

            return authors;
        }


        /// <summary>
        /// Converts an author represented in an Xml string to an Author object
        /// </summary>
        /// <param name="xmlString">Xml definition of the author</param>
        /// <returns>Returns an Author object populated with data</returns>
        internal static Author LoadAuthorFromXml(string xmlString)
        {
            var author = new Author();

            try
            {
                XElement xml = XElement.Parse(xmlString);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                    author.Id = Convert.ToInt32(xml.Element("id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                    author.Name = xml.Element("name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                    author.Test = xml.Element("test").Value.Equals("1");

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                    author.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                    author.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                    author.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);

            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, xmlString));
                throw;
            }

            return author;
        }
    }
}
