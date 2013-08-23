using System;
using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    public class AnonymousInstrumentBuilder : DynamicFluentBuilder<Instrument>
    {
        public AnonymousInstrumentBuilder()
        {
            SetProperty(x => x.Id, ARandom.IntBetween(100000, int.MaxValue));
            SetProperty(x => x.Name_EN, "Test_" + ARandom.Text(100).TrimEnd());
            SetProperty(x => x.Name_NL, "Test_" + ARandom.Text(100).TrimEnd());
            SetProperty(x => x.Name_FR, "Test_" + ARandom.Text(100).TrimEnd());
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);
        }
    }
}
