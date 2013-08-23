using System;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class Song
    {
        public int Id { get; set; }
        public int Sequence { get; set; }
        public string Title { get; set; }
        public int? AuthorId { get; set; }
        public bool Test { get; set; }
        private DateTime? _createdDttm;

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

        
        /// <summary>
        /// Returns a System.String containing an Xml representation of the current object
        /// </summary>
        /// <returns></returns>
        public string ToXml()
        {
            string xmlString;

            var xml = new XElement("song",
                        new XElement("id", Id),
                        Sequence != 0 ? new XElement("sort_order", Sequence) : null,
                        Title != null ? new XElement("title", Title) : null,
                        !AuthorId.IsNullOrZero() ? new XElement("author_id", AuthorId) : null,
                        Test == true ? new XElement("test", Test.ToInt()) : null
                      );

            xmlString = xml.ToString();
            return xmlString;
        }
    }
}
