using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static class OrderRepository
    {
        internal static Order LoadOrderFromXml(string xmlString)
        {
            var order = new Order();
            return order;
        }


        internal static List<Order> LoadOrdersFromXml(string xmlString)
        {
            XElement xml = XElement.Parse(xmlString);

            return xml.Nodes().Select(node => LoadOrderFromXml(node.ToString())).ToList();
        }
    }
}
