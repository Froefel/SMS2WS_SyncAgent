using System;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    public class Customer : ISyncObject
    {
        private const string _objectName = "customer";
        public int? StoreId { get; set; }
        public int? WebshopId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string ShippingAddressStreet { get; set; }
        public string ShippingAddressHomeNumber { get; set; }
        public string ShippingAddressZip { get; set; }
        public string ShippingAddressCity { get; set; }
        public string ShippingAddressState { get; set; }
        public int? ShippingAddressStateId { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string BillingName { get; set; }
        public string BillingContact { get; set; }
        public string BillingAddress1 { get; set; }
        public string BillingAddress2 { get; set; }
        public string BillingAddress3 { get; set; }
        public string VatNumber { get; set; }
        public byte WebshopDiscount6 { get; set; }
        public byte WebshopDiscount21 { get; set; }
        public bool IsTeacher { get; set; }
        public bool IsReseller { get; set; }
        public string Institution { get; set; }
        public string TeachingSubjects { get; set; }
        public string TeacherCardNumber { get; set; }
        public string TeacherRegistrationNote { get; set; }
        public bool ForcePasswordReset { get; set; }
        public bool Test { get; set; }
        private string _shippingAddressCountry;
        private int? _shippingAddressCountryId;
        private string _name4Sort;
        private string _fullName;
        private DateTime? _teacherCardValidFrom;
        private DateTime? _teacherCardValidTo;
        private DateTime? _teacherConfirmed;
        private DateTime? _lastLoginDttm;
        private DateTime? _createdDttm;
        private DateTime? _updatedDttm;
        private DateTime? _deletedDttm;


        public string ObjectName
        {
            get { return _objectName; }
        }

        public int? ShippingAddressCountryId
        {
            get { return _shippingAddressCountryId; }

            set { 
                _shippingAddressCountryId = value;
                
                //get corresponding text value for the CountryId
                if (value != null && Enum.IsDefined(typeof(Enums.Country), value))
                    _shippingAddressCountry = Enum.GetName(typeof(Enums.Country), value);
                else
                    _shippingAddressCountry = null;
            }
        }

        public string ShippingAddressCountry
        {
            get { return _shippingAddressCountry; }

            set
            {
                _shippingAddressCountry = value;

                //get corresponding text value for the CountryId
                if (value != null && Enum.IsDefined(typeof (Enums.Country), value))
                    _shippingAddressCountryId = (int)Enum.Parse(typeof (Enums.Country), value);
                else
                    _shippingAddressCountryId = null;
            }
        }

        public string Name4Sort
        {
            get
            {
                if (_name4Sort == null)
                    _name4Sort = MakeName4Sort();
                
                return _name4Sort;
            }
        }

        public string FullName
        {
            get
            {
                if (_fullName == null)
                    _fullName = MakeFullName();

                return _fullName;
            }
        }

        public DateTime? TeacherCardValidFrom
        {
            get { return _teacherCardValidFrom; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _teacherCardValidFrom = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? TeacherCardValidTo
        {
            get { return _teacherCardValidTo; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _teacherCardValidTo = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? TeacherConfirmed
        {
            get { return _teacherConfirmed; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _teacherConfirmed = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? LastLoginDttm
        {
            get { return _lastLoginDttm; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _lastLoginDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? CreatedDttm
        {
            get { return _createdDttm; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _createdDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? UpdatedDttm
        {
            get { return _updatedDttm; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _updatedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }

        public DateTime? DeletedDttm
        {
            get { return _deletedDttm; }

            set
            {
                //remove milliseconds from DateTime in order to make it compatible with MS-Access
                _deletedDttm = value != null ? (DateTime?)DateTime.Parse(value.ToString()) : null;
            }
        }


        private string MakeName4Sort()
        {
            string tmpName4Sort;
            string tmpFirstName = (!string.IsNullOrEmpty(FirstName) ? FirstName : "zzz");

            if (LastName.Length > 0)
                tmpName4Sort = Utility.FilterString(LastName);
            else
                tmpName4Sort = Utility.FilterString(BillingName);

            if (tmpName4Sort.Length > 0)
                tmpName4Sort = tmpName4Sort + " " + tmpFirstName;
            else
                tmpName4Sort = tmpFirstName;

            return tmpName4Sort.Trim().ToLower();
        }


        private string MakeFullName()
        {
            return (LastName.Replace(" ", "") + " " + FirstName).Trim();
        }

        
        public bool SetSyncStatus(bool status)
        {
            return CustomerRepository.SetCustomerSyncStatus((int)StoreId, status);
        }


        public bool ConfirmPasswordReset()
        {
            return CustomerRepository.ConfirmPasswordReset((int) StoreId);
        }


        public bool ConfirmSendTeacherConfirmedEmail()
        {
            return CustomerRepository.ConfirmSendTeacherConfirmedEmail((int)StoreId);
        }


        /// <summary>
        /// Converts the current object to an Xml representation
        /// </summary>
        /// <returns>Returns a System.String containing an Xml representation of the current object</returns>
        public string ToXml()
        {
            var xml = new XElement(_objectName,
                        !WebshopId.IsNullOrZero() ? new XElement("id", WebshopId) : null,
                        !StoreId.IsNullOrZero() ? new XElement("store_id", StoreId) : null,
                        LastName != null ? new XElement("last_name", LastName) : null,
                        FirstName != null ? new XElement("first_name", FirstName) : null,
                        ShippingAddressStreet != null ? new XElement("shipping_address_street", ShippingAddressStreet) : null,
                        ShippingAddressHomeNumber != null ? new XElement("shipping_address_home_number", ShippingAddressHomeNumber) : null,
                        ShippingAddressZip != null ? new XElement("shipping_address_zip", ShippingAddressZip) : null,
                        ShippingAddressCity != null ? new XElement("shipping_address_city", ShippingAddressCity) : null,
                        ShippingAddressStateId != null ? new XElement("shipping_address_state_id", ShippingAddressStateId) : null,
                        ShippingAddressState != null ? new XElement("shipping_address_state_name", ShippingAddressState) : null,
                        ShippingAddressCountryId != null ? new XElement("shipping_address_country_id", ShippingAddressCountryId) : null,
                        ShippingAddressCountry != null ? new XElement("shipping_address_country_name", ShippingAddressCountry) : null,
                        Phone != null ? new XElement("phone", Phone) : null,
                        Mobile != null ? new XElement("mobile", Mobile) : null,
                        Email != null ? new XElement("email", Email) : null,
                        BillingName != null ? new XElement("billing_name", BillingName) : null,
                        BillingContact != null ? new XElement("billing_contact", BillingContact) : null,
                        BillingAddress1 != null ? new XElement("billing_address1", BillingAddress1) : null,
                        BillingAddress2 != null ? new XElement("billing_address2", BillingAddress2) : null,
                        BillingAddress3 != null ? new XElement("billing_address3", BillingAddress3) : null,
                        VatNumber != null ? new XElement("vat_number", VatNumber) : null,
                        new XElement("std_discount_for_tax_rate_id1", WebshopDiscount6),
                        new XElement("std_discount_for_tax_rate_id2", WebshopDiscount21),
                        new XElement("is_teacher", IsTeacher.ToInt().ToString()),
                        new XElement("is_reseller", IsReseller.ToInt().ToString()),
                        Institution != null ? new XElement("institution", Institution) : null,
                        TeachingSubjects != null ? new XElement("teaching_subjects", TeachingSubjects) : null,
                        TeacherCardNumber != null ? new XElement("card_number", TeacherCardNumber) : null,
                        TeacherCardValidFrom != null ? new XElement("card_valid_from", ((DateTime)TeacherCardValidFrom).ToString("yyyy-MM-dd")) : null,
                        TeacherCardValidTo != null ? new XElement("card_valid_to", ((DateTime)TeacherCardValidTo).ToString("yyyy-MM-dd")) : null,
                        TeacherRegistrationNote != null ? new XElement("teacher_registration_note", TeacherRegistrationNote) : null,
                        !TeacherConfirmed.IsNullOrDefault() ? new XElement("teacher_confirmed", ((DateTime)TeacherConfirmed).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !LastLoginDttm.IsNullOrDefault() ? new XElement("last_login", ((DateTime)LastLoginDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        Test == true ? new XElement("test", Test.ToInt()) : null,
                        !CreatedDttm.IsNullOrDefault() ? new XElement("created", ((DateTime)CreatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !UpdatedDttm.IsNullOrDefault() ? new XElement("updated", ((DateTime)UpdatedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null,
                        !DeletedDttm.IsNullOrDefault() ? new XElement("deleted", ((DateTime)DeletedDttm).ToString("yyyy-MM-dd HH:mm:ss")) : null
                      );
   
            return xml.ToString();
        }


        public static string ValidateXmlStructure(string xml)
        {
            const string xsdFileName = "Customer.xsd";
            string validationError = Utility.ValidateXmlStructure(xml, xsdFileName);

            return validationError;
        }
    }
}
