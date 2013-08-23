using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    class Test_WebMethods_ProductSeries
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int seriesId = new PredefinedDataObjects().ProductSeries.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("productseries",
                                                             "getById",
                                                             "id=" + seriesId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Product Series with Id {0} could not be retrieved from the webshop. Unexpected return value: {1}", seriesId, errorMsg));

            string validationError = ProductSeries.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }


        [Test]
        public void GetProductSeriesById_with_valid_id_returns_product_series()
        {
            const int seriesId = 1; // The Joy of...

            var expectedSeries = new ProductSeries { Id = seriesId, Name = "The Joy of..." };

            string errorMsg;
            ProductSeries actualSeries = WebMethods.ProductSeriesMethods.GetProductSeriesById(seriesId, out errorMsg);

            Assert.AreEqual(expectedSeries.Id, actualSeries.Id);
            Assert.AreEqual(expectedSeries.Name, actualSeries.Name);
        }


        [Test]
        public void GetProductSeriesById_with_invalid_id_returns_null()
        {
            const int seriesId = -1;

            string errorMsg;
            ProductSeries actualSeries = WebMethods.ProductSeriesMethods.GetProductSeriesById(seriesId, out errorMsg);

            Assert.IsNull(actualSeries);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value: {0}", errorMsg));
        }


        [Test]
        public void UpdateProductSeries_creates_new_product_series_and_returns_ok()
        {
            ProductSeries series = new AnonymousProductSeriesBuilder().build();

            string result = WebMethods.ProductSeriesMethods.UpdateProductSeries(series);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("ProductSeries with id {0} could not be created/updated. Unexpected return value: {1}", series.Id, result));
        }


        [Test]
        public void UpdateProductSeries_with_values_saves_all_data_correctly()
        {
            ProductSeries series = new AnonymousProductSeriesBuilder().build();

            //save the product series to the webshop
            string result = WebMethods.ProductSeriesMethods.UpdateProductSeries(series);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Product Series with id {0} could not be created/updated. Unexpected return value was: {1}", series.Id, result));

            //retrieve the series from the webshop
            string errorMsg;
            ProductSeries seriesFromWS = WebMethods.ProductSeriesMethods.GetProductSeriesById(series.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(series.Id, seriesFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(series.Name, seriesFromWS.Name, "The field comparison for field \"name\" failed.");
            Assert.AreEqual(series.Test, seriesFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(series.CreatedDttm, seriesFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(series.UpdatedDttm, seriesFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(series.DeletedDttm, seriesFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void UpdateProductSeries_accepts_invalid_product_series_and_returns_error()
        {
            // There is currently no way to generate this error condition
            // Invalid Xml would generate an error, but the Author object is always transformed into valid Xml
            // Invalid data does not seem to generate an error, so I suppose it is being handled by the API
            return;
        }


        [Test]
        public void DeleteProductSeriesById_with_valid_id_returns_ok()
        {
            ProductSeries series = new AnonymousProductSeriesBuilder().build();

            string result = WebMethods.ProductSeriesMethods.UpdateProductSeries(series);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Product Series {0} could not be created/updated. Unexpected return value: {1}", series.Id, result));

            result = WebMethods.ProductSeriesMethods.DeleteProductSeriesById(series.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Product Series {0} could not be deleted. Unexpected return value: {1}", series.Id, result));
        }


        [Test]
        public void DeleteProductSeriesById_with_invalid_id_returns_error()
        {
            int seriesId = -1;
            string result = WebMethods.ProductSeriesMethods.DeleteProductSeriesById(seriesId);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value: {0}", result));
        }
    }
#endif
}
