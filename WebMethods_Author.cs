using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class AuthorMethods
        {
            /// <summary>
            /// Get an author from the Webshop
            /// </summary>
            /// <param name="authorId">Id of the author to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns an Author object populated with data</returns>
            internal static Author GetAuthorById(int authorId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("author",
                                                      "getById",
                                                      "id=" + authorId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Author author = AuthorRepository.LoadAuthorFromXml(xmlData);
                    return author;
                }
            }

            /// <summary>
            /// Update an author in the webshop
            /// </summary>
            /// <param name="author">The author to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateAuthor(Author author)
            {
                string data = author.ToXml();
                string result = SendDataThroughWebMethod("author",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

            /// <summary>
            /// Delete an author from the webshop
            /// </summary>
            /// <param name="authorId">Id of the author to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteAuthorById(int authorId)
            {
                string result = GetDataFromWebMethod("author",
                                                     "deleteById",
                                                     "id=" + authorId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

        }
    }
}
