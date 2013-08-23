using System;
using Fluency;
using Fluency.DataGeneration;
using System.IO;

namespace SMS2WS_SyncAgent
{
    public class AnonymousProductCategoryBuilder : DynamicFluentBuilder<ProductCategory>
    {
        public AnonymousProductCategoryBuilder()
        {
            InitializeObject(false);
        }

        public AnonymousProductCategoryBuilder(bool UseValidLookupValues)
        {
            InitializeObject(UseValidLookupValues);
        }

        private void InitializeObject(bool UseValidLookupValues)
        {
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string imagePath = appPath + @"\Resources\testcategory.jpg";
            string imageFileName = Path.GetFileName(imagePath);

            var pdo = new PredefinedDataObjects();

            SetProperty(x => x.Id, ARandom.IntBetween(10000, 20000));
            SetProperty(x => x.ParentId, UseValidLookupValues ? pdo.ProductCategory.Id : ARandom.IntBetween(10000, 20000));
            SetProperty(x => x.SortOrder, ARandom.IntBetween(1, 100));
            SetProperty(x => x.Name, "Test_" + ARandom.Text(50).TrimEnd());
            SetProperty(x => x.PictureFilename, imageFileName);
            SetProperty(x => x.PictureData, Utility.LoadImageFromFile(imagePath));
            SetProperty(x => x.ProductCount, ARandom.PositiveInt());
            SetProperty(x => x.TargetUrl, ARandom.String(100));
            SetProperty(x => x.Path, ARandom.String(100));
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);
        }
    }
}
