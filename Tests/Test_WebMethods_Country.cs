using System;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
 #if DEBUG
    [TestFixture    ]
    class Test_WebMethods_Country
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int countryId = (int)Enums.Country.Belgium;

            string xmlData = WebMethods.GetDataFromWebMethod("country",
                                                             "getById",
                                                             "id=" + countryId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Country with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", countryId, errorMsg));

            string validationError = Country.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }


        [Test]
        public void GetCountryById_with_valid_id_returns_country()
        {
            const int countryId = 21; // Belgium

            var expectedCountry = new Country { Id = countryId, Name = "Belgium" };

            string errorMsg;
            Country actualCountry = WebMethods.CountryMethods.GetCountryById(countryId, out errorMsg);

            Assert.AreEqual(expectedCountry.Id, actualCountry.Id);
            Assert.AreEqual(expectedCountry.Name, actualCountry.Name);
        }


        [Test]
        public void GetCountryById_with_invalid_id_returns_null()
        {
            const int countryId = -1;

            string errorMsg;
            Country actualCountry = WebMethods.CountryMethods.GetCountryById(countryId, out errorMsg);

            Assert.IsNull(actualCountry);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }
    }
#endif
}
