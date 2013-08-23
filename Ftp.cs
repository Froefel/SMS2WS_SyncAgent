using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SMS2WS_SyncAgent
{
    internal static class Ftp
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        public static bool UploadProductPictures(List<ProductPicture> productPictures)
        {
            try
            {
                if (productPictures.Count == 0)
                    return true;

                using (Stream stream = new MemoryStream())
                {
                    stream.Position = 0;

                    // fill the stream
                    foreach (ProductPicture pic in productPictures)
                    {
                        if (pic.ToBeUploaded)
                        {
                            bool result = UploadFile(stream, pic.FilePath, pic.FileName, "products");
                            if (!result)
                            {
                                string msg = String.Format("Could not upload product picture {0}", pic.FileName);
                                Console.WriteLine(msg);
                                log.Error(msg);                                
                            }
                        }
                    }
                }
                return true;
            }

            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }


        private static bool UploadFile(Stream stream, string filePath, string fileName, string ftp_dir)
        {
            stream.Seek(0, SeekOrigin.Begin);

            try
            {
                var fileInfo = new FileInfo(filePath);
                string uri = String.Format("ftp://{0}/{1}/{2}", AppSettings.FtpHost,
                                                                ftp_dir,
                                                                fileName);
                var requestFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                //Provide the WebPermission Credentials
                requestFTP.Credentials = new NetworkCredential(AppSettings.FtpLogonImages, AppSettings.FtpPasswordImages);
                //By default KeepAlive is true, where the control connection is not closed after a command is executed
                requestFTP.KeepAlive = false;
                //Specify the data transfer type.
                requestFTP.UseBinary = true;
                //Specify the command to be executed
                requestFTP.Method = WebRequestMethods.Ftp.UploadFile;
                //Notify the server about the size of the uploaded file
                requestFTP.ContentLength = fileInfo.Length;
                //The buffer size is set to 2kb
                int bufferLength = 2048;
                byte[] buffer = new byte[bufferLength];

                FileStream fStream = fileInfo.OpenRead();
                Stream uploadStream = requestFTP.GetRequestStream();
                int contentLength = fStream.Read(buffer, 0, bufferLength);

                while (contentLength != 0)
                {
                    uploadStream.Write(buffer, 0, contentLength);
                    contentLength = fStream.Read(buffer, 0, bufferLength);
                }
                uploadStream.Close();
                fStream.Close();

                string msg = String.Format("Product picture uploaded: {0} ({1} bytes)", fileName, fileInfo.Length);
                Console.WriteLine(msg);
                log.Info(msg);
                
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }
    }
}
