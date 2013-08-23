using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Web;
using System.Collections.Specialized;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Generic method to retrieve data via a webshop API function
        /// </summary>
        /// <param name="targetObject">The object name used in the QueryString</param>
        /// <param name="method">The method name used in the QueryString</param>
        /// <param name="arguments">Arguments for the function used in the QueryString. The arguments are in the form "&[name]=[value]"</param>
        /// <returns>A valid Xml string as returned by the web method</returns>
        internal static string GetDataFromWebMethod(string targetObject, string method, string arguments)
        {
            var uri = new Uri(AppSettings.ApiMethodBaseUri + targetObject + "?action=" + method + (arguments != null ? "&" + arguments : ""));

            WebClient wc = AuthenticatedWebClient();

            string result = HttpUtility.HtmlDecode(wc.DownloadString(uri));
            wc.Dispose();

            return PreprocessWebMethodResult(result);
        }


        private static string GetDataFromWebMethod(string targetObject, string method)
        {
            var uri = new Uri(AppSettings.ApiMethodBaseUri + targetObject + "?action=" + method);

            return GetDataFromWebMethod(targetObject, method, "");
        }


        /// <summary>
        /// Generic method to send data via a webshop API method
        /// </summary>
        /// <param name="targetObject">The object name used in the QueryString</param>
        /// <param name="method">The method name used in the QueryString</param>
        /// <param name="arguments">Arguments for the function used in the QueryString. The arguments are in the form "&[name]=[value]"</param>
        /// <returns>A valid Xml string as returned by the web method</returns>
        private static string SendDataThroughWebMethod(string targetObject, string method, string arguments, string data)
        {
            string result;

            var uri = new Uri(AppSettings.ApiMethodBaseUri + targetObject + "?action=" + method + (arguments != null ? "&" + arguments : ""));

            try
            {
                var wc = AuthenticatedWebClient();
                var formData = new NameValueCollection();
                formData["data"] = data;
                byte[] responseBytes = wc.UploadValues(uri, "POST", formData);
                result = HttpUtility.HtmlDecode(Encoding.UTF8.GetString(responseBytes));
                wc.Dispose();
            }
            catch (Exception exception)
            {
                log.Info(exception);
                Console.WriteLine(exception);
                throw;
            }

            return PreprocessWebMethodResult(result);
        }


        /// <summary>
        /// Returns a WebClient that has been pre-authenticated for subsequent requests
        /// </summary>
        /// <returns></returns>
        private static WebClient AuthenticatedWebClient()
        {
            var uri = new Uri(AppSettings.ApiBaseUri);
            var cache = new CredentialCache();
            cache.Add(uri, "Basic", new NetworkCredential(AppSettings.ApiOutputFormat, AppSettings.ApiSecurityKey));

            var wc = new WebClient();
            wc.Credentials = cache;

            return wc;
        }


        private static string PreprocessWebMethodResult(string data)
        {
            XElement xml;
            string result = data;

            //check if the result is an xml tree for one of the expected objects
            var globals = new MyGlobals();
            bool IsValidElement = false;
            foreach (string validElement in globals.ValidXmlTopNodes)
                if (result.StartsWith("<" + validElement + ">"))
                {
                    IsValidElement = true;
                    break;
                }

            if (IsValidElement)
                result = "<pre>" + result + "</pre>";
            
            else if (result.StartsWith("<div id=\"result\""))
            {
                result = result.Substring(result.IndexOf("<pre>"));
                result = result.Substring(0, result.IndexOf("</pre>") + "</pre>".Length);
            }


            if (result.StartsWith("<pre>"))
            {
                result = result.Replace("<pre>", "").Replace("</pre>", "");
                result = result.Replace(" & ", " &amp; ");

                //parse the xml text
                xml = XElement.Parse(result);

                //check if the xml top node is an expected value
                if (!globals.ValidXmlTopNodes.Contains(xml.Name.ToString()))
                {
                    result = null;
                }
            }
            else
            {
                result = "<status>" + String.Format("error: return value is not a valid xml document.\n{0}", data) + "</status>";
            }

            return result;
        }
        
        /// <summary>
        /// Determines if the data returned from a web method call contains an error message
        /// </summary>
        /// <param name="xmlData">The xml returned from a web method call</param>
        /// <returns>boolean true if the web method call contains an error message, false otherwise</returns>
        internal static bool WebMethodReturnedError(string xmlData, out string errorMsg)
        {
            try
            {
                XElement xml = XElement.Parse(xmlData);
                errorMsg = xml.Value;
                return xml.Name.ToString().Equals("status", StringComparison.InvariantCultureIgnoreCase) &&
                       xml.Value.StartsWith("error", StringComparison.InvariantCultureIgnoreCase);
            }
            catch (XmlException e)
            {
                errorMsg = "error: " + e.Message;
                return true;
            }
        }


    }
}