using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    public class Test_WebMethods_Supplier
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int supplierId = new PredefinedDataObjects().Supplier.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("supplier",
                                                             "getById",
                                                             "id=" + supplierId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Supplier with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", supplierId, errorMsg));

            string validationError = Supplier.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }

        
        [Test]
        public void GetSupplierById_with_valid_id_returns_supplier()
        {
            const int supplierId = 211; //Bas Van den Broek

            var expectedSupplier = new Supplier();
            expectedSupplier.Id = supplierId;
            expectedSupplier.Name = "Bas van den Broek";

            string errorMsg;
            Supplier actualSupplier = WebMethods.SupplierMethods.GetSupplierById(supplierId, out errorMsg);

            Assert.AreEqual(expectedSupplier.Id, actualSupplier.Id);
            Assert.AreEqual(expectedSupplier.Name, actualSupplier.Name);
        }


        [Test]
        public void GetSupplierById_with_invalid_id_returns_error()
        {
            const int supplierId = -1;

            string errorMsg;
            Supplier supplier = WebMethods.SupplierMethods.GetSupplierById(supplierId, out errorMsg);

            Assert.IsNull(supplier);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void UpdateSupplier_creates_new_supplier_and_returns_ok()
        {
            Supplier newSupplier = new AnonymousSupplierBuilder().build();

            string result = WebMethods.SupplierMethods.UpdateSupplier(newSupplier);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Supplier with id {0} could not be created/updated. Unexpected return value: {1}", newSupplier.Id, result));
        }


        [Test]
        public void UpdateSupplier_with_values_saves_all_data_correctly()
        {
            Supplier supplier = new AnonymousSupplierBuilder().build();

            //save the supplier to the webshop
            string result = WebMethods.SupplierMethods.UpdateSupplier(supplier);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Supplier with id {0} could not be created/updated. Unexpected return value was: {1}", supplier.Id, result));

            //retrieve the supplier from the webshop
            string errorMsg;
            Supplier supplierFromWS = WebMethods.SupplierMethods.GetSupplierById(supplier.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(supplier.Id, supplierFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(supplier.Name, supplierFromWS.Name, "The field comparison for field \"name\" failed.");
            Assert.AreEqual(supplier.Test, supplierFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(supplier.CreatedDttm, supplierFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(supplier.UpdatedDttm, supplierFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(supplier.DeletedDttm, supplierFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void DeleteSupplierById_with_valid_id_returns_ok()
        {
            Supplier supplier = new AnonymousSupplierBuilder().build();

            string result = WebMethods.SupplierMethods.UpdateSupplier(supplier);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Supplier with id {0} could not be created/updated. Unexpected return value: {1}", supplier.Id, result));

            result = WebMethods.SupplierMethods.DeleteSupplierById(supplier.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Supplier with id {0} could not be deleted. Unexpected return value: {1}", supplier.Id, result));
        }


        [Test]
        public void DeleteAuthorById_with_invalid_id_returns_error()
        {
            int authorId = -1;
            string result = WebMethods.AuthorMethods.DeleteAuthorById(authorId);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }
    }
#endif
}
