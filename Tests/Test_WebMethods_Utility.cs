using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    class Test_WebMethods_Utility
    {
        [Test]
        public void DeleteTestDataByTable_returns_ok()
        {
            var tables = new string[]
                {
                    "author",
                    "binding",
                    "customer",
                    "customer_contact",
                    "instrument",
                    "manufacturer",
                    "product",
                    "product_product_category",
                    "product_picture",
                    "product_series",
                    "song",
                    "supplier"
                };

            foreach (string table in tables)
            {
                string result = WebMethods.Utility.DeleteTestDataByTable(table);
                result = XElement.Parse(result).Value;
                Assert.IsTrue(result == "ok",
                              string.Format("Test data could not be deleted for table {0}. Unexpected return value: {1}", table, result));
            }
        }


        [Test]
        public void DeleteTestData_returns_ok()
        {
            string result = WebMethods.Utility.DeleteTestData();
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Test data could not be deleted. Unexpected return value: {0}", result));
        }
    }
#endif
}
