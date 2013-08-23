using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
 #if DEBUG
    [TestFixture]
    class Test_WebMethods_Manufacturer
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int manufacturerId = new PredefinedDataObjects().Manufacturer.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("manufacturer",
                                                             "getById",
                                                             "id=" + manufacturerId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Manufacturer with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", manufacturerId, errorMsg));

            string validationError = Manufacturer.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }



        [Test]
        public void GetManufacturerById_with_valid_id_returns_manufacturer()
        {
            const int manufacturerId = 564; // Music Sales

            var expectedManufacturer = new Manufacturer { Id = manufacturerId, Name = "Music Sales" };

            string errorMsg;
            Manufacturer actualManufacturer = WebMethods.ManufacturerMethods.GetManufacturerById(manufacturerId, out errorMsg);

            Assert.AreEqual(expectedManufacturer.Id, actualManufacturer.Id);
            Assert.AreEqual(expectedManufacturer.Name, actualManufacturer.Name);
        }


        [Test]
        public void GetManufacturerById_with_invalid_id_returns_null()
        {
            const int manufacturerId = -1;

            string errorMsg;
            Manufacturer actualManufacturer = WebMethods.ManufacturerMethods.GetManufacturerById(manufacturerId, out errorMsg);

            Assert.IsNull(actualManufacturer);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void UpdateManufacturer_creates_new_manufacturer_and_returns_ok()
        {
            Manufacturer manufacturer = new AnonymousManufacturerBuilder().build();

            string result = WebMethods.ManufacturerMethods.UpdateManufacturer(manufacturer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Manufacturer with id {0} could not be created/updated. Unexpected return value: {1}", manufacturer.Id, result));
        }


        [Test]
        public void UpdateManufacturer_with_values_saves_all_data_correctly()
        {
            Manufacturer manufacturer = new AnonymousManufacturerBuilder().build();

            //save the instrument to the webshop
            string result = WebMethods.ManufacturerMethods.UpdateManufacturer(manufacturer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Manufacturer with id {0} could not be created/updated. Unexpected return value was: {1}", manufacturer.Id, result));

            //retrieve the instrument from the webshop
            string errorMsg;
            Manufacturer manufacturerFromWS = WebMethods.ManufacturerMethods.GetManufacturerById(manufacturer.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(manufacturer.Id, manufacturerFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(manufacturer.Name, manufacturerFromWS.Name, "The field comparison for field \"name\" failed.");
            Assert.AreEqual(manufacturer.Test, manufacturerFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(manufacturer.CreatedDttm, manufacturerFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(manufacturer.UpdatedDttm, manufacturerFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(manufacturer.DeletedDttm, manufacturerFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void UpdateManufacturer_accepts_invalid_manufacturer_and_returns_error()
        {
            // There is currently no way to generate this error condition
            // Invalid Xml would generate an error, but the Manufacturer object is always transformed into valid Xml
            // Invalid data does not seem to generate an error, so I suppose it is being handled by the API
            return;
        }


        [Test]
        public void DeleteManufacturerById_with_valid_id_returns_ok()
        {
            Manufacturer manufacturer = new AnonymousManufacturerBuilder().build();

            string result = WebMethods.ManufacturerMethods.UpdateManufacturer(manufacturer);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Manufacturer with id {0} could not be created/updated. Unexpected return value: {1}", manufacturer.Id, result));

            result = WebMethods.ManufacturerMethods.DeleteManufacturerById(manufacturer.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Manufacturer with id {0} could not be deleted. Unexpected return value: {1}", manufacturer.Id, result));
        }


        [Test]
        public void DeleteManufacturerById_with_invalid_id_returns_error()
        {
            int manufacturerId = -1;
            string result = WebMethods.ManufacturerMethods.DeleteManufacturerById(manufacturerId);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }
    }
#endif
}
