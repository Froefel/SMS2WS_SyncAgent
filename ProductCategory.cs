using System;
using System.Configuration;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class ProductCategory : ISyncObject
    {
        private const string _objectName = "product_category";
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }
        private byte[] _pictureData;
        public string PictureFilename { get; set; }
        public int SortOrder { get; set; }
        public int ProductCount { get; set; }
        public string TargetUrl { get; set; }
        public string Path { get; set; }
        public bool PictureUpdated { get; set; }
        public bool Test { get; set; }
        private DateTime? _createdDttm;
        private DateTime? _updatedDttm;
        private DateTime? _deletedDttm;


        public string ObjectName
        {
            get { return _objectName; }
        }

        public byte[] PictureData
        {
            get
            {
                if (!Test && !string.IsNullOrEmpty(PictureFilename))
                {
                    string picturePath = ConfigurationManager.AppSettings["ProductCategoryPicturesPath"] + @"\" + PictureFilename;
                    _pictureData = Utility.LoadImageFromFile(picturePath);
                }
                return _pictureData;
            }

            set { _pictureData = value; }
        }

        public DateTime? CreatedDttm
        {
            get { return _createdDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _createdDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? UpdatedDttm
        {
            get { return _updatedDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _updatedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? DeletedDttm
        {
            get { return _deletedDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _deletedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        
        /// <summary>
        /// Returns a System.String containing an Xml representation of the current object
        /// </summary>
        /// <returns></returns>
        public string ToXml()
        {
            string pictureDataBase64Encoded;

            if (PictureUpdated && PictureData != null && PictureData.Length > 0)
                pictureDataBase64Encoded = Convert.ToBase64String(PictureData);
            else
                pictureDataBase64Encoded = null;

            var xml = new XElement(_objectName,
                        Id != 0 ? new XElement("id", Id.ToString()) : null,
                        ParentId != 0 ? new XElement("parent_id", ParentId.ToString()) : null,
                        Name != null ? new XElement("name_nl", Name) : null,
                        SortOrder != 0 ? new XElement("sort_order", SortOrder.ToString()) : null,
                        PictureFilename != null ? new XElement("picture_file_name", PictureFilename) : null,
                        pictureDataBase64Encoded != null ? new XElement("picture_data", pictureDataBase64Encoded) : null,
                        TargetUrl != null ? new XElement("target_url", TargetUrl) : null,
                        Test == true ? new XElement("test", Test.ToInt()) : null,
                        !CreatedDttm.IsNullOrDefault() ? new XElement("created", ((DateTime)CreatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !UpdatedDttm.IsNullOrDefault() ? new XElement("updated", ((DateTime)UpdatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !DeletedDttm.IsNullOrDefault() ? new XElement("deleted", ((DateTime)DeletedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null
                      );

            return xml.ToString();
        }


        public bool SetSyncStatus(bool status)
        {
            return ProductCategoryRepository.SetProductCategorySyncStatus(this.Id, status);
        }


        public static string ValidateXmlStructure(string xml)
        {
            const string xsdFileName = "ProductCategory.xsd";
            string validationError = Utility.ValidateXmlStructure(xml, xsdFileName);

            return validationError;
        }
    }
}
