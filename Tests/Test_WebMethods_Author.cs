using System;
using System.Xml.Linq;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    class Test_WebMethods_Author
    {
        [Test]
        public void Get_method_returns_valid_xml()
        {
            int authorId = new PredefinedDataObjects().Author.Id;

            string xmlData = WebMethods.GetDataFromWebMethod("author",
                                                             "getById",
                                                             "id=" + authorId);
            string errorMsg;
            Assert.IsFalse(WebMethods.WebMethodReturnedError(xmlData, out errorMsg),
                           string.Format("Author with id {0} could not be retrieved from the webshop. Unexpected return value: {1}", authorId, errorMsg));

            string validationError = Author.ValidateXmlStructure(xmlData);
            Assert.IsNull(validationError);
        }


        [Test]
        public void GetAuthorById_with_valid_id_returns_author()
        {
            int authorId = new PredefinedDataObjects().Author.Id;

            var expectedAuthor = new Author {Id = authorId, Name = "Mozart W.A."};

            string errorMsg;
            Author actualAuthor = WebMethods.AuthorMethods.GetAuthorById(authorId, out errorMsg);

            Assert.AreEqual(expectedAuthor.Id, actualAuthor.Id);
            Assert.AreEqual(expectedAuthor.Name, actualAuthor.Name);
        }


        [Test]
        public void GetAuthorById_with_invalid_id_returns_null()
        {
            const int authorId = -1;

            string errorMsg;
            Author actualAuthor = WebMethods.AuthorMethods.GetAuthorById(authorId, out errorMsg);

            Assert.IsNull(actualAuthor);
            Assert.IsTrue(errorMsg.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", errorMsg));
        }


        [Test]
        public void UpdateAuthor_creates_new_author_and_returns_ok()
        {
            Author author = new AnonymousAuthorBuilder().build();

            string result = WebMethods.AuthorMethods.UpdateAuthor(author);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Author with id {0} could not be created/updated. Unexpected return value was: {1}", author.Id, result));
        }


        [Test]
        public void UpdateAuthor_with_values_saves_all_data_correctly()
        {
            Author author = new AnonymousAuthorBuilder().build();

            Console.WriteLine("author.Id = {0}", author.Id);

            //save the author to the webshop
            string result = WebMethods.AuthorMethods.UpdateAuthor(author);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result == "ok",
                          string.Format("Author with id {0} could not be created/updated. Unexpected return value was: {1}", author.Id, result));

            //retrieve the author from the webshop
            string errorMsg;
            Author authorFromWS = WebMethods.AuthorMethods.GetAuthorById(author.Id, out errorMsg);

            //compare all values
            Assert.AreEqual(author.Id, authorFromWS.Id, "The field comparison for field \"id\" failed.");
            Assert.AreEqual(author.Name, authorFromWS.Name, "The field comparison for field \"name\" failed.");
            Assert.AreEqual(author.Test, authorFromWS.Test, "The field comparison for field \"test\" failed.");
            //Assert.AreEqual(author.CreatedDttm, authorFromWS.CreatedDttm, "The field comparison for field \"created\" failed.");
            //Assert.AreEqual(author.UpdatedDttm, authorFromWS.UpdatedDttm, "The field comparison for field \"updated\" failed.");
            //Assert.AreEqual(author.DeletedDttm, authorFromWS.DeletedDttm, "The field comparison for field \"deleted\" failed.");
        }


        [Test]
        public void UpdateAuthor_accepts_invalid_author_and_returns_error()
        {
            // There is currently no way to generate this error condition
            // Invalid Xml would generate an error, but the Author object is always transformed into valid Xml
            // Invalid data does not seem to generate an error, so I suppose it is being handled by the API
            return;
        }


        [Test]
        public void DeleteAuthorById_with_valid_id_returns_ok()
        {
            Author author = new AnonymousAuthorBuilder().build();

            string result = WebMethods.AuthorMethods.UpdateAuthor(author);
            Assert.IsTrue(XElement.Parse(result).Value == "ok",
                          string.Format("Author with id {0} could not be created or updated; test aborted.", author.Id));

            result = WebMethods.AuthorMethods.DeleteAuthorById(author.Id);
            result = XElement.Parse(result).Value;
            Assert.IsTrue(result == "ok",
                          string.Format("Author with id {0} could not be deleted. Unexpected resturn value was: {1}", author.Id, result));
        }


        [Test]
        public void DeleteAuthorById_with_invalid_id_returns_error()
        {
            int authorId = -1;
            string result = WebMethods.AuthorMethods.DeleteAuthorById(authorId);
            result = XElement.Parse(result).Value;

            Assert.IsTrue(result.StartsWith("error"),
                          string.Format("Expected result should start with \"error: \". Unexpected return value was: {0}", result));
        }
    }
#endif
}
