using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static class CountryRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Returns an Country object from the database
        /// </summary>
        /// <param name="countryId">Id of the country to be retrieved</param>
        /// <returns>Returns an Country object populated with data</returns>
        internal static Country GetCountryById(int countryId)
        {
            Country country = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Countries where CountryID = @countryId";
                    cmd.Parameters.AddWithValue("@countryId", countryId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            while (dr.Read())
                            {
                                country = new Country();
                                country.Id = dr.GetInt32(dr.GetOrdinal("CountryID"));
                                country.Name = dr.GetStringSafe(dr.GetOrdinal("Country"));
                                country.Iso2Code = dr.GetStringSafe(dr.GetOrdinal("ISO2"));
                                country.IsEuropean = dr.GetBoolean(dr.GetOrdinal("European"));
                            }
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
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, countryId));
                throw;
            }

            return country;
        }


        /// <summary>
        /// Converts a country represented in an Xml string to an Country object
        /// </summary>
        /// <param name="xmlString">Xml definition of the country</param>
        /// <returns>Returns a Country object populated with data</returns>
        internal static Country LoadCountryFromXml(string xmlString)
        {
            var country = new Country();

            try
            {
                XElement xml = XElement.Parse(xmlString);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                    country.Id = Convert.ToInt32(xml.Element("id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("name")))
                    country.Name = xml.Element("name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("iso1_code")))
                    country.Iso2Code = xml.Element("iso1_code").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("european")))
                    country.IsEuropean = Convert.ToBoolean(int.Parse(xml.Element("european").Value));
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, xmlString));
                throw;
            }

            return country;
        }


        /// <summary>
        /// Converts a list of countries represented in an Xml string to a List<Country> object
        /// </summary>
        /// <param name="xmlString">Xml definition of the countries list</param>
        /// <returns>Returns a List<Country> object populated with data</returns>
        internal static List<Country> LoadCountriesFromXml(string xmlString)
        {
            XElement xml = XElement.Parse(xmlString);

            return xml.Nodes().Select(node => LoadCountryFromXml(node.ToString())).ToList();
        }
    }
}
