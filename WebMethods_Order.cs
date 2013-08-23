using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class OrderMethods
        {
            /// <summary>
            /// Get an order from the Webshop
            /// </summary>
            /// <param name="customerId">Webshop Order Id of the order to be retrieved</param>
            /// <returns>Returns an Order object populated with data</returns>
            internal static Order GetOrderById(int orderId)
            {
                string xmlData = GetDataFromWebMethod("order",
                                                      "getById",
                                                      "id=" + orderId);
                string errorMsg;
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    throw new Exception(String.Format("Unable to retrieve order {0} from webshop. {1}",
                                                      orderId,
                                                      errorMsg));
                }
                else
                {
                    Order order = OrderRepository.LoadOrderFromXml(xmlData);
                    return order;
                }
            }



            /// <summary>
            /// Get a list of orders from the Webshop for a given customer
            /// </summary>
            /// <param name="customerID">Webshop Customer Id of the cutomer whose orders to retrieve</param>
            /// <returns>Returns a list of Order objects populated with data</returns>
            internal static List<Order> GetOrdersByCustomerId(int customerId)
            {
                string xmlData = GetDataFromWebMethod("order",
                                                      "getByCustomerId",
                                                      "id=" + customerId);
                List<Order> orders = OrderRepository.LoadOrdersFromXml(xmlData);

                return orders;
            }


            /// <summary>
            /// Get a list of orders from the Webshop for a given date
            /// </summary>
            /// <param name="date">Date for which to retrieve orders</param>
            /// <returns>Returns a list of Order objects populated with data</returns>
            internal static List<Order> GetOrdersByDate(DateTime date)
            {
                string xmlData = GetDataFromWebMethod("order",
                                                      "getByDate",
                                                      "date=" + date.ToString("yyyy-MM-dd"));
                List<Order> orders = OrderRepository.LoadOrdersFromXml(xmlData);

                return orders;
            }
        }
    }
}
