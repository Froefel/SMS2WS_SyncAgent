using System;
using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    class AnonymousManufacturerBuilder : DynamicFluentBuilder<Manufacturer>
    {
        public AnonymousManufacturerBuilder()
        {
            SetProperty(x => x.Id, ARandom.IntBetween(100000, int.MaxValue));
            SetProperty(x => x.Name, "Test_" + ARandom.Text(100).TrimEnd());
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);
        }
    }
}
