using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal static partial class WebMethods
    {
        internal static class BindingMethods
        {
            /// <summary>
            /// Get a binding from the Webshop
            /// </summary>
            /// <param name="bindingId">Id of the binding to be retrieved</param>
            /// <param name="errorMsg" type="output">Error message returned by the web method</param>
            /// <returns>Returns a Binding object populated with data</returns>
            internal static Binding GetBindingById(int bindingId, out string errorMsg)
            {
                string xmlData = GetDataFromWebMethod("binding",
                                                      "getById",
                                                      "id=" + bindingId);
                if (WebMethodReturnedError(xmlData, out errorMsg))
                {
                    return null;
                }
                else
                {
                    errorMsg = null;
                    Binding binding = BindingRepository.LoadBindingFromXml(xmlData);
                    return binding;
                }
            }

            /// <summary>
            /// Update a binding in the webshop
            /// </summary>
            /// <param name="binding">The binding to be updated</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string UpdateBinding(Binding binding)
            {
                string data = binding.ToXml();
                string result = SendDataThroughWebMethod("binding",
                                                         "update",
                                                         null,
                                                         data);
                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

            /// <summary>
            /// Delete a binding from the webshop
            /// </summary>
            /// <param name="bindingId">Id of the binding to be deleted</param>
            /// <returns>Returns a string with "ok" or an error message</returns>
            internal static string DeleteBindingById(int bindingId)
            {
                string result = GetDataFromWebMethod("binding",
                                                     "deleteById",
                                                     "id=" + bindingId);
                //TODO: check xmlData here

                XElement xml = XElement.Parse(result);
                return xml.ToString();
            }

        }
    }
}
