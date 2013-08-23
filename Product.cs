using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace SMS2WS_SyncAgent
{
    public class Product : ISyncObject
    {
        private const string _objectName = "product";
        private int _id;
        private Enums.ProductType _productTypeId;
        private Enums.TaxRate? _taxRateId;
        private string _name_NL;
        private string _name_EN;
        private string _subtitle_NL;
        private List<Song> _songs;
        private int? _authorId;
        private int? _arrangerId;
        private int? _manufacturerId;
        private int? _instrumentId;
        private string _referenceNumber;
        private string _isbn;
        private string _ismn;
        private string _ean;
        private string _upc;
        private Enums.Language? _languageId;
        private int? _bindingId;
        private string _gradeLevel;
        private int? _seriesId;
        private int? _pages;
        private decimal? _salesPrice;
        private string _description_NL;
        private decimal? _weight;
        private decimal? _length;
        private decimal? _width;
        private decimal? _height;
        private List<ProductCategory> _productCategories;
        private List<ProductPicture> _productPictures;
        private int? _internalStock;
        private int? _externalStock;
        private int _supplierId;
        private bool _promotion;
        private bool _highlightOnHome;
        private bool _activeInWebshop;
        private bool _bestSeller;
        private int _minimumOrderQuantity;
        private byte? _teacherDiscount;
        private byte? _resellerDiscount;
        private string _searchKeywords;
        private bool _storePickupOnly;
        private DateTime? _createdDttm;
        private DateTime? _updatedDttm;
        private DateTime? _deletedDttm;
        public bool Test { get; set; }
        public ProductLogBits LogBits { get; set; }


        // constructor
        public Product()
        {
            LogBits = new ProductLogBits();
            _songs = new List<Song>();
            _productPictures = new List<ProductPicture>();
            _productCategories = new List<ProductCategory>();
        }

        public Product(int productId): this()
        {
            Id = productId;
        }

        public string ObjectName
        {
            get { return _objectName; }
        }

        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                _productTypeId = (value < 900000) ? Enums.ProductType.Book : Enums.ProductType.NonBook;
                _taxRateId = (value < 900000) ? Enums.TaxRate.Books : Enums.TaxRate.NonBooks;
                LogBits.BitSet(Enums.Logfield.ProductId, true);
            }
        }

        public Enums.ProductType ProductTypeId
        {
            get { return _productTypeId; }
            set
            {
                _productTypeId = value;
                LogBits.BitSet(Enums.Logfield.ProductTypeId, true);
            }
        }

        public Enums.TaxRate? TaxRateId
        {
            get { return _taxRateId; }
            set
            {
                _taxRateId = value;
                LogBits.BitSet(Enums.Logfield.TaxRateId, true);
            }
        }

        public string Name_NL
        {
            get { return _name_NL; }
            set 
            { 
                _name_NL = value;
                LogBits.BitSet(Enums.Logfield.PublicProductName_NL, true);
            }
        }

        public string Name_EN
        {
            get { return _name_EN; }
            set
            {
                _name_EN = value;
                LogBits.BitSet(Enums.Logfield.PublicProductName_EN, true);
            }
        }

        public string Subtitle_NL
        {
            get { return _subtitle_NL; }
            set 
            { 
                _subtitle_NL = value;
                LogBits.BitSet(Enums.Logfield.Subtitle_NL, true);
            }
        }

        public List<Song> Songs
        {
            get { return _songs; }
            set
            {
                _songs = value;
                LogBits.BitSet(Enums.Logfield.SongSortOrder, true);
                LogBits.BitSet(Enums.Logfield.SongTitle, true);
            }
        }

        public int? AuthorId
        {
            get { return _authorId; }
            set
            {
                _authorId = value;
                LogBits.BitSet(Enums.Logfield.AuthorId, true);
            }
        }

        public int? ArrangerId
        {
            get { return _arrangerId; }
            set
            {
                _arrangerId = value;
                LogBits.BitSet(Enums.Logfield.ArrangerId, true);
            }
        }

        public int? ManufacturerId
        {
            get { return _manufacturerId; }
            set
            {
                _manufacturerId = value;
                LogBits.BitSet(Enums.Logfield.ManufacturerId, true);
            }
        }
       
        public int? InstrumentId
        {
            get { return _instrumentId; }
            set
            {
                _instrumentId = value;
                LogBits.BitSet(Enums.Logfield.InstrumentId, true);
            }
        }
       
        public string ReferenceNumber
        {
            get { return _referenceNumber; }
            set
            {
                _referenceNumber = value;
                LogBits.BitSet(Enums.Logfield.ReferenceNumber, true);
            }
        }

        public string Isbn
        {
            get { return _isbn; }
            set
            {
                _isbn = value;
                LogBits.BitSet(Enums.Logfield.Isbn, true);
            }
        }

        public string Ismn
        {
            get { return _ismn; }
            set
            {
                _ismn = value;
                LogBits.BitSet(Enums.Logfield.Ismn, true);
            }
        }

        public string Ean
        {
            get { return _ean; }
            set
            {
                _ean = value;
                LogBits.BitSet(Enums.Logfield.Ean, true);
            }
        }

        public string Upc
        {
            get { return _upc; }
            set
            {
                _upc = value;
                LogBits.BitSet(Enums.Logfield.Upc, true);
            }
        }

        public Enums.Language? LanguageId
        {
            get { return _languageId; }
            set
            {
                _languageId = value;
                LogBits.BitSet(Enums.Logfield.LanguageId, true);
            }
        }

        public int? BindingId
        {
            get { return _bindingId; }
            set
            {
                _bindingId = value;
                LogBits.BitSet(Enums.Logfield.BindingId, true);
            }
        }

        public string GradeLevel
        {
            get { return _gradeLevel; }
            set
            {
                _gradeLevel = value;
                LogBits.BitSet(Enums.Logfield.GradeLevel, true);
            }
        }

        public int? SeriesId
        {
            get { return _seriesId; }
            set
            {
                _seriesId = value;
                LogBits.BitSet(Enums.Logfield.SeriesId, true);
            }
        }

        public int? Pages
        {
            get { return _pages; }
            set
            {
                _pages = value;
                LogBits.BitSet(Enums.Logfield.Pages, true);
            }
        }

        public decimal? SalesPrice
        {
            get { return _salesPrice; }
            set
            {
                _salesPrice = value;
                LogBits.BitSet(Enums.Logfield.SalesPrice, true);
            }
        }

        public string Description_NL
        {
            get { return _description_NL; }
            set
            {
                _description_NL = value;
                LogBits.BitSet(Enums.Logfield.Description_NL, true);
            }
        }

        public decimal? Weight
        {
            get { return _weight; }
            set
            {
                _weight = value;
                LogBits.BitSet(Enums.Logfield.Weight, true);
            }
        }

        public decimal? Length
        {
            get { return _length; }
            set
            {
                _length = value;
                LogBits.BitSet(Enums.Logfield.Length, true);
            }
        }

        public decimal? Width
        {
            get { return _width; }
            set
            {
                _width = value;
                LogBits.BitSet(Enums.Logfield.Width, true);
            }
        }

        public decimal? Height
        {
            get { return _height; }
            set
            {
                _height = value;
                LogBits.BitSet(Enums.Logfield.Height, true);
            }
        }

        public List<ProductCategory> ProductCategories
        {
            get { return _productCategories; }
            set
            {
                _productCategories = value;
                LogBits.BitSet(Enums.Logfield.ProductCategory, true);
            }
        }

        public List<ProductPicture> ProductPictures
        {
            get { return _productPictures; }
            set
            {
                _productPictures = value;
                LogBits.BitSet(Enums.Logfield.ProductPictureFilename, true);
            }
        }

        public int? InternalStock
        {
            get { return _internalStock; }
            set
            {
                _internalStock = value;
                LogBits.BitSet(Enums.Logfield.InternalStock, true);
            }
        }

        public int? ExternalStock
        {
            get { return _externalStock; }
            set
            {
                _externalStock = value;
                LogBits.BitSet(Enums.Logfield.ExternalStock, true);
            }
        }

        public int SupplierId
        {
            get { return _supplierId; }
            set
            {
                _supplierId = value;
                LogBits.BitSet(Enums.Logfield.SupplierId, true);
            }
        }

        public bool Promotion
        {
            get { return _promotion; }
            set
            {
                _promotion = value;
                LogBits.BitSet(Enums.Logfield.Promotion, true);
            }
        }

        public bool HighlightOnHome
        {
            get { return _highlightOnHome; }
            set
            {
                _highlightOnHome = value;
                LogBits.BitSet(Enums.Logfield.HighlightOnHome, true);
            }
        }

        public bool ActiveInWebshop
        {
            get { return _activeInWebshop; }
            set
            {
                _activeInWebshop = value;
                LogBits.BitSet(Enums.Logfield.ActiveInWebshop, true);
            }
        }

        public bool BestSeller
        {
            get { return _bestSeller; }
            set
            {
                _bestSeller = value;
                //LogBits.BitSet(Enums.Logfield.BestSeller, true);
            }
        }

        public int MinimumOrderQuantity
        {
            get { return _minimumOrderQuantity; }
            set
            {
                _minimumOrderQuantity = value;
                LogBits.BitSet(Enums.Logfield.MinimumOrderQty, true);
            }
        }

        public byte? TeacherDiscount
        {
            get { return _teacherDiscount; }
            set
            {
                _teacherDiscount = value;
                LogBits.BitSet(Enums.Logfield.WebshopTeacherDiscount, true);
            }
        }

        public byte? ResellerDiscount
        {
            get { return _resellerDiscount; }
            set
            {
                _resellerDiscount = value;
                LogBits.BitSet(Enums.Logfield.WebshopResellerDiscount, true);
            }
        }

        public string SearchKeywords
        {
            get { return _searchKeywords; }
            set
            {
                _searchKeywords = value;
                LogBits.BitSet(Enums.Logfield.SearchKeywords, true);
            }
        }

        public bool StorePickupOnly
        {
            get { return _storePickupOnly; }
            set
            {
                _storePickupOnly = value;
                LogBits.BitSet(Enums.Logfield.StorePickupOnly, true);
            }
        }

        public DateTime? CreatedDttm
        {
            get { return _createdDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _createdDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
                LogBits.BitSet(Enums.Logfield.CreateDttm, true);
            }
        }

        public DateTime? UpdatedDttm
        {
            get { return _updatedDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _updatedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
                LogBits.BitSet(Enums.Logfield.UpdateDttm, true);
            }
        }

        public DateTime? DeletedDttm
        {
            get { return _deletedDttm; }
            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _deletedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
                LogBits.BitSet(Enums.Logfield.DeleteDttm, true);
            }
        }



        /// <summary>
        /// Returns a System.String containing an Xml representation of the specified property of the current object
        /// </summary>
        /// <param name="property">name of a specific property that is to be written out as Xml. 
        /// If this value is filled in, only the <id> node and the node for property are output.
        /// At the time of this writing, this is only used for product_pictures.
        /// </param>
        /// <returns></returns>
        public string ToXml(string property)
        {

            var xml = new XElement(_objectName);

            xml.Add(new XElement("id", Id.ToString()));

            if ((property == null || property == "product_type_id") && LogBits.BitTest(Enums.Logfield.ProductTypeId)) 
                xml.Add(new XElement("product_type_id", (int)ProductTypeId));

            if ((property == null || property == "name_nl") && Name_NL != null && LogBits.BitTest(Enums.Logfield.PublicProductName_NL))
                xml.Add(new XElement("name_nl", Name_NL));

            //if ((property == null || property == "name_en") && Name_EN != null && LogBits.BitTest(Enums.Logfield.PublicProductName_EN))
            //    xml.Add(new XElement("name_en", Name_EN));

            if ((property == null || property == "songs") && LogBits.BitTest(Enums.Logfield.SongSortOrder))
                xml.Add(new XElement("songs", from song in Songs
                                              select new XElement(XElement.Parse(song.ToXml()))));

            if ((property == null || property == "subtitle_nl") && LogBits.BitTest(Enums.Logfield.Subtitle_NL))
                xml.Add(new XElement("subtitle_nl", Subtitle_NL));

            if ((property == null || property == "author_id") && LogBits.BitTest(Enums.Logfield.AuthorId))
                xml.Add(new XElement("author_id", AuthorId.ToString()));

            if ((property == null || property == "arranger_id") && LogBits.BitTest(Enums.Logfield.ArrangerId))
                xml.Add(new XElement("arranger_id", ArrangerId.ToString()));

            if ((property == null || property == "manufacturer_id") && LogBits.BitTest(Enums.Logfield.ManufacturerId))
                xml.Add(new XElement("manufacturer_id", ManufacturerId.ToString()));

            if ((property == null || property == "instrument_id") && LogBits.BitTest(Enums.Logfield.InstrumentId))
                xml.Add(new XElement("instrument_id", InstrumentId.ToString()));

            if ((property == null || property == "sell_price") && !SalesPrice.IsNullOrZero() && LogBits.BitTest(Enums.Logfield.SalesPrice))
                xml.Add(new XElement("sell_price", SalesPrice));

            if ((property == null || property == "reference") && LogBits.BitTest(Enums.Logfield.ReferenceNumber))
                xml.Add(new XElement("reference", ReferenceNumber));

            if ((property == null || property == "isbn") && LogBits.BitTest(Enums.Logfield.Isbn))
                xml.Add(new XElement("isbn", Isbn));

            if ((property == null || property == "ismn") && LogBits.BitTest(Enums.Logfield.Ismn))
                xml.Add(new XElement("ismn", Ismn));

            if ((property == null || property == "ean") && LogBits.BitTest(Enums.Logfield.Ean))
                xml.Add(new XElement("ean", Ean));

            if ((property == null || property == "upc") && LogBits.BitTest(Enums.Logfield.Upc))
                xml.Add(new XElement("upc", Upc));

            if ((property == null || property == "language_id") && LogBits.BitTest(Enums.Logfield.LanguageId))
                xml.Add(new XElement("language_id", LanguageId != null ? ((int)LanguageId).ToString() : String.Empty));

            if ((property == null || property == "binding_id") && LogBits.BitTest(Enums.Logfield.BindingId))
                xml.Add(new XElement("binding_id", BindingId.ToString()));

            if ((property == null || property == "grade_level") && LogBits.BitTest(Enums.Logfield.GradeLevel))
                xml.Add(new XElement("grade_level", GradeLevel));

            if ((property == null || property == "product_series_id") && LogBits.BitTest(Enums.Logfield.SeriesId))
                xml.Add(new XElement("product_series_id", SeriesId.ToString()));

            if ((property == null || property == "nr_of_pages") && LogBits.BitTest(Enums.Logfield.Pages))
                xml.Add(new XElement("nr_of_pages", Pages.ToString()));

            if ((property == null || property == "tax_rate_id") && LogBits.BitTest(Enums.Logfield.TaxRateId))
                xml.Add(new XElement("tax_rate_id", ((int)TaxRateId).ToString()));

            if ((property == null || property == "product_pictures") && LogBits.BitTest(Enums.Logfield.ProductPictureFilename))
                xml.Add(new XElement("product_pictures", from picture in ProductPictures
                                                         select new XElement(XElement.Parse(picture.ToXml()))));

            if ((property == null || property == "description_nl") && LogBits.BitTest(Enums.Logfield.Description_NL))
                xml.Add(new XElement("description_nl", Description_NL));

            if ((property == null || property == "weight") && LogBits.BitTest(Enums.Logfield.Weight))
                xml.Add(new XElement("weight", Weight.ToString()));

            if ((property == null || property == "length") && LogBits.BitTest(Enums.Logfield.Length))
                xml.Add(new XElement("length", Length.ToString()));

            if ((property == null || property == "width") && LogBits.BitTest(Enums.Logfield.Width))
                xml.Add(new XElement("width", Width.ToString()));

            if ((property == null || property == "height") && LogBits.BitTest(Enums.Logfield.Height))
                xml.Add(new XElement("height", Height.ToString()));

            if ((property == null || property == "product_categories") && LogBits.BitTest(Enums.Logfield.ProductCategory))
                xml.Add(new XElement("product_categories", from category in ProductCategories
                                                           select new XElement("product_category", new XElement("id", category.Id),
                                                                                                   new XElement("test", Test.ToInt()))));
            
            if ((property == null || property == "internal_stock_qty") && LogBits.BitTest(Enums.Logfield.InternalStock))
                xml.Add(new XElement("internal_stock_qty", InternalStock.ToString()));

            if ((property == null || property == "external_stock_qty") && LogBits.BitTest(Enums.Logfield.ExternalStock))
                xml.Add(new XElement("external_stock_qty", ExternalStock != null ? ExternalStock.ToString() : "1"));

            if ((property == null || property == "supplier_id") && LogBits.BitTest(Enums.Logfield.SupplierId))
                xml.Add(new XElement("supplier_id", SupplierId.ToString()));

            if ((property == null || property == "promotion") && LogBits.BitTest(Enums.Logfield.Promotion))
                xml.Add(new XElement("promotion", Promotion.ToInt().ToString()));

            if ((property == null || property == "highlight_on_home") && LogBits.BitTest(Enums.Logfield.HighlightOnHome))
                xml.Add(new XElement("highlight_on_home", HighlightOnHome.ToInt().ToString()));

            if ((property == null || property == "available") && LogBits.BitTest(Enums.Logfield.ActiveInWebshop))
                xml.Add(new XElement("available", ActiveInWebshop.ToInt().ToString()));

            if ((property == null || property == "bestseller") && LogBits.BitTest(Enums.Logfield.BestSeller))
                xml.Add(new XElement("bestseller", BestSeller.ToInt().ToString()));

            if ((property == null || property == "minimum_order_qty") && LogBits.BitTest(Enums.Logfield.MinimumOrderQty))
                xml.Add(new XElement("minimum_order_qty", (MinimumOrderQuantity != 0) ? MinimumOrderQuantity.ToString() : String.Empty));

            if ((property == null || property == "teacher_discount") && LogBits.BitTest(Enums.Logfield.WebshopTeacherDiscount))
                xml.Add(new XElement("teacher_discount", TeacherDiscount.ToString()));

            if ((property == null || property == "reseller_discount") && LogBits.BitTest(Enums.Logfield.WebshopResellerDiscount))
                xml.Add(new XElement("reseller_discount", ResellerDiscount.ToString()));

            if ((property == null || property == "search_keywords") && LogBits.BitTest(Enums.Logfield.SearchKeywords))
                xml.Add(new XElement("search_keywords", SearchKeywords));

            if ((property == null || property == "store_pickup_only") && LogBits.BitTest(Enums.Logfield.StorePickupOnly))
                xml.Add(new XElement("store_pickup_only", StorePickupOnly.ToInt().ToString()));

            if ((property == null || property == "test"))
                xml.Add(new XElement("test", Test.ToInt()));

            if ((property == null || property == "created") && LogBits.BitTest(Enums.Logfield.CreateDttm))
                xml.Add(new XElement("created", (!CreatedDttm.IsNullOrDefault()) ? ((DateTime)CreatedDttm).ToString("yyyy-MM-dd HH:mm:ss") : String.Empty));

            if ((property == null || property == "updated") && LogBits.BitTest(Enums.Logfield.UpdateDttm))
                xml.Add(new XElement("updated", (!UpdatedDttm.IsNullOrDefault()) ? ((DateTime)UpdatedDttm).ToString("yyyy-MM-dd HH:mm:ss") : String.Empty));

            if ((property == null || property == "deleted") && LogBits.BitTest(Enums.Logfield.DeleteDttm))
                xml.Add(new XElement("deleted", (!DeletedDttm.IsNullOrDefault()) ? ((DateTime)DeletedDttm).ToString("yyyy-MM-dd HH:mm:ss") : String.Empty));

            string xmlString = xml.ToString();

            return xmlString;
        }


        /// <summary>
        /// Returns a System.String containing an Xml representation of the current object
        /// </summary>
        /// <returns></returns>
        public string ToXml()
        {
            return ToXml(null);
        }


        public bool SetSyncStatus(bool status, Dictionary<int, string> dictLogIds)
        {
            return ProductRepository.SetProductSyncStatus(this.Id, dictLogIds);
        }


        public static string ValidateXmlStructure(string xml)
        {
            const string xsdFileName = "Product.xsd";
            string validationError = Utility.ValidateXmlStructure(xml, xsdFileName);

            return validationError;
        }

    }
}
