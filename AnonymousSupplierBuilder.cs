using System;
using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    public class AnonymousSupplierBuilder : DynamicFluentBuilder<Supplier>
    {
        public AnonymousSupplierBuilder()
        {
            SetProperty(x => x.Id, ARandom.IntBetween(100000, int.MaxValue));
            SetProperty(x => x.Name, "Test_" + ARandom.Text(50).TrimEnd());
            SetProperty(x => x.MinimumDeliveryDays, ARandom.PositiveInt());
            SetProperty(x => x.MaximumDeliveryDays, ARandom.PositiveInt());
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);
        }
    }
}
