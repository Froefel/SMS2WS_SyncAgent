using System;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    public class Test_CustomerRepository
    {
        [Test]
        public void GetCustomerById_with_valid_id_returns_customer()
        {
            int customerId = 27193; // Hans De Schryver
            Customer customer = CustomerRepository.GetCustomerById(customerId);
            Assert.AreEqual(customerId, customer.StoreId);
        }


        [Test]
        public void UpdateCustomer_with_new_customer_returns_success()
        {
            Customer customer = new AnonymousCustomerBuilder().build();
            customer.WebshopId = null;
            customer.UpdatedDttm = null;
            customer.CreatedDttm = DateTime.Now;
            bool result = CustomerRepository.UpdateCustomer(customer);

            Assert.IsTrue(result);
        }


        //[Test]
        public void UpdateCustomer_with_existing_customer_returns_success()
        {

            int customerId = 27193; // Hans De Schryver
            Customer customer = CustomerRepository.GetCustomerById(customerId);
            //Customer customercopy = CustomerFactory.GetCustomerById(customerStoreId);

            customer.LastName = "De Schryver" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            customer.FirstName = null;
            customer.UpdatedDttm = DateTime.Now;
            bool result = CustomerRepository.UpdateCustomer(customer);

            Assert.IsTrue(result);

            //restore original object
            //result = CustomerFactory.UpdateCustomer(customercopy);
        }
    }
#endif
}
