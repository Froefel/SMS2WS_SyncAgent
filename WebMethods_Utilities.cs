using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal class Utility
        {
            /// <summary>
            /// Delete all test records in a given table in the Webshop
            /// </summary>
            /// <returns>Returns a string with "ok" or an error message</returns>
                internal static string DeleteTestDataByTable(string tableName)
            {
                string result = GetDataFromWebMethod("utility",
                                                     "deleteTestDataByTable",
                                                     "table_name=" + tableName);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }


                /// <summary>
                /// Delete all test records in all tables in the Webshop
                /// </summary>
                /// <returns>Returns a string with "ok" or an error message</returns>
                internal static string DeleteTestData()
                {
                    string result = GetDataFromWebMethod("utility",
                                                         "deleteTestData");
                    //TODO: check xmlData here

                    XElement xml = XElement.Parse(result);
                    return xml.ToString();
                }
        }
    }
}
