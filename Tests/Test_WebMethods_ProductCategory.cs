using System.Xml.Linq;
using System.Collections.Generic;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    public class Test_WebMethods_ProductCategory
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int categoryId = new PredefinedDataObjects().ProductCategory.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("productcategory",
                                                             "getById",
                                                             "id=" + categoryId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Product Category with Id {0} could not be retrieved from the webshop. Unexpected return value: {1}", categoryId, errorMsg));

            string validationError = ProductCategory.ValidateXmlStructure(xmlData);
            Assert.IsTrue(validationError == null, validationError);
        }

        
        [Test]
        public void GetProductCategoryById_with_valid_id_returns_product_category()
        {
            const int categoryId = 1; // Bladmuziek & Muziekboeken

            var expectedCategory = new ProductCategory
                {
                    Id = categoryId,
                    Name = "Bladmuziek & Muziekboeken"
                };

            string errorMsg;
            ProductCategory actualCategory = WebMethods.ProductCategoryMethods.GetProductCategoryById(categoryId, out errorMsg);

            Assert.AreEqual(expectedCategory.Id, actualCategory.Id);
            Assert.AreEqual(expectedCategory.Name, actualCategory.Name);
        }


        [Test]
        public void GetProductCategoryById_with_invalid_id_returns_null()
        {
            const int categoryId = -1;

            string errorMsg;
            ProductCategory actualCategory = WebMethods.ProductCategoryMethods.GetProductCategoryById(categoryId, out errorMsg);

            Assert.IsNull(actualCategory);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void GetAllProductCategories_returns_multiple_product_categories()
        {
            string errorMsg;
            List<ProductCategory> categories = WebMethods.ProductCategoryMethods.GetAllProductCategories(out errorMsg);

            Assert.IsNotNull(categories, 
                             string.Format("Unexpected return value: {0}", errorMsg));
            Assert.Greater(categories.Count, 1,
                           string.Format("{0} product categories were returned", categories.Count));
        }


        [Test]
        public void UpdateProductCategory_creates_new_category_with_valid_parent_id_and_returns_ok()
        {

            ProductCategory category = new AnonymousProductCategoryBuilder(true).build();

            string result = WebMethods.ProductCategoryMethods.UpdateProductCategory(category);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("ProductCategory with id {0} could not be created/updated. Unexpected return value: {1}", category.Id, result));
        }


        [Test]
        public void UpdateProductCategory_with_values_saves_all_data_correctly()
        {
            ProductCategory category = new AnonymousProductCategoryBuilder(true).build();

            //save the product category to the webshop
            string result = WebMethods.ProductCategoryMethods.UpdateProductCategory(category);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Product Category with id {0} could not be created/updated. Unexpected return value was: {1}", category.Id, result));

            //retrieve the category from the webshop
            string errorMsg;
            ProductCategory categoryFromWS = WebMethods.ProductCategoryMethods.GetProductCategoryById(category.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(category.Id, categoryFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(category.Name, categoryFromWS.Name, "The field comparison for field \"name\" failed.");
            Assert.AreEqual(category.ParentId, categoryFromWS.ParentId, "The field comparison for field \"parent_id\" failed.");
            Assert.AreEqual(category.Test, categoryFromWS.Test, "The field comparison for field \"test\" failed.");
            Assert.AreEqual(category.TargetUrl, categoryFromWS.TargetUrl, "The field comparison for field \"target_url\" failed.");
            Assert.AreEqual(category.CreatedDttm, categoryFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            Assert.AreEqual(category.UpdatedDttm, categoryFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            Assert.AreEqual(category.DeletedDttm, categoryFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void DeleteProductCategoryById_with_valid_id_returns_ok()
        {
            ProductCategory category = new AnonymousProductCategoryBuilder(true).build();

            string result = WebMethods.ProductCategoryMethods.UpdateProductCategory(category);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("ProductCategory with id {0} could not be created/updated. Unexpected return value: {1}", category.Id, result));

            result = WebMethods.ProductCategoryMethods.DeleteProductCategoryById(category.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("ProductCategory with id {0} could not be deleted. Unexpected return value: {1}", category.Id, result));
        }


        [Test]
        public void DeleteProductCategoryById_with_invalid_id_returns_error()
        {
            int categoryId = -1;
            string result = WebMethods.ProductCategoryMethods.DeleteProductCategoryById(categoryId);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }


        [Test]
        public void DeleteProductCategoryById_with_associated_products_returns_error()
        {
            //create test category
            ProductCategory category = new AnonymousProductCategoryBuilder(true).build();

            string result = WebMethods.ProductCategoryMethods.UpdateProductCategory(category);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Product Category with id {0} could not be created/updated. Unexpected return value: {1}", category.Id, result));

            //set a test product linked to the test category
            var newProduct = new AnonymousProductBuilder(true).build();
            newProduct.ProductCategories[0] = category;

            result = WebMethods.ProductMethods.UpdateProduct(newProduct);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Product with id {0} could not be created/updated. Unexpected return value: {1}", newProduct.Id, result));

            //delete the test category
            result = WebMethods.ProductCategoryMethods.DeleteProductCategoryById(category.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("ProductCategory with id {0} was deleted, even though a product was associated with it. Unexpected return value: {1}", category.Id, result));
        }


        [Test]
        public void UpdateProductCount_returns_ok()
        {
            string result = WebMethods.ProductCategoryMethods.UpdateProductCount();
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Product counts could not be updated. Unexpected return value: {0}", result));
        }
    }
#endif
}
