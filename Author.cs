using System;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class Author : ISyncObject
    {
        private const string _objectName = "author";
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Test { get; set; }
        private DateTime? _createdDttm;
        private DateTime? _updatedDttm;
        private DateTime? _deletedDttm;

        public string ObjectName
        {
            get { return _objectName; }
        }

        public DateTime? CreatedDttm
        {
            get
            {
                return _createdDttm;
            }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _createdDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? UpdatedDttm
        {
            get
            {
                return _updatedDttm;
            }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _updatedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? DeletedDttm
        {
            get
            {
                return _deletedDttm;
            }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _deletedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }


        /// <summary>
        /// Converts the current object to an Xml representation
        /// </summary>
        /// <returns>Returns a System.String containing an Xml representation of the current object</returns>
        public string ToXml()
        {
            var xml = new XElement(_objectName,
                        new XElement("id", Id),
                        Name != null ? new XElement("name", Name) : null,
                        Test == true ? new XElement("test", Test.ToInt()) : null,
                        !CreatedDttm.IsNullOrDefault() ? new XElement("created", ((DateTime)CreatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !UpdatedDttm.IsNullOrDefault() ? new XElement("updated", ((DateTime)UpdatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !DeletedDttm.IsNullOrDefault() ? new XElement("deleted", ((DateTime)DeletedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null
                      );
   
            return xml.ToString();
        }


        public bool SetSyncStatus(bool status)
        {
            return AuthorRepository.SetAuthorSyncStatus(this.Id, status);
        }


        public static string ValidateXmlStructure(string xml)
        {
            const string xsdFileName = "Author.xsd";
            string validationError = Utility.ValidateXmlStructure(xml, xsdFileName);

            return validationError;
        }
    }
}
