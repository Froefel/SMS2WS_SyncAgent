using System;
using System.Collections.Generic;
using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    public class AnonymousProductBuilder : DynamicFluentBuilder<Product>
    {
        public AnonymousProductBuilder()
        {
            InitializeObject(false);
        }

        public AnonymousProductBuilder(bool useValidLookupValues)
        {
            InitializeObject(useValidLookupValues);
        }

        private void InitializeObject(bool useValidLookupValues)
        {
            SetProperty(x => x.LogBits, new ProductLogBits());
            SetProperty(x => x.Id, ARandom.IntBetween(1000000, 2000000));
            SetProperty(x => x.ProductTypeId, (Enums.ProductType)ARandom.IntBetween(1, 2));
            SetProperty(x => x.ActiveInWebshop, ARandom.Boolean());
            SetProperty(x => x.Name_NL, "Test_" + ARandom.Text(200).TrimEnd());
            SetProperty(x => x.Name_EN, "Test_" + ARandom.Text(200).TrimEnd());
            SetProperty(x => x.Subtitle_NL, ARandom.Text(300).TrimEnd());
            SetProperty(x => x.ReferenceNumber, ARandom.Text(20).TrimEnd());
            SetProperty(x => x.Isbn, ARandom.String(13));
            SetProperty(x => x.Ismn, ARandom.String(13));
            SetProperty(x => x.Ean, ARandom.String(13));
            SetProperty(x => x.Upc, ARandom.String(12));
            SetProperty(x => x.GradeLevel, ARandom.String(10));
            SetProperty(x => x.Pages, ARandom.IntBetween(1, Int16.MaxValue));
            SetProperty(x => x.SalesPrice, ARandom.CurrencyAmountLessThan(100000));
            SetProperty(x => x.TaxRateId, (Enums.TaxRate)ARandom.IntBetween(1, 2));
            SetProperty(x => x.Description_NL, ARandom.Text(500).TrimEnd());
            SetProperty(x => x.Weight, ARandom.CurrencyAmountLessThan(100));
            SetProperty(x => x.Length, ARandom.CurrencyAmountLessThan(100));
            SetProperty(x => x.Width, ARandom.CurrencyAmountLessThan(100));
            SetProperty(x => x.Height, ARandom.CurrencyAmountLessThan(100));
            SetProperty(x => x.InternalStock, ARandom.PositiveInt());
            SetProperty(x => x.ExternalStock, ARandom.PositiveInt());
            SetProperty(x => x.TeacherDiscount, (byte)ARandom.IntBetween(0, 100));
            SetProperty(x => x.ResellerDiscount, (byte)ARandom.IntBetween(0, 100));
            SetProperty(x => x.Promotion, ARandom.Boolean());
            SetProperty(x => x.HighlightOnHome, ARandom.Boolean());
            //TODO: set Besteller property to ARandom.Boolean() when BestSeller is properly supported
            SetProperty(x => x.BestSeller, false);
            SetProperty(x => x.MinimumOrderQuantity, ARandom.IntBetween(1, 500));
            SetProperty(x => x.SearchKeywords, ARandom.Text(200));
            SetProperty(x => x.StorePickupOnly, ARandom.Boolean());
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);

            SetProperty(x => x.Songs, GetRandomSonglist(5, useValidLookupValues));
            SetProperty(x => x.ProductPictures, GetRandomProductPictures(5));
            SetProperty(x => x.ProductCategories, GetRandomProductCategories(5, useValidLookupValues));

            var pdo = new PredefinedDataObjects();
            SetProperty(x => x.AuthorId, useValidLookupValues ? pdo.Author.Id : ARandom.PositiveInt());
            SetProperty(x => x.ArrangerId, useValidLookupValues ? pdo.Author.Id : ARandom.PositiveInt());
            SetProperty(x => x.SupplierId, useValidLookupValues ? pdo.Supplier.Id : ARandom.PositiveInt());
            SetProperty(x => x.ManufacturerId, useValidLookupValues ? pdo.Manufacturer.Id : ARandom.PositiveInt());
            SetProperty(x => x.InstrumentId, useValidLookupValues ? pdo.Instrument.Id : ARandom.PositiveInt());
            SetProperty(x => x.LanguageId, ARandom.EnumValue<Enums.Language>());
            SetProperty(x => x.BindingId, useValidLookupValues ? pdo.Binding.Id : ARandom.PositiveInt());
            SetProperty(x => x.SeriesId, useValidLookupValues ? pdo.ProductSeries.Id : ARandom.PositiveInt());
        }

        private List<Song> GetRandomSonglist(int maxNumberOfSongs, bool useValidLookupValues)
        {
            var songs = new List<Song>();
            var random = new Random();
            int numberOfSongs = random.Next(0, maxNumberOfSongs);

            for (int cntSongs = 0; cntSongs <= numberOfSongs; cntSongs++)
            {
                songs.Add(new AnonymousSongBuilder(useValidLookupValues).build());
            }
            return songs;
        }


        private List<ProductPicture> GetRandomProductPictures(int maxNumberOfPictures)
        {
            var pictures = new List<ProductPicture>();
            var random = new Random();
            int numberOfPictures = random.Next(0, maxNumberOfPictures);

            for (int cntPictures = 0; cntPictures <= numberOfPictures; cntPictures++)
            {
                pictures.Add(new ProductPicture { FileName = ARandom.String(15), 
                                                Test = true} );
            }

            return pictures;
        }

        private List<ProductCategory> GetRandomProductCategories(int maxNumberOfCategories, bool useValidLookupValues)
        {
            var categories = new List<ProductCategory>();

            if (useValidLookupValues)
            {
                categories.Add(new PredefinedDataObjects().ProductCategory);
                categories.Add(new AnonymousProductCategoryBuilder().With(x => x.Id, 126).With(x => x.ParentId, 1).build());
                categories.Add(new AnonymousProductCategoryBuilder().With(x => x.Id, 128).With(x => x.ParentId, 1).build());
            }
            else
            {
                var random = new Random();
                int numberOfCategories = random.Next(0, maxNumberOfCategories);

                for (int cntCategories = 0; cntCategories <= numberOfCategories; cntCategories++)
                {
                    categories.Add(new AnonymousProductCategoryBuilder(useValidLookupValues).build());
                }
            }
            return categories;
        }
    }
}
