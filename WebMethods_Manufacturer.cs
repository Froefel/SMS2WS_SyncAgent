using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class ManufacturerMethods
        {
            /// <summary>
            /// Get a manufacturer from the Webshop
            /// </summary>
            /// <param name="manufacturerId">Id of the manufacturer to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a Manufacturer object populated with data</returns>
            internal static Manufacturer GetManufacturerById(int manufacturerId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("manufacturer",
                                                      "getById",
                                                      "id=" + manufacturerId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Manufacturer manufacturer = ManufacturerRepository.LoadManufacturerFromXml(xmlData);
                    return manufacturer;
                }
            }

            /// <summary>
            /// Update a manufacturer in the webshop
            /// </summary>
            /// <param name="manufacturer">The manufacturer to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateManufacturer(Manufacturer manufacturer)
            {
                string data = manufacturer.ToXml();
                string result = SendDataThroughWebMethod("manufacturer",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

            /// <summary>
            /// Delete a manufacturer from the webshop
            /// </summary>
            /// <param name="manufacturerId">Id of the manufacturer to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteManufacturerById(int manufacturerId)
            {
                string result = GetDataFromWebMethod("manufacturer",
                                                     "deleteById",
                                                     "id=" + manufacturerId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

        }
    }
}
