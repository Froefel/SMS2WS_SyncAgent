using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class Country
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Iso2Code { get; set; }
        public bool IsEuropean { get; set; }

        /// <summary>
        /// Converts the current object to an Xml representation
        /// </summary>
        /// <returns>Returns a System.String containing an Xml representation of the current object</returns>
        public string ToXml()
        {
            var xml = new XElement("country",
                        new XElement("id", Id),
                        new XElement("name", Name),
                        new XElement("iso1_code", Iso2Code),
                        new XElement("european", IsEuropean)
                      );

            return xml.ToString();
        }

        public static string ValidateXmlStructure(string xml)
        {
            const string xsdFileName = "Country.xsd";
            string validationError = Utility.ValidateXmlStructure(xml, xsdFileName);

            return validationError;
        }
    }
}
