using System;
using Fluency;
using Fluency.DataGeneration;

namespace SMS2WS_SyncAgent
{
    public class AnonymousCustomerBuilder : DynamicFluentBuilder<Customer>
    {
        public AnonymousCustomerBuilder()
        {
            SetProperty(x => x.StoreId, ARandom.IntBetween(1000000, int.MaxValue));
            //SetProperty(x => x.WebshopId, ARandom.IntBetween(1000000, int.MaxValue));
            SetProperty(x => x.LastName, "Test_" + ARandom.LastName());
            SetProperty(x => x.FirstName, "Test_" + ARandom.FirstName());
            SetProperty(x => x.ShippingAddressStreet, "Test_" + ARandom.Text(15).TrimEnd());
            SetProperty(x => x.ShippingAddressHomeNumber, ARandom.Text(5).TrimEnd());
            SetProperty(x => x.ShippingAddressCity, ARandom.City());
            SetProperty(x => x.ShippingAddressZip, ARandom.ZipCode());
            SetProperty(x => x.ShippingAddressState, "");
            SetProperty(x => x.ShippingAddressCountryId, (int?)ARandom.EnumValue<Enums.Country>());
            SetProperty(x => x.ShippingAddressCountry, Enum.GetName(typeof(Enums.Country), ARandom.EnumValue<Enums.Country>()));
            SetProperty(x => x.Phone, ARandom.StringPattern("##/### ## ##"));
            SetProperty(x => x.Mobile, ARandom.StringPattern("####/## ## ##"));
            SetProperty(x => x.Email, "archive+" + ARandom.StringFromCharacterSet(15, "abcdefghijklmnopqrstuvwxyz").TrimEnd() + "@animatomusic.be");
            SetProperty(x => x.BillingAddress1, ARandom.Text(15).TrimEnd());
            SetProperty(x => x.BillingAddress2, ARandom.Text(15).TrimEnd());
            SetProperty(x => x.BillingAddress3, ARandom.Text(15).TrimEnd());
            SetProperty(x => x.BillingName, ARandom.Text(30).TrimEnd());
            SetProperty(x => x.BillingContact, ARandom.Text(30).TrimEnd());
            SetProperty(x => x.VatNumber, ARandom.Text(15).TrimEnd());
            SetProperty(x => x.WebshopDiscount6, (byte)ARandom.IntBetween(0, 100));
            SetProperty(x => x.WebshopDiscount21, (byte)ARandom.IntBetween(0, 100));
            SetProperty(x => x.IsTeacher, ARandom.Boolean());
            SetProperty(x => x.IsReseller, ARandom.Boolean());
            SetProperty(x => x.Institution, ARandom.Text(50).TrimEnd());
            SetProperty(x => x.TeacherCardNumber, "17511240450-000010");
            SetProperty(x => x.TeacherCardValidFrom, ARandom.DateAfter(DateTime.Today));
            SetProperty(x => x.TeacherCardValidTo, ARandom.DateAfter(DateTime.Today));
            SetProperty(x => x.TeacherRegistrationNote, ARandom.Text(150).TrimEnd());
            SetProperty(x => x.TeacherConfirmed, ARandom.DateTime());
            SetProperty(x => x.TeachingSubjects, ARandom.Text(50).TrimEnd());
            SetProperty(x => x.Test, true);
            SetProperty(x => x.CreatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.UpdatedDttm, ARandom.DateTimeInPast());
            SetProperty(x => x.DeletedDttm, (DateTime?)null);
        }
    }
}
