using System;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class ProductMethods
        {
            /// <summary>
            /// Get a product from the webshop
            /// </summary>
            /// <param name="productId">product Id for the product to be retrieved</param>
            /// <returns>Returns a Product object populated with data</returns>
            internal static Product GetProductById(int productId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("product",
                                                      "getById",
                                                      "id=" + productId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Product product = ProductRepository.LoadProductFromXml(xmlData);
                    return product;
                }
            }


            /// <summary>
            /// Update a product in the webshop
            /// </summary>
            /// <param name="product">The product to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateProduct(Product product)
            {
                string data = product.ToXml();
                string result = SendDataThroughWebMethod("product",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            /// <summary>
            /// Deletes a product from the webshop
            /// </summary>
            /// <param name="productId">Product Id for the product to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteProductById(int productId)
            {
                string result = GetDataFromWebMethod("product",
                                                     "deleteById",
                                                     "id=" + productId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }
        }
    }
}
