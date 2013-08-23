using System.IO;
using System.Xml.Linq;


namespace SMS2WS_SyncAgent
{
    public class ProductPicture
    {
        public string FilePath { get; set; }
        public bool Test { get; set; }
        private string _fileName;
        public bool ToBeUploaded;

        public string FileName
        {
            get
            {
                if (_fileName != null)
                    return _fileName;
                else
                {
                    return Path.GetFileName(FilePath);
                }
            }

            set
            {
                _fileName = value;
            }
        }

        /// <summary>
        /// Returns a System.String containing an Xml representation of the current object
        /// </summary>
        /// <returns></returns>
        public string ToXml()
        {
            string xmlString;

            var xml = new XElement("product_picture",
                        new XElement("picture_file_name", FileName),
                        new XElement("test", Test.ToInt())
                      );

            xmlString = xml.ToString();
            return xmlString;
        }
    }
}
