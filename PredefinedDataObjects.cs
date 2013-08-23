namespace SMS2WS_SyncAgent
{
    public class PredefinedDataObjects
    {
        private Author _author;
        private Instrument _instrument;
        private ProductSeries _series;
        private Manufacturer _manufacturer;
        private Binding _binding;
        private Country _country;
        private Supplier _supplier;
        private ProductCategory _productCategory;
        private Product _product;
        private Customer _customer;

        public Author Author
        {
            get
            {
                if (_author == null)
                    _author = AuthorRepository.GetAuthorById(3311);  //Mozart W.A.

                return _author;
            }
        }
        
        public Manufacturer Manufacturer
        {
            get
            {
                if (_manufacturer == null)
                    _manufacturer = ManufacturerRepository.GetManufacturerById(371);  //Hal Leonard

                return _manufacturer;
            } 
        }
    
        public Instrument Instrument
        {
            get
            {
                if (_instrument == null)
                    _instrument = InstrumentRepository.GetInstrumentById(328);  //Piano

                return _instrument;
            }
        }

        public ProductSeries ProductSeries
        {
            get
            {
                if (_series == null)
                    _series = ProductSeriesRepository.GetProductSeriesById(1);  //The Joy Of...

                return _series;
            }
        }

        public Binding Binding
        {
            get
            {
                if (_binding == null)
                    _binding = BindingRepository.GetBindingById(1);  //Stapled

                return _binding;
            }
        }

        public Country Country
        {
            get
            {
                if (_country == null)
                    _country = CountryRepository.GetCountryById(21);  //Belgium

                return _country;
            }
        }

        public Supplier Supplier
        {
            get
            {
                if (_supplier == null)
                    _supplier = SupplierRepository.GetSupplierById(113);  //MDS

                return _supplier;
            }
        }

        public ProductCategory ProductCategory
        {
            get
            {
                if (_productCategory == null)
                    _productCategory = ProductCategoryRepository.GetProductCategoryById(47);  //Piano

                return _productCategory;
            }
        }

        public Product Product(Enums.ProductType productType)
        {
            {
                if (_product == null)
                {
                    switch (productType)
                    {
                        case Enums.ProductType.Book:
                            _product = ProductRepository.GetProductById(27780);  //Vademecum - Gistelinck
                            break;

                        case Enums.ProductType.NonBook:
                            _product = ProductRepository.GetProductById(900197);  //Alhambra 1C guitar
                            break;
                    }
                }
                return _product;
            }
        }

        public Customer Customer
        {
            get
            {
                if (_customer == null)
                    _customer = CustomerRepository.GetCustomerById(27193);  //Hans De Schryver

                return _customer;
            }
        }
    }
}
