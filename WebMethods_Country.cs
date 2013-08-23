using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class CountryMethods
        {
            /// <summary>
            /// Get a country from the webshop
            /// </summary>
            /// <param name="countryId">Id of the country to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a Country object populated with data</returns>
            internal static Country GetCountryById(int countryId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("country",
                                                      "getById",
                                                      "id=" + countryId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Country country = CountryRepository.LoadCountryFromXml(xmlData);
                    return country;
                }
            }


            /// <summary>
            /// Get a list of all countries from the webshop
            /// </summary>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a list of country objects populated with data</returns>
            internal static List<Country> GetAllCountries(out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("country",
                                                      "getAll");
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    List<Country> countries = CountryRepository.LoadCountriesFromXml(xmlData);
                    return countries;
                }
            }
        }
    }
}
