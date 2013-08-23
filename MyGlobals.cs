using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class MyGlobals
    {
        public readonly List<string> ValidXmlTopNodes = new List<string>();

        public MyGlobals()
        {
            GetXmlTopNodesFromSchemas();
        }


        private void GetXmlTopNodesFromSchemas()
        {
            //Process the list of .xsd files found in the directory.
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string xsdFilePath = appPath + @"\Resources\";
            string[] fileEntries = Directory.GetFiles(xsdFilePath, "*.xsd");
            foreach (string xsdFileName in fileEntries)
            {
                var xs = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
                var doc = XDocument.Load(xsdFileName);
                try
                {
                    ValidXmlTopNodes.Add(doc.Descendants(xs + "element").FirstOrDefault().Attribute("name").Value);
                }
                catch { }

                ValidXmlTopNodes.Add("status");
                ValidXmlTopNodes.Add("customers");
                ValidXmlTopNodes.Add("countries");
                ValidXmlTopNodes.Add("product_categories");

                ValidXmlTopNodes.Add("product_category");
                ValidXmlTopNodes.Add("product");
                ValidXmlTopNodes.Add("supplier");
                ValidXmlTopNodes.Add("author");
                ValidXmlTopNodes.Add("manufacturer");
                ValidXmlTopNodes.Add("instrument");
                ValidXmlTopNodes.Add("binding");
                ValidXmlTopNodes.Add("product_series");
                ValidXmlTopNodes.Add("customer");
                ValidXmlTopNodes.Add("country");
            }
        }
    }
}
