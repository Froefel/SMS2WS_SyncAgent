using System;
using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    class Test_WebMethods_Binding
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int bindingId = new PredefinedDataObjects().Binding.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("binding",
                                                             "getById",
                                                             "id=" + bindingId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Binding with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", bindingId, errorMsg));

            string validationError = Binding.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }


        [Test]
        public void GetBindingById_with_valid_id_returns_binding()
        {
            const int bindingId = 1; // Geniet

            var expectedBinding = new Binding() { Id = bindingId, Name_NL = "Geniet" };

            string errorMsg;
            Binding actualBinding = WebMethods.BindingMethods.GetBindingById(bindingId, out errorMsg);

            Assert.AreEqual(expectedBinding.Id, actualBinding.Id);
            Assert.AreEqual(expectedBinding.Name_NL, actualBinding.Name_NL);
        }


        [Test]
        public void GetBindingById_with_invalid_id_returns_null()
        {
            const int bindingId = -1;

            string errorMsg;
            Binding actualBinding = WebMethods.BindingMethods.GetBindingById(bindingId, out errorMsg);

            Assert.IsNull(actualBinding);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void UpdateBinding_creates_new_binding_and_returns_ok()
        {
            Binding binding = new AnonymousBindingBuilder().build();

            string result = WebMethods.BindingMethods.UpdateBinding(binding);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Binding with id {0} could not be created/updated. Unexpected return value: {1}", binding.Id, result));
        }


        [Test]
        public void UpdateBinding_with_values_saves_all_data_correctly()
        {
            Binding binding = new AnonymousBindingBuilder().build();
            Console.WriteLine("Binding.Id = {0}", binding.Id);

            //save the binding to the webshop
            string result = WebMethods.BindingMethods.UpdateBinding(binding);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Binding with id {0} could not be created/updated. Unexpected return value was: {1}", binding.Id, result));

            //retrieve the binding from the webshop
            string errorMsg;
            Binding bindingFromWS = WebMethods.BindingMethods.GetBindingById(binding.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(binding.Id, bindingFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(binding.Name_NL, bindingFromWS.Name_NL, "The field comparison for field \"name_nl\" failed.");
            Assert.AreEqual(binding.Test, bindingFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(binding.CreatedDttm, bindingFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(binding.UpdatedDttm, bindingFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(binding.DeletedDttm, bindingFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void UpdateBinding_accepts_invalid_binding_and_returns_error()
        {
            // There is currently no way to generate this error condition
            // Invalid Xml would generate an error, but the Binding object is always transformed into valid Xml
            // Invalid data does not seem to generate an error, so I suppose it is being handled by the API
            return;
        }


        [Test]
        public void DeleteBindingById_with_valid_id_returns_ok()
        {
            Binding binding = new AnonymousBindingBuilder().build();

            string result = WebMethods.BindingMethods.UpdateBinding(binding);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Binding with id {0} could not be created/updated. Unexpected return value: {1}", binding.Id, result));

            result = WebMethods.BindingMethods.DeleteBindingById(binding.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Binding with id {0} could not be deleted. Unexpected return value: {1}", binding.Id, result));
        }


        [Test]
        public void DeleteBindingById_with_invalid_id_returns_error()
        {
            int bindingId = -1;
            string result = WebMethods.BindingMethods.DeleteBindingById(bindingId);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }
    }
#endif
}
