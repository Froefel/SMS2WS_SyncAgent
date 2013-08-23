using System.Collections.Generic;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal class ProductCategoryMethods
        {
            /// <summary>
            /// Get a product category from the Webshop
            /// </summary>
            /// <param name="categoryId">Category Id of the prodict category to be retrieved</param>
            /// <returns>Returns a ProductCategory object populated with data</returns>
            internal static ProductCategory GetProductCategoryById(int categoryId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("productcategory",
                                                      "getById",
                                                      "id=" + categoryId);
                
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    ProductCategory category = ProductCategoryRepository.LoadProductCategoryFromXml(xmlData);
                    return category;
                }
            }


            /// <summary>
            /// Get all product categories from the Webshop
            /// </summary>
            /// <returns>Returns a list of ProductCategory objects populated with data</returns>
            internal static List<ProductCategory> GetAllProductCategories(out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("productcategory",
                                                      "getAll");
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    List<ProductCategory> categories = ProductCategoryRepository.LoadProductCategoriesFromXml(xmlData);
                    return categories;
                }
            }


            /// <summary>
            /// Update a product category in the Webshop
            /// </summary>
            /// <param name="category">ProductCategory object to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateProductCategory(ProductCategory category)
            {
                string data = category.ToXml();
                string result = SendDataThroughWebMethod("productcategory",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            /// <summary>
            /// Delete a product ctegory from the webshop
            /// </summary>
            /// <param name="categoryId">Category Id of the category to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteProductCategoryById(int categoryId)
            {
                string result = GetDataFromWebMethod("productcategory",
                                                     "deleteById",
                                                     "id=" + categoryId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            /// <summary>
            /// Update the product counts for all product categories in the Webshop
            /// </summary>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateProductCount()
            {
                string result = GetDataFromWebMethod("productcategory",
                                                     "updateProductCount");
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }
        }
    }
}
