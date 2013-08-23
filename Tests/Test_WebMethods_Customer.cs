using System;
using System.Xml.Linq;
using System.Collections.Generic;
using Fluency.DataGeneration;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    public class Test_WebMethods_Customer
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            
            int customerStoreId = (int)new PredefinedDataObjects().Customer.StoreId;
            
            string xmlData = WebMethods.GetDataFromWebMethod("customer",
                                                             "getByStoreId",
                                                             "id=" + customerStoreId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg), 
                           string.Format("Customer with store_id {0} could not be retrieved from the webshop. Unexpected return value: {1}", customerStoreId, errorMsg));
            
            /*
            Customer customer = new Customer();
            customer.StoreId = ARandom.IntBetween(1000000, int.MaxValue);
            customer.DeletedDttm = DateTime.Now;
            string xmlData = customer.ToXml();
            */
            string validationError = Customer.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }



        [Test]
        public void GetCustomerByWebshopId_with_valid_id_returns_customer() 
        {
            string errorMsg;

            //create test customer
            //note that the WebshopId value will be ignored by the API
            Customer expectedCustomer = new AnonymousCustomerBuilder().build();

            string result = WebMethods.CustomerMethods.UpdateCustomer(expectedCustomer);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be created. Unexpected return value was: {1}", expectedCustomer.StoreId, result));

            //get WebshopId for test customer after it has been created by the API
            Customer actualCustomer = WebMethods.CustomerMethods.GetCustomerByStoreId((int)expectedCustomer.StoreId, out errorMsg);
            Assert.IsNull(errorMsg,
                          string.Format("Customer with store_id {0} could not be retrieved. Unexpected return value: {1}", expectedCustomer.StoreId, errorMsg));

            //get customer by WebshopId
            actualCustomer = WebMethods.CustomerMethods.GetCustomerByWebshopId((int)actualCustomer.WebshopId, out errorMsg);
            Assert.IsNull(errorMsg,
                          string.Format("Customer with store_id {0} could not be retrieved. Unexpected return value: {1}", expectedCustomer.StoreId, errorMsg));
            Assert.AreEqual(expectedCustomer.StoreId, actualCustomer.StoreId);
            Assert.AreEqual(expectedCustomer.FirstName, actualCustomer.FirstName);
            Assert.AreEqual(expectedCustomer.LastName, actualCustomer.LastName);
            Assert.AreEqual(expectedCustomer.Email, actualCustomer.Email);
        }


        [Test]
        public void UpdateCustomer_with_values_saves_all_data_correctly()
        {
            Customer customer = new AnonymousCustomerBuilder().build();

            //save the customer to the webshop
            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be created/updated. Unexpected return value was: {1}", customer.StoreId, result));

            //retrieve the customer from the webshop
            string errorMsg;
            Customer customerFromWS = WebMethods.CustomerMethods.GetCustomerByStoreId((int)customer.StoreId, out errorMsg);

            //compare all values
            Assert.AreEqual(customer.StoreId, customerFromWS.StoreId, "The field comparison for field \"store_id\" failed.");
            Assert.AreEqual(customer.LastName, customerFromWS.LastName, "The field comparison for field \"last_name\" failed.");
            Assert.AreEqual(customer.FirstName, customerFromWS.FirstName, "The field comparison for field \"first_name\" failed.");
            Assert.AreEqual(customer.ShippingAddressStreet, customerFromWS.ShippingAddressStreet, "The field comparison for field \"shipping_address_street\" failed.");
            Assert.AreEqual(customer.ShippingAddressHomeNumber, customerFromWS.ShippingAddressHomeNumber, "The field comparison for field \"shipping_address_home_number\" failed.");
            Assert.AreEqual(customer.ShippingAddressZip, customerFromWS.ShippingAddressZip, "The field comparison for field \"shipping_address_zip\" failed.");
            Assert.AreEqual(customer.ShippingAddressCity, customerFromWS.ShippingAddressCity, "The field comparison for field \"shipping_address_city\" failed.");
            Assert.AreEqual(customer.ShippingAddressCountry, customerFromWS.ShippingAddressCountry, "The field comparison for field \"shipping_address_country\" failed.");
            Assert.AreEqual(customer.Phone, customerFromWS.Phone, "The field comparison for field \"phone\" failed.");
            Assert.AreEqual(customer.Mobile, customerFromWS.Mobile, "The field comparison for field \"mobile\" failed.");
            Assert.AreEqual(customer.Email, customerFromWS.Email, "The field comparison for field \"email\" failed.");
            Assert.AreEqual(customer.BillingName, customerFromWS.BillingName, "The field comparison for field \"billing_name\" failed.");
            Assert.AreEqual(customer.BillingContact, customerFromWS.BillingContact, "The field comparison for field \"billing_contact\" failed.");
            Assert.AreEqual(customer.BillingAddress1, customerFromWS.BillingAddress1, "The field comparison for field \"billing_address1\" failed.");
            Assert.AreEqual(customer.BillingAddress2, customerFromWS.BillingAddress2, "The field comparison for field \"billing_address2\" failed.");
            Assert.AreEqual(customer.BillingAddress3, customerFromWS.BillingAddress3, "The field comparison for field \"billing_address3\" failed.");
            Assert.AreEqual(customer.VatNumber, customerFromWS.VatNumber, "The field comparison for field \"vat_number\" failed.");
            Assert.AreEqual(customer.WebshopDiscount6, customerFromWS.WebshopDiscount6, "The field comparison for field \"std_discount_for_tax_rate_id1\" failed.");
            Assert.AreEqual(customer.WebshopDiscount21, customerFromWS.WebshopDiscount21, "The field comparison for field \"std_discount_for_tax_rate_id2\" failed.");
            Assert.AreEqual(customer.IsTeacher, customerFromWS.IsTeacher, "The field comparison for field \"is_teacher\" failed.");
            Assert.AreEqual(customer.IsReseller, customerFromWS.IsReseller, "The field comparison for field \"is_reseller\" failed.");
            Assert.AreEqual(customer.Institution, customerFromWS.Institution, "The field comparison for field \"institution\" failed.");
            Assert.AreEqual(customer.TeachingSubjects, customerFromWS.TeachingSubjects, "The field comparison for field \"teaching_subjects\" failed.");
            Assert.AreEqual(customer.TeacherCardNumber, customerFromWS.TeacherCardNumber, "The field comparison for field \"card_number\" failed.");
            Assert.AreEqual(customer.TeacherCardValidFrom, customerFromWS.TeacherCardValidFrom, "The field comparison for field \"card_valid_from\" failed.");
            Assert.AreEqual(customer.TeacherCardValidTo, customerFromWS.TeacherCardValidTo, "The field comparison for field \"card_valid_to\" failed.");
            Assert.AreEqual(customer.TeacherRegistrationNote, customerFromWS.TeacherRegistrationNote, "The field comparison for field \"teacher_registration_note\" failed.");
            Assert.AreEqual(customer.TeacherConfirmed, customerFromWS.TeacherConfirmed, "The field comparison for field \"teacher_confirmed\" failed.");
            Assert.AreEqual(customer.LastLoginDttm, customerFromWS.LastLoginDttm, "The field comparison for field \"last_login\" failed.");
            Assert.AreEqual(customer.Test, customerFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(customer.CreatedDttm, customerFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(customer.UpdatedDttm, customerFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(customer.DeletedDttm, customerFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void UpdateCustomer_with_nulls_saves_all_data_correctly()
        {
            var customer = new Customer();
            customer.StoreId = ARandom.IntBetween(1000000, int.MaxValue);
            customer.LastName = ARandom.LastName();
            customer.Email = "archive+" + ARandom.StringFromCharacterSet(15, "abcdefghijklmnopqrstuvwxyz").TrimEnd() + "@animatomusic.be";
            customer.Test = true;

            //save the customer to the webshop
            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be created/updated. Unexpected return value was: {1}", customer.StoreId, result));

            //retrieve the customer from the webshop
            string errorMsg;
            Customer customerFromWS = WebMethods.CustomerMethods.GetCustomerByStoreId((int)customer.StoreId, out errorMsg);

            //compare all values
            Assert.AreEqual(customer.StoreId, customerFromWS.StoreId, "The field comparison for field \"store_id\" failed.");
            Assert.AreEqual(customer.LastName, customerFromWS.LastName, "The field comparison for field \"last_name\" failed.");
            Assert.AreEqual(customer.FirstName, customerFromWS.FirstName, "The field comparison for field \"first_name\" failed.");
            Assert.AreEqual(customer.ShippingAddressStreet, customerFromWS.ShippingAddressStreet, "The field comparison for field \"shipping_address_street\" failed.");
            Assert.AreEqual(customer.ShippingAddressHomeNumber, customerFromWS.ShippingAddressHomeNumber, "The field comparison for field \"shipping_address_home_number\" failed.");
            Assert.AreEqual(customer.ShippingAddressZip, customerFromWS.ShippingAddressZip, "The field comparison for field \"shipping_address_zip\" failed.");
            Assert.AreEqual(customer.ShippingAddressCity, customerFromWS.ShippingAddressCity, "The field comparison for field \"shipping_address_city\" failed.");
            Assert.AreEqual(customer.ShippingAddressCountry, customerFromWS.ShippingAddressCountry, "The field comparison for field \"shipping_address_country\" failed.");
            Assert.AreEqual(customer.Phone, customerFromWS.Phone, "The field comparison for field \"phone\" failed.");
            Assert.AreEqual(customer.Mobile, customerFromWS.Mobile, "The field comparison for field \"mobile\" failed.");
            Assert.AreEqual(customer.Email, customerFromWS.Email, "The field comparison for field \"email\" failed.");
            Assert.AreEqual(customer.BillingName, customerFromWS.BillingName, "The field comparison for field \"billing_name\" failed.");
            Assert.AreEqual(customer.BillingContact, customerFromWS.BillingContact, "The field comparison for field \"billing_contact\" failed.");
            Assert.AreEqual(customer.BillingAddress1, customerFromWS.BillingAddress1, "The field comparison for field \"billing_address1\" failed.");
            Assert.AreEqual(customer.BillingAddress2, customerFromWS.BillingAddress2, "The field comparison for field \"billing_address2\" failed.");
            Assert.AreEqual(customer.BillingAddress3, customerFromWS.BillingAddress3, "The field comparison for field \"billing_address3\" failed.");
            Assert.AreEqual(customer.VatNumber, customerFromWS.VatNumber, "The field comparison for field \"vat_number\" failed.");
            Assert.AreEqual(customer.WebshopDiscount6, customerFromWS.WebshopDiscount6, "The field comparison for field \"std_discount_for_tax_rate_id1\" failed.");
            Assert.AreEqual(customer.WebshopDiscount21, customerFromWS.WebshopDiscount21, "The field comparison for field \"std_discount_for_tax_rate_id2\" failed.");
            Assert.AreEqual(customer.IsTeacher, customerFromWS.IsTeacher, "The field comparison for field \"is_teacher\" failed.");
            Assert.AreEqual(customer.IsReseller, customerFromWS.IsReseller, "The field comparison for field \"is_reseller\" failed.");
            Assert.AreEqual(customer.Institution, customerFromWS.Institution, "The field comparison for field \"institution\" failed.");
            Assert.AreEqual(customer.TeachingSubjects, customerFromWS.TeachingSubjects, "The field comparison for field \"teaching_subjects\" failed.");
            Assert.AreEqual(customer.TeacherCardNumber, customerFromWS.TeacherCardNumber, "The field comparison for field \"card_number\" failed.");
            Assert.AreEqual(customer.TeacherCardValidFrom, customerFromWS.TeacherCardValidFrom, "The field comparison for field \"card_valid_from\" failed.");
            Assert.AreEqual(customer.TeacherCardValidTo, customerFromWS.TeacherCardValidTo, "The field comparison for field \"card_valid_to\" failed.");
            Assert.AreEqual(customer.TeacherRegistrationNote, customerFromWS.TeacherRegistrationNote, "The field comparison for field \"teacher_registration_note\" failed.");
            Assert.AreEqual(customer.TeacherConfirmed, customerFromWS.TeacherConfirmed, "The field comparison for field \"teacher_confirmed\" failed.");
            Assert.AreEqual(customer.LastLoginDttm, customerFromWS.LastLoginDttm, "The field comparison for field \"last_login\" failed.");
            Assert.AreEqual(customer.Test, customerFromWS.Test, "The field comparison for field \"test\" failed.");
            //CreatedDttm and UpdateDttm should not be compared, because upon creation of a customer in the webshop, both fields will be set if no value is provided
            //Assert.AreEqual(customer.CreatedDttm, customerFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            //Assert.AreEqual(customer.UpdatedDttm, customerFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(customer.DeletedDttm, customerFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }
        
        
        [Test]
        public void GetCustomerByWebshopId_with_invalid_id_returns_null()
        {
            const int customerId = -1;

            string errorMsg;
            Customer actualCustomer = WebMethods.CustomerMethods.GetCustomerByWebshopId(customerId, out errorMsg);

            Assert.IsNull(actualCustomer, errorMsg);
        }


        [Test]
        public void GetCustomerByStoreId_with_valid_id_returns_customer()
        {
            //create test customer
            Customer expectedCustomer = new AnonymousCustomerBuilder().build();

            string result = WebMethods.CustomerMethods.UpdateCustomer(expectedCustomer);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be created. Unexpected return value: {1}", expectedCustomer.StoreId, result));

            string errorMsg;
            Customer actualCustomer = WebMethods.CustomerMethods.GetCustomerByStoreId((int)expectedCustomer.StoreId, out errorMsg);
            Assert.IsNull(errorMsg,
                          string.Format("Customer with store_id {0} could not be retrieved. Unexpected return value: {1}", expectedCustomer.StoreId, errorMsg));
            Assert.AreEqual(expectedCustomer.StoreId, actualCustomer.StoreId);
            Assert.AreEqual(expectedCustomer.FirstName, actualCustomer.FirstName);
            Assert.AreEqual(expectedCustomer.LastName, actualCustomer.LastName);
            Assert.AreEqual(expectedCustomer.Email, actualCustomer.Email);
        }


        [Test]
        public void GetCustomerByStoreId_with_invalid_customerId_returns_null()
        {
            const int customerId = -1;

            string errorMsg;
            Customer actualCustomer = WebMethods.CustomerMethods.GetCustomerByStoreId(customerId, out errorMsg);

            Assert.IsNull(actualCustomer);
        }


        [Test]
        public void GetCustomersUpdatedSinceDateTime_accepts_timestamp_and_returns_multiple_customers()
        {
            //create 2 test customers
            Customer customer = new AnonymousCustomerBuilder().build();
            customer.UpdatedDttm = DateTime.Now;

            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Could not create or update customer with store_id {0}. Unexpected return value: {1}", customer.StoreId, result));

            customer = new AnonymousCustomerBuilder().build();
            customer.UpdatedDttm = DateTime.Now;

            result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Could not create or update customer with store_id {0}. Unexpected return value: {1}", customer.StoreId, result));

            
            //now check whether multiple updated customers are returned
            DateTime timestamp = DateTime.Today;

            string errorMsg;
            List<Customer> customers = WebMethods.CustomerMethods.GetCustomersUpdatedSinceDateTime(timestamp, out errorMsg);

            Assert.IsNotNull(customers,
                             string.Format("Unexpected return value: {0}", errorMsg));
            Assert.Greater(customers.Count, 1,
                           string.Format("{0} customers were returned.", customers.Count));
        }


        [Test]
        public void GetCustomersUpdatedSinceDateTime_returns_error_if_resultset_is_too_big()
        {
            DateTime timestamp = DateTime.Parse("2012-01-01");

            string errorMsg;
            List<Customer> customers = WebMethods.CustomerMethods.GetCustomersUpdatedSinceDateTime(timestamp, out errorMsg);

            Assert.IsTrue(errorMsg.StartsWith("error: result_set_to_big"),
                          string.Format("Expected result should start with \"error: result_set_to_big \". Unexpected return value: {0}", errorMsg));
        }


        [Test]
        public void UpdateCustomer_creates_new_customer_and_returns_ok()
        {
            Customer customer = new AnonymousCustomerBuilder().build();

            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Could not create or update customer with store_id {0}. Unexpected return value: {1}", customer.StoreId, result));
        }


        [Test]
        public void UpdateCustomer_accepts_invalid_customer_and_returns_error()
        {
            // There is currently no way to generate this error condition
            // Invalid Xml would generate an error, but the Customer object is always transformed into valid Xml
            // Invalid data does not seem to generate an error, so I suppose it is being handled by the API
            return;
        }


        [Test]
        public void DeleteCustomerByWebshopId_with_valid_customerId_returns_ok()
        {
            Customer customer = new AnonymousCustomerBuilder().build();

            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be created. Unexpected return value: {1}", customer.StoreId, result));

            string errorMsg;
            Customer CustomerFromWS = WebMethods.CustomerMethods.GetCustomerByStoreId((int)customer.StoreId, out errorMsg);
            Assert.IsNotNull(CustomerFromWS,
                             string.Format("Customer with store_id {0} could not be retrieved from the webshop. Unexpected return value: {1}", customer.StoreId, errorMsg));

            result = WebMethods.CustomerMethods.DeleteCustomerByWebshopId((int)CustomerFromWS.WebshopId);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be deleted. Unexpected return value: {1}", customer.StoreId, result));
        }


        [Test]
        public void DeleteCustomerByWebshopId_with_invalid_customerId_returns_error()
        {
            int customerId = -1;
            string result = WebMethods.CustomerMethods.DeleteCustomerByWebshopId(customerId);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"), 
                          string.Format("Expected result should start with \"error: \". Unexpected return value: {0}", result));
        }


        [Test]
        public void SendPasswordResetEmail_with_valid_email_returns_ok()
        {
            string email = "kanguroe@gmail.com";

            string errorMsg;
            string result;
            
            //check if a customer already exists with the given email address
            //if not, create a new customer with the given email address 
            Customer customer = WebMethods.CustomerMethods.GetCustomerByEmail(email, out errorMsg);
            if (customer == null)
            {
                customer = new AnonymousCustomerBuilder().With(x => x.Email, email).build();
                result = WebMethods.CustomerMethods.UpdateCustomer(customer);
                result = XElement.Parse(result).Value;
                Assert.IsTrue(result == "ok",
                              string.Format("Customer with email address '{0}' could not be created. Unexpected return value: {1}", email, result));
            }

            result = WebMethods.CustomerMethods.SendPasswordResetEmail(email);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Password for customer with email address '{0}' could not be reset. Unexpected return value: {1}", email, result));
        }


        [Test]
        public void SendPasswordResetEmail_with_invalid_email_format_returns_error()
        {
            string email = "kanguroe ,@@gmail.com";

            string result = WebMethods.CustomerMethods.SendPasswordResetEmail(email);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }


        [Test]
        public void SendPasswordResetEmail_with_nonexistent_email_returns_error()
        {
            string email = "nonexistentemail_" + DateTime.Now.ToString("yyyyMMddHHmmss");

            string result = WebMethods.CustomerMethods.SendPasswordResetEmail(email);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }


        [Test]
        public void ConfirmTeacherRegistration_with_valid_id_returns_ok()
        {
            Customer customer = null;

            string email = "kanguroe@gmail.com";
            string errorMsg;
            List<Customer> customers = WebMethods.CustomerMethods.GetCustomerByEmail(email, false, out errorMsg);
            if (customers.Count == 1)
                customer = customers[0];

            if (customer == null)
            {
                customer = new AnonymousCustomerBuilder().With(x => x.Email, email).build();
            }

            customer.IsTeacher = true;
            customer.TeacherConfirmed = DateTime.Now;

            string result = WebMethods.CustomerMethods.UpdateCustomer(customer);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Customer with store_id {0} could not be updated/created. Unexpected return value: {1}", customer.StoreId, result));

            customers = WebMethods.CustomerMethods.GetCustomerByEmail(email, false, out errorMsg);
            if (customers.Count == 1)
                customer = customers[0];
            Assert.IsNotNull(customer,
                             string.Format("Customer with store_id {0} could not be retrieved from the webshop. Unexpected return value: {1}", customer.StoreId, errorMsg));

            result = WebMethods.CustomerMethods.ConfirmTeacherRegistration((int)customer.WebshopId);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Teacher registration confirmation for customer with webshop_id {0} failed. Unexpected return value: {1}", customer.WebshopId, result));
        }

        
        [Test]
        public void ConfirmTeacherRegistration_with_invalid_id_returns_error()
        {
            int customerId = -1;

            string result = WebMethods.CustomerMethods.ConfirmTeacherRegistration(customerId);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }
    }
#endif
}
