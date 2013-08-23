using System.Collections.Generic;
using NUnit.Framework;

namespace SMS2WS_SyncAgent
{
#if DEBUG
    [TestFixture]
    class Test_Ftp
    {
        [Test]
        public void UploadProductPictures_uploads_single_picture_and_returns_true()
        {
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var productPictures = new List<ProductPicture>();
            var pic = new ProductPicture();
            pic.FilePath = appPath + @"\Resources\test_product_picture.jpg";
            pic.FileName = "xxxxxx.jpg";
            productPictures.Add(pic);

            bool result = Ftp.UploadProductPictures(productPictures);

            Assert.AreEqual(true, result);
        }
    }
#endif
}
