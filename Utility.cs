using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace SMS2WS_SyncAgent
{
    internal static class Utility
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Loads an image file from disk
        /// </summary>
        /// <param name="filePath">Fully qualified file name</param>
        /// <returns>A byte array with the binary image data</returns>
        internal static byte[] LoadImageFromFile(string filePath)
        {
            Image img = Image.FromFile(filePath);
            byte[] arr;
            using (var ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                arr = ms.ToArray();
            }

            return arr;
        }


        /// <summary>
        /// Determines the SMS table name in which a given product is defined
        /// </summary>
        /// <param name="productId">The Id of the product to be evaluated</param>
        /// <returns>A string with the SMS table name in which the product is defined</returns>
        internal static string GetProductTableName(int productId)
        {
            string result;

            if ((productId > 0 && productId < 900000) ||
                (productId < 0 && productId%2 == 0))
                result = "Artikels";
            else
                result = "Artikels2";

            return result;
        }


        internal static Enums.ProductType GetProductType(int productId)
        {
            Enums.ProductType result;

            if ((productId > 0 && productId < 900000) ||
                (productId < 0 && productId%2 == 0))
                result = Enums.ProductType.Book;
            else
                result = Enums.ProductType.NonBook;

            return result;
        }


        /// <summary>
        /// Determines if an Xml element exists and if it contains an actual or logical empty value
        /// </summary>
        /// <param name="element">Xml element to be inspected</param>
        /// <returns>A boolean value indicating whether the Xml element is empty or non-existant</returns>
        internal static bool XmlElementIsEmptyOrSpecialValue(XElement element)
        {
            return XmlElementIsEmptyOrSpecialValue(element, "");
        }


        /// <summary>
        /// Determines if an Xml element exists and if it contains an actual or logical empty value
        /// </summary>
        /// <param name="element">Xml element to be inspected</param>
        /// <param name="specialValue">A value that is considered as empty. For example "0000-00-00 00:00:00" is considered an empty date</param>
        /// <returns>A boolean value indicating whether the Xml element is empty or non-existant</returns>
        internal static bool XmlElementIsEmptyOrSpecialValue(XElement element, string specialValue)
        {
            if (element != null && (element.Value != "" && element.Value != specialValue))
                return false;
            else
                return true;
        }


        /// <summary>
        /// Get last primary key value in a given table
        /// </summary>
        /// <param name="tableName">Table Name</param>
        /// <param name="fieldName">Name of the field that is the primary key in tableName. 
        ///                         Note that this method does not work for tables with a composite primary key
        /// </param>
        internal static T GetLastPrimaryKeyValueInTable<T>(string tableName, string fieldName)
        {
            T result;

            //create and open connection
            using (OleDbConnection conn = DAL.GetConnection())
            {
                //create command
                OleDbCommand cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format(@"select max([{0}]) from [{1}]", fieldName, tableName);

                //execute command
                result = (T) cmd.ExecuteScalar();
            }

            return result;
        }


        internal static string ValidateXmlStructure(string xml, string xsdFileName)
        {
            string validationError;

            try
            {
                string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string xsdFilePath = appPath + @"\Resources\" + xsdFileName;
                var settings = new XmlReaderSettings();
                settings.Schemas.Add(null, xsdFilePath);
                settings.ValidationType = ValidationType.Schema;
                var document = new XmlDocument();
                document.LoadXml(xml);
                XmlReader rdr = XmlReader.Create(new StringReader(document.InnerXml), settings);
                while (rdr.Read())
                {
                }
                validationError = null;
            }
            catch (Exception e)
            {
                validationError = e.Message;
            }

            return validationError;
        }

        /// <summary>
        /// Resizes each dimension of a two-dimensional array
        /// </summary>
        /// <typeparam name="t">type of first dimension</typeparam>
        /// <typeparam name="T">type of second dimension</typeparam>
        /// <param name="original">original array to be resized</param>
        /// <param name="x">number of columns in the new array</param>
        /// <param name="y">number of rows in the new array</param>
        /// <returns></returns>
        internal static T[,] ResizeArray<t, T>(T[,] original, int x, int y)
        {
            T[,] newArray = new T[x,y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minY; ++i)
                Array.Copy(original, i*original.GetLength(0), newArray, i*newArray.GetLength(0), minX);

            return newArray;
        }

        internal static string FilterString(string sourceString)
        {
            var dictReplace = new Dictionary<int, string>();
            var validChars = new List<string>();

            //build the dictionary with character replacements
            for (int i = 32; i <= 255; i++)
            {
                switch (i)
                {
                    case 192:
                    case 193:
                    case 194:
                    case 195:
                    case 196:
                    case 197:
                    case 198:
                    {
                        dictReplace.Add(i, "A");
                        break;
                    }
                    case 199:
                    {
                        dictReplace.Add(i, "C");
                        break;
                    }
                    case 200:
                    case 201:
                    case 202:
                    case 203:
                    {
                        dictReplace.Add(i, "E");
                        break;
                    }
                    case 204:
                    case 205:
                    case 206:
                    case 207:
                    {
                        dictReplace.Add(i, "I");
                        break;
                    }
                    case 208:
                    {
                        dictReplace.Add(i, "D");
                        break;
                    }
                    case 209:
                    {
                        dictReplace.Add(i, "N");
                        break;
                    }
                    case 210:
                    case 211:
                    case 212:
                    case 213:
                    case 214:
                    case 216:
                    {
                        dictReplace.Add(i, "O");
                        break;
                    }
                    case 217:
                    case 218:
                    case 219:
                    case 220:
                    {
                        dictReplace.Add(i, "U");
                        break;
                    }
                    case 221:
                    {
                        dictReplace.Add(i, "Y");
                        break;
                    }
                    case 223:
                    {
                        dictReplace.Add(i, "SS");
                        break;
                    }
                    case 224:
                    case 225:
                    case 226:
                    case 227:
                    case 228:
                    case 229:
                    case 230:
                    {
                        dictReplace.Add(i, "a");
                        break;
                    }
                    case 231:
                    {
                        dictReplace.Add(i, "c");
                        break;
                    }
                    case 232:
                    case 233:
                    case 234:
                    case 235:
                    {
                        dictReplace.Add(i, "e");
                        break;
                    }
                    case 236:
                    case 237:
                    case 238:
                    case 239:
                    {
                        dictReplace.Add(i, "i");
                        break;
                    }
                    case 241:
                    {
                        dictReplace.Add(i, "n");
                        break;
                    }
                    case 242:
                    case 243:
                    case 244:
                    case 245:
                    case 246:
                    case 248:
                    {
                        dictReplace.Add(i, "o");
                        break;
                    }
                    case 249:
                    case 250:
                    case 251:
                    case 252:
                    {
                        dictReplace.Add(i, "u");
                        break;
                    }
                    case 253:
                    case 255:
                    {
                        dictReplace.Add(i, "y");
                        break;
                    }
                }
            }

            //create string with valid characters
            //uppercase characters
            for (int i = 65; i <= 90; i++)
            {
                validChars.Add(Char.ConvertFromUtf32(i));
            }
            //lowercase characters
            for (int i = 97; i <= 122; i++)
            {
                validChars.Add(Char.ConvertFromUtf32(i));
            }
            //digits
            for (int i = 0; i <= 9; i++)
            {
                validChars.Add(Char.ConvertFromUtf32(i));
            }

            //apply the filter to the original string 
            string result = "";
            foreach (char character in sourceString)
            {
                string addCharacter = character.ToString();

                if (dictReplace.ContainsKey(character))
                    addCharacter = dictReplace[character];

                if (validChars.Contains(addCharacter))
                    result += addCharacter;
            }

            return result;
        }


        public static bool IsConnectedToInternet()
        {
            const string destinationUri = "http://www.animatomusic.be";
            HttpStatusCode statusCode = HttpStatusCode.Unused;
            bool connected = false;
            const int maxNumberOfRetries = 5;
            int numberOfRetries = 0;

            var sw = new Stopwatch();
            sw.Start();

            Console.Write("Checking internet connection to {0}", destinationUri);

            while (!connected)
            {
                try
                {
                    var hwebRequest = (HttpWebRequest)WebRequest.Create(destinationUri);
                    hwebRequest.Timeout = 5000;

                    using (var hWebResponse = (HttpWebResponse)hwebRequest.GetResponse())
                    {
                        statusCode = hWebResponse.StatusCode;
                        connected = true;
                    }
                }
                catch (Exception retryConnectException)
                {
                    if (numberOfRetries == maxNumberOfRetries)
                    {
                        log.Error(retryConnectException.ToString());
                        break;
                    }
                    numberOfRetries++;
                    //wait 0.5 second between retries
                    System.Threading.Thread.Sleep(500);
                }
            }

            sw.Stop();
            string msg = String.Format(connected ? "\rChecking internet connection to {0} ({1} sec, {2} retries)\n"
                                                 : "\rChecking internet connection to {0} failed! ({1} sec, {2} retries)\n",
                                       destinationUri,
                                       Math.Round(sw.ElapsedMilliseconds/1000d, 2),
                                       numberOfRetries);
            log.Debug(msg);
            Console.Write(msg);

            return (statusCode == HttpStatusCode.OK);
        }


        internal static string GetExceptionWithMethodSignatureDetails(MethodBase method, Exception ex, params object[] values)
        {
            ParameterInfo[] parms = method.GetParameters();
            object[] namevalues = new object[2*parms.Length];

            string result = "Error in " + method.Name + "(";
            for (int i = 0, j = 0; i < parms.Length; i++, j += 2)
            {
                result += "{" + j + "}={" + (j + 1) + "}, ";
                namevalues[j] = parms[i].Name;
                if (i < values.Length) namevalues[j + 1] = values[i];
            }
            result += "exception=" + ex.Message + ")";
            result = string.Format(result, namevalues);

            return result;
        }


        internal static void UpdatePresence(string message)
        {
            using (OleDbConnection conn = DAL.GetConnection())
            {
                if (conn != null)
                {
                    //create command
                    var cmd = conn.CreateCommand();

                    string sql = "update Settings_Shared " +
                                 "set Waarde = @value " +
                                 "where Description = @key";
                    cmd.Parameters.AddWithValue("@value", AppSettings.ApplicationVersion + ", " +
                                                          DateTime.Now.ToString() + ", " +
                                                          message);
                    cmd.Parameters.AddWithValue("@key", "SyncAgentLastCheckin");

                    cmd.CommandText = sql;

                    //execute the command
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}
