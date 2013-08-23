using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    public class AnonymousSongBuilder : DynamicFluentBuilder<Song>
    {
        public AnonymousSongBuilder()
        {
            InitializeObject(false);
        }

        public AnonymousSongBuilder(bool UseValidLookupValues)
        {
            InitializeObject(UseValidLookupValues);
        }

        private void InitializeObject(bool UseValidLookupValues)
        {
            var pdo = new PredefinedDataObjects();

            SetProperty(x => x.Id, ARandom.IntBetween(1000000, int.MaxValue));
            SetProperty(x => x.Sequence, ARandom.PositiveInt());
            SetProperty(x => x.Title, "Test_" + ARandom.Text(200).TrimEnd());
            SetProperty(x => x.AuthorId, UseValidLookupValues ? pdo.Author.Id : ARandom.PositiveInt());
            SetProperty(x => x.Test, true);
        }
    }
}
