using System;
using System.Collections.Generic;
using System.Web;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class CustomerMethods
        {
            /// <summary>
            /// Get a customer from the Webshop
            /// </summary>
            /// <param name="customerId">Webshop Id of the customer to be retrieved</param>
            /// <param name="errorMsg" type="out">error message that can be returned by the method</param>
            /// <returns>Returns a Customer object populated with data</returns>
            internal static Customer GetCustomerByWebshopId(int customerId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("customer",
                                                      "getById",
                                                      "id=" + customerId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Customer customer = CustomerRepository.LoadCustomerFromXml(xmlData);
                    return customer;
                }
            }


            /// <summary>
            /// Get a customer from the webshop
            /// </summary>
            /// <param name="customerStoreId">SMS Id of the customer to be retrieved</param>
            /// <param name="errorMsg" type="out">error message that can be returned by the method</param>
            /// <returns>Returns a Customer object populated with data</returns>
            internal static Customer GetCustomerByStoreId(int customerStoreId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("customer",
                                                      "getByStoreId",
                                                      "id=" + customerStoreId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Customer customer = CustomerRepository.LoadCustomerFromXml(xmlData);
                    return customer;
                }
            }


            /// <summary>
            /// Get a customer from the webshop with a given email address
            /// </summary>
            /// <param name="email">Email address of the customer to be retrieved</param>
            /// <param name="showDeleted">Option to include deleted customers</param>
            /// <param name="errorMsg" type="out">Error message that can be returned by the method</param>
            /// <returns>Returns a Customer object populated with data</returns>
            internal static List<Customer> GetCustomerByEmail(string email, bool showDeleted, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("customer",
                                                      "getByEmail",
                                                      "email=" + HttpUtility.HtmlEncode(email) + (showDeleted ? "&show_deleted=" + showDeleted.ToInt() : ""));
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    List<Customer> customers = CustomerRepository.LoadCustomerListFromXml(xmlData);
                    return customers;
                }
            }


            /// <summary>
            /// Get a customer from the webshop with a given email address
            /// Note: deleted customers will not be included
            /// </summary>
            /// <param name="email">Email address of the customer to be retrieved</param>
            /// <param name="errorMsg" type="out">Error message that can be returned by the method</param>
            /// <returns>Returns a Customer object populated with data</returns>
            internal static Customer GetCustomerByEmail(string email, out string errorMsg)
            {
                List<Customer> customers = GetCustomerByEmail(email, false, out errorMsg);
                
                //the list should only contain 1 object, corresponding to the active customer
                if (customers.Count == 1)
                    return customers[0];
                else 
                {
                    if (customers.Count > 1)
                        errorMsg = errorMsg + "; multiple customer were returned while only 1 was expected.";
                    return null;
                }
            }


            /// <summary>
            /// Retrieve from the Webshop a list of customers that have been updated since a given timestamp
            /// </summary>
            /// <param name="timestampStart">Time stamp of the oldest record to be retrieved</param>
            /// <param name="showDeleted">Option to include deleted objects</param>
            /// <param name="errorMsg" type="out">Error message that can be returned by the method</param>
            /// <returns>Returns a list of Customer objects populated with data</returns>
            internal static List<Customer> GetCustomersUpdatedSinceDateTime(DateTime timestampStart, bool showDeleted, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("customer",
                                                      "getUpdatedByDateTime",
                                                      "datetime=" + HttpUtility.HtmlEncode(timestampStart.ToString("yyyy-MM-dd HH:mm:ss")) + (showDeleted ? "&show_deleted=" + showDeleted.ToInt() : ""));
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    List<Customer> customers = CustomerRepository.LoadCustomerListFromXml(xmlData);
                    return customers;
                }
            }


            /// <summary>
            /// Retrieve a list of customers whose data has changed or been deleted in a given time frame
            /// </summary>
            /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
            /// <param name="errorMsg" type="out">Error message that can be returned by the method</param>
            /// <returns>Returns a list of Customer objects populated with data</returns>
            internal static List<Customer> GetCustomersUpdatedSinceDateTime(DateTime timestampStart, out string errorMsg)
            {
                return GetCustomersUpdatedSinceDateTime(timestampStart, false, out errorMsg);
            }


            /// <summary>
            /// Update a customer in the webshop
            /// </summary>
            /// <param name="customer">Customer object to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateCustomer(Customer customer)
            {
                string data = customer.ToXml();
                string result = SendDataThroughWebMethod("customer",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            /// <summary>
            /// Delete a customer from the webshop
            /// </summary>
            /// <param name="customerId">Webshop Id of the customer to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteCustomerByWebshopId(int customerId)
            {
                string result = GetDataFromWebMethod("customer",
                                                     "deleteById",
                                                     "id=" + customerId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            /// <summary>
            /// Reset the password for a given email address and send a password reset email
            /// </summary>
            /// <param name="email">Email address for which the password should be reset</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string SendPasswordResetEmail(string email)
            {
                string result = GetDataFromWebMethod("customer",
                                                     "sendPasswordResetEmail",
                                                     "email=" + email);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


            internal static string ConfirmTeacherRegistration(int customerId)
            {
                string result = GetDataFromWebMethod("customer",
                                                     "confirmTeacherRegistration",
                                                     "id=" + customerId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }
        }
    }
}
