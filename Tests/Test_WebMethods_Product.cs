using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Fluency.DataGeneration;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    public class Test_WebMethods_Product
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int productId = new PredefinedDataObjects().Product(Enums.ProductType.Book).Id;

            string xmlData = WebMethods.GetDataFromWebMethod("product",
                                                             "getById",
                                                             "id=" + productId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Product with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", productId, errorMsg));

            string validationError = Product.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }

        
        [Test]
        public void GetProductById_with_valid_id_returns_product()
        {
            int productId = new PredefinedDataObjects().Product(Enums.ProductType.NonBook).Id;

            var expectedProduct = new Product();
            expectedProduct.Id = productId;
            expectedProduct.Name_NL = "Alhambra 1C klassieke gitaar";

            string errorMsg;
            Product actualProduct = WebMethods.ProductMethods.GetProductById(productId, out errorMsg);
            Assert.AreEqual(expectedProduct.Id, actualProduct.Id);
            Assert.AreEqual(expectedProduct.Name_NL, actualProduct.Name_NL);
        }


        [Test]
        public void GetProductById_with_invalid_id_returns_error()
        {
            const int productId = -1;

            string errorMsg;
            Product product = WebMethods.ProductMethods.GetProductById(productId, out errorMsg);

            Assert.IsNull(product);
            Assert.IsTrue(errorMsg.StartsWith("error"), string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void UpdateProduct_creates_new_product_and_returns_ok()
        {
            //create new test product with valid lookup values
            var newProduct = new AnonymousProductBuilder(true).build();
            //newProduct.InstrumentId = new PredefinedDataObjects().Instrument.Id;

            string result = WebMethods.ProductMethods.UpdateProduct(newProduct);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok", string.Format("Product with id {0} could not be created/updated. Unexpected return value: {1}", newProduct.Id, result));
        }


        [Test]
        public void UpdateProduct_with_values_saves_all_data_correctly()
        {
            Product product = new AnonymousProductBuilder(true).build();

            //modify the product pictures to be valid
            for (int i = 0; i < product.ProductPictures.Count; i++)
            {
                if (i == 0)
                    product.ProductPictures[i].FileName = String.Format("{0}.jpg", product.Id.ToString("D6"));
                else
                    product.ProductPictures[i].FileName = String.Format("{0}_{1}.jpg", product.Id.ToString("D6"), i.ToString("D2"));
            }

            //now upload product pictures to FTP server
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var productPictures = new List<ProductPicture>();
            foreach (ProductPicture pic in product.ProductPictures)
                productPictures.Add(new ProductPicture { FilePath = appPath + @"\Resources\test_product_picture.jpg", 
                                                         FileName = pic.FileName,
                                                         ToBeUploaded = true } );

            bool bResult = Ftp.UploadProductPictures(productPictures);

            Assert.AreEqual(true, bResult, "failed to upload product pictures.");


            //save the product to the webshop
            string result = WebMethods.ProductMethods.UpdateProduct(product);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok", string.Format("Product with id {0} could not be created/updated. Unexpected return value was: {1}", product.Id, result));
                
            //retrieve the product from the webshop
            string errorMsg;
            Product productFromWS = WebMethods.ProductMethods.GetProductById(product.Id, out errorMsg);
            Assert.IsNotNull(productFromWS,
                             string.Format("Could not retrieve product with id {0} from the webshop. Unexpected return value was: {1}", product.Id, errorMsg));              

            //compare all values
            Assert.AreEqual(product.Id, productFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(product.ProductTypeId, productFromWS.ProductTypeId, "The field comparison for field \"product_type_id\" failed.");
            Assert.AreEqual(product.Name_NL, productFromWS.Name_NL, "The field comparison for field \"name_nl\" failed.");
            Assert.AreEqual(product.Subtitle_NL, productFromWS.Subtitle_NL, "The field comparison for field \"subtitle_nl\" failed.");
            Assert.AreEqual(product.AuthorId, productFromWS.AuthorId, "The field comparison for field \"author_id\" failed.");
            Assert.AreEqual(product.ArrangerId, productFromWS.ArrangerId);
            Assert.AreEqual(product.ManufacturerId, productFromWS.ManufacturerId, "The field comparison for field \"manufacturer_id\" failed.");
            Assert.AreEqual(product.InstrumentId, productFromWS.InstrumentId, "The field comparison for field \"instrument_id\" failed.");
            Assert.AreEqual(product.ReferenceNumber, productFromWS.ReferenceNumber, "The field comparison for field \"reference\" failed.");
            Assert.AreEqual(product.Isbn, productFromWS.Isbn, "The field comparison for field \"isbn\" failed.");
            Assert.AreEqual(product.Ismn, productFromWS.Ismn, "The field comparison for field \"ismn\" failed.");
            Assert.AreEqual(product.Ean, productFromWS.Ean, "The field comparison for field \"ean\" failed.");
            Assert.AreEqual(product.Upc, productFromWS.Upc, "The field comparison for field \"upc\" failed.");
            Assert.AreEqual(product.LanguageId, productFromWS.LanguageId, "The field comparison for field \"language_id\" failed.");
            Assert.AreEqual(product.BindingId, productFromWS.BindingId, "The field comparison for field \"binding_id\" failed.");
            Assert.AreEqual(product.GradeLevel, productFromWS.GradeLevel, "The field comparison for field \"grade_level\" failed.");
            Assert.AreEqual(product.SeriesId, productFromWS.SeriesId, "The field comparison for field \"product_series_id\" failed.");
            Assert.AreEqual(product.Pages, productFromWS.Pages, "The field comparison for field \"nr_of_pages\" failed.");
            Assert.AreEqual(product.SalesPrice, productFromWS.SalesPrice, "The field comparison for field \"sales_price\" failed.");
            Assert.AreEqual(product.TaxRateId, productFromWS.TaxRateId, "The field comparison for field \"tax_rate_id\" failed.");
            Assert.AreEqual(product.Description_NL, productFromWS.Description_NL, "The field comparison for field \"description_nl\" failed.");
            Assert.AreEqual(product.Weight, productFromWS.Weight, "The field comparison for field \"weight\" failed.");
            Assert.AreEqual(product.Length, productFromWS.Length, "The field comparison for field \"length\" failed.");
            Assert.AreEqual(product.Width, productFromWS.Width, "The field comparison for field \"width\" failed.");
            Assert.AreEqual(product.Height, productFromWS.Height, "The field comparison for field \"height\" failed.");
            Assert.AreEqual(product.InternalStock, productFromWS.InternalStock, "The field comparison for field \"internal_stock\" failed.");
            Assert.AreEqual(product.ExternalStock, productFromWS.ExternalStock, "The field comparison for field \"external_stok\" failed.");
            Assert.AreEqual(product.SupplierId, productFromWS.SupplierId, "The field comparison for field \"supplier_id\" failed.");
            Assert.AreEqual(product.Promotion, productFromWS.Promotion, "The field comparison for field \"promotion\" failed.");
            Assert.AreEqual(product.HighlightOnHome, productFromWS.HighlightOnHome, "The field comparison for field \"highlight_on_home\" failed.");
            Assert.AreEqual(product.BestSeller, productFromWS.BestSeller, "The field comparison for field \"bestseller\" failed.");
            Assert.AreEqual(product.MinimumOrderQuantity, productFromWS.MinimumOrderQuantity);
            Assert.AreEqual(product.TeacherDiscount, productFromWS.TeacherDiscount, "The field comparison for field \"teacher_discount\" failed.");
            Assert.AreEqual(product.ResellerDiscount, productFromWS.ResellerDiscount, "The field comparison for field \"reseller_discount\" failed.");
            Assert.AreEqual(product.SearchKeywords, productFromWS.SearchKeywords, "The field comparison for field \"keywords_nl\" failed.");
            Assert.AreEqual(product.StorePickupOnly, productFromWS.StorePickupOnly, "The field comparison for field \"store_pickup_only\" failed.");
            Assert.AreEqual(product.Test, productFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(product.CreatedDttm, productFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(product.UpdatedDttm, productFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(product.DeletedDttm, productFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");

            //compare lists
            Assert.AreEqual(product.Songs.Count, productFromWS.Songs.Count, "The field comparison for field \"songs\" failed.");
            Assert.AreEqual(product.ProductPictures.Count, productFromWS.ProductPictures.Count, "The field comparison for field \"product_pictures\" failed.");
            Assert.AreEqual(product.ProductCategories.Count, productFromWS.ProductCategories.Count, "The field comparison for field \"product_categories\" failed.");
            
            //compare song list contents
            foreach (Song song in product.Songs)
            {
                Song songFromWS = productFromWS.Songs.Find(delegate(Song s) { return s.Id == song.Id; });
                Assert.IsNotNull(songFromWS, string.Format("Song with id {0} could not be found.", song.Id));
                Assert.AreEqual(song.Id, songFromWS.Id);
                Assert.AreEqual(song.Title, songFromWS.Title, "The field comparison for field \"song.title\" failed.");
                Assert.AreEqual(song.AuthorId, songFromWS.AuthorId, "The field comparison for field \"song.author_id\" failed.");
                Assert.AreEqual(song.Sequence, songFromWS.Sequence, "The field comparison for field \"song.sort_order\" failed.");
            }

            //compare product picture list contents
            foreach (ProductPicture picture in product.ProductPictures)
            {
                ProductPicture pictureFromWS = productFromWS.ProductPictures.Find(delegate(ProductPicture p) { return p.FileName == picture.FileName; });
                Assert.IsNotNull(pictureFromWS, string.Format("Product Picture \"{0}\" could not be found.", picture.FileName));
                Assert.AreEqual(picture.FileName, pictureFromWS.FileName, "The field comparison for field \"picture.FileName\" failed.");
                Assert.AreEqual(picture.Test, pictureFromWS.Test, "The field comparison for field \"picture.Test\" failed.");
            }

            //compare product category list contents
            foreach (ProductCategory category in product.ProductCategories)
            {
                Assert.IsTrue(productFromWS.ProductCategories.Exists(delegate(ProductCategory cat) { return cat.Id == category.Id; }),
                              string.Format("Product Category with id {0} could not be found.", category.Id));
            }
        }


        [Test]
        public void UpdateProduct_with_nulls_saves_all_data_correctly()
        {
            Product product = new Product();
            product.Id = ARandom.IntBetween(1000000, 2000000);
            product.ProductTypeId = (Enums.ProductType) ARandom.IntBetween(1, 2);
            product.TaxRateId = (Enums.TaxRate) ARandom.IntBetween(1, 2);
            product.SupplierId = new PredefinedDataObjects().Supplier.Id;
            product.InternalStock = 0;
            product.ExternalStock = 1;
            product.SalesPrice = 0;
            product.ProductCategories.Add(new PredefinedDataObjects().ProductCategory);
            //the following line causes the LogBits to be set properly
            product.ProductCategories = product.ProductCategories;
            product.Test = true;
            Console.WriteLine("product.Id = {0}", product.Id);

            //save the product to the webshop
            string result = WebMethods.ProductMethods.UpdateProduct(product);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok", string.Format("Product with id {0} could not be created/updated. Unexpected return value was: {1}", product.Id, result));

            //retrieve the product from the webshop
            string errorMsg;
            Product productFromWS = WebMethods.ProductMethods.GetProductById(product.Id, out errorMsg);
            Assert.IsNotNull(productFromWS, string.Format("Could not retrieve product with id {0} from the webshop. Unexpected return value was: {1}", product.Id, errorMsg));

            //compare all values
            Assert.AreEqual(product.Id, productFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(product.ProductTypeId, productFromWS.ProductTypeId, "The field comparison for field \"product_type_id\" failed.");
            Assert.AreEqual(product.Name_NL, productFromWS.Name_NL, "The field comparison for field \"name_nl\" failed.");
            //Assert.AreEqual(product.Name_EN, productFromWS.Name_EN, "The field comparison for field \"name_en\" failed.");
            Assert.AreEqual(product.Subtitle_NL, productFromWS.Subtitle_NL, "The field comparison for field \"subtitle_nl\" failed.");
            Assert.AreEqual(product.AuthorId, productFromWS.AuthorId, "The field comparison for field \"author_id\" failed.");
            Assert.AreEqual(product.ArrangerId, productFromWS.ArrangerId);
            Assert.AreEqual(product.ManufacturerId, productFromWS.ManufacturerId, "The field comparison for field \"manufacturer_id\" failed.");
            Assert.AreEqual(product.InstrumentId, productFromWS.InstrumentId, "The field comparison for field \"instrument_id\" failed.");
            Assert.AreEqual(product.ReferenceNumber, productFromWS.ReferenceNumber, "The field comparison for field \"reference\" failed.");
            Assert.AreEqual(product.Isbn, productFromWS.Isbn, "The field comparison for field \"isbn\" failed.");
            Assert.AreEqual(product.Ismn, productFromWS.Ismn, "The field comparison for field \"ismn\" failed.");
            Assert.AreEqual(product.Ean, productFromWS.Ean, "The field comparison for field \"ean\" failed.");
            Assert.AreEqual(product.Upc, productFromWS.Upc, "The field comparison for field \"upc\" failed.");
            Assert.AreEqual(product.LanguageId, productFromWS.LanguageId, "The field comparison for field \"language_id\" failed.");
            Assert.AreEqual(product.BindingId, productFromWS.BindingId, "The field comparison for field \"binding_id\" failed.");
            Assert.AreEqual(product.GradeLevel, productFromWS.GradeLevel, "The field comparison for field \"grade_level\" failed.");
            Assert.AreEqual(product.SeriesId, productFromWS.SeriesId, "The field comparison for field \"product_series_id\" failed.");
            Assert.AreEqual(product.Pages, productFromWS.Pages, "The field comparison for field \"nr_of_pages\" failed.");
            Assert.AreEqual(product.SalesPrice, productFromWS.SalesPrice, "The field comparison for field \"sales_price\" failed.");
            Assert.AreEqual(product.TaxRateId, productFromWS.TaxRateId, "The field comparison for field \"tax_rate_id\" failed.");
            Assert.AreEqual(product.Description_NL, productFromWS.Description_NL, "The field comparison for field \"description_nl\" failed.");
            Assert.AreEqual(product.Weight, productFromWS.Weight, "The field comparison for field \"weight\" failed.");
            Assert.AreEqual(product.Length, productFromWS.Length, "The field comparison for field \"length\" failed.");
            Assert.AreEqual(product.Width, productFromWS.Width, "The field comparison for field \"width\" failed.");
            Assert.AreEqual(product.Height, productFromWS.Height, "The field comparison for field \"height\" failed.");
            Assert.AreEqual(product.InternalStock, productFromWS.InternalStock, "The field comparison for field \"internal_stock\" failed.");
            Assert.AreEqual(product.ExternalStock, productFromWS.ExternalStock, "The field comparison for field \"external_stok\" failed.");
            Assert.AreEqual(product.SupplierId, productFromWS.SupplierId, "The field comparison for field \"supplier_id\" failed.");
            Assert.AreEqual(product.Promotion, productFromWS.Promotion, "The field comparison for field \"promotion\" failed.");
            Assert.AreEqual(product.HighlightOnHome, productFromWS.HighlightOnHome, "The field comparison for field \"highlight_on_home\" failed.");
            Assert.AreEqual(product.BestSeller, productFromWS.BestSeller, "The field comparison for field \"bestseller\" failed.");
            Assert.AreEqual(product.MinimumOrderQuantity, productFromWS.MinimumOrderQuantity);
            Assert.AreEqual(product.TeacherDiscount, productFromWS.TeacherDiscount, "The field comparison for field \"teacher_discount\" failed.");
            Assert.AreEqual(product.ResellerDiscount, productFromWS.ResellerDiscount, "The field comparison for field \"reseller_discount\" failed.");
            Assert.AreEqual(product.SearchKeywords, productFromWS.SearchKeywords, "The field comparison for field \"keywords_nl\" failed.");
            Assert.AreEqual(product.StorePickupOnly, productFromWS.StorePickupOnly, "The field comparison for field \"store_pickup_only\" failed.");
            //Assert.AreEqual(product.CreatedDttm, productFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(product.UpdatedDttm, productFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(product.DeletedDttm, productFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");

            //compare lists
            Assert.AreEqual(product.Songs.Count, productFromWS.Songs.Count, "The field comparison for field \"songs\" failed.");
            Assert.AreEqual(product.ProductPictures.Count, productFromWS.ProductPictures.Count, "The field comparison for field \"product_pictures\" failed.");
            Assert.AreEqual(product.ProductCategories.Count, productFromWS.ProductCategories.Count, "The field comparison for field \"product_categories\" failed.");
        }
        
        
        [Test]
        public void DeleteProductById_with_valid_productId_returns_ok()
        {
            //create test product with valid lookup values
            var newProduct = new AnonymousProductBuilder(true).build();
            newProduct.Songs.Clear();

            string result = WebMethods.ProductMethods.UpdateProduct(newProduct);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok", string.Format("Product with id {0} could not be created/updated. Unexpected return value: {1}", newProduct.Id, result));

            //now delete the product
            result = WebMethods.ProductMethods.DeleteProductById(newProduct.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok", string.Format("Product with id {0} could not be deleted. Unexpected return value: {1}", newProduct.Id, result));
        }


        [Test]
        public void DeleteProductById_with_invalid_productId_returns_error()
        {
            int productId = -1;
            string result = WebMethods.ProductMethods.DeleteProductById(productId);
            string actual = XElement.Parse(result).Value;
            Assert.IsTrue(actual.StartsWith("error"), string.Format("Return value should start with \"error: \". Unexpected return value: {0}", actual));
        }
    }
#endif
}
