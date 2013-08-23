using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class ProductSeriesMethods
        {
            /// <summary>
            /// Get a product series from the webshop
            /// </summary>
            /// <param name="seriesId">Id of the product series to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a ProductSeries object populated with data</returns>
            internal static ProductSeries GetProductSeriesById(int seriesId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("productseries",
                                                      "getById",
                                                      "id=" + seriesId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    ProductSeries series = ProductSeriesRepository.LoadProductSeriesFromXml(xmlData);
                    return series;
                }
            }

            /// <summary>
            /// Update a product series in the webshop
            /// </summary>
            /// <param name="series">The product series to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateProductSeries(ProductSeries series)
            {
                string data = series.ToXml();
                string result = SendDataThroughWebMethod("productseries",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

            /// <summary>
            /// Delete a product series from the webshop
            /// </summary>
            /// <param name="seriesId">Id of the product series to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteProductSeriesById(int seriesId)
            {
                string result = GetDataFromWebMethod("productseries",
                                                     "deleteById",
                                                     "id=" + seriesId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

        }
    }
}
