using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class SupplierMethods
        {
            /// <summary>
            /// Get a supplier from the Webshop
            /// </summary>
            /// <param name="supplierId">Id of the supplier to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a Supplier object populated with data</returns>
            internal static Supplier GetSupplierById(int supplierId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("supplier",
                                                      "getById",
                                                      "id=" + supplierId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Supplier supplier = SupplierRepository.LoadSupplierFromXml(xmlData);
                    return supplier;
                }
            }

            /// <summary>
            /// Update a supplier in the webshop
            /// </summary>
            /// <param name="supplier">The supplier to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateSupplier(Supplier supplier)
            {
                string data = supplier.ToXml();
                string result = SendDataThroughWebMethod("supplier",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

            /// <summary>
            /// Delete a supplier from the webshop
            /// </summary>
            /// <param name="supplierId">Id of the supplier to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteSupplierById(int supplierId)
            {
                string result = GetDataFromWebMethod("supplier",
                                                     "deleteById",
                                                     "id=" + supplierId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

        }
    }
}
