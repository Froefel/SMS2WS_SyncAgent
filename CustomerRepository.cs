using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    internal class CustomerRepository
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        /// <summary>
        /// Returns a Customer object from the database
        /// </summary>
        /// <param name="customerStoreId">Id of the customer to be retrieved</param>
        /// <returns>Returns a Customer object populated with data</returns>
        internal static Customer GetCustomerById(int customerStoreId)
        {
            Customer customer = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Klanten where KlantID = @customerStoreId";
                    cmd.Parameters.AddWithValue("@customerStoreId", customerStoreId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            List<Customer> customers = LoadCustomerListFromDataReader(dr);
                            if (customers.Count >= 1)
                                customer = customers[0];
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                    }
                }
            }
            catch (OleDbException exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customerStoreId));
                throw;
            }

            return customer;
        }


        /// <summary>
        /// Check if a customer with a given StoreId or WebshopId exists in the DB
        /// </summary>
        /// <param name="customerStoreId">StoreId of the customer to be checked</param>
        /// <param name="customerWebshopId">WebshopId of the customer to be checked</param>
        internal static bool CustomerExists(int? customerStoreId, int? customerWebshopId)
        {
            bool result = false;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    if (customerStoreId != null)
                    {
                        //create command
                        OleDbCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "select count(*) from Klanten where KlantID = @storeId";
                        cmd.Parameters.AddWithValue("@storeId", customerStoreId);

                        //execute command
                        result = cmd.ExecuteScalar().Equals(1);
                    }

                    if (result == false && customerWebshopId != null)
                    {
                        //create command
                        OleDbCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "select count(*) from Klanten where KlantID_WS = @webshopId and DeleteDttm is null";
                        cmd.Parameters.AddWithValue("@webshopId", customerWebshopId);

                        //execute command
                        int rows = (int)cmd.ExecuteScalar();
                        result = rows >= 1;
                    }
                }
            }
            catch (OleDbException exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customerStoreId));
                throw;
            }

            return result;
        }


        /// <summary>
        /// Check if a customer with a given email exists in the DB
        /// </summary>
        /// <param name="emailAddress">Email address of the customer to be checked</param>
        internal static bool CustomerExists(string emailAddress)
        {
            bool result;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select count(*) from Klanten where Email = @email and DeleteDttm is null";
                    cmd.Parameters.AddWithValue("@email", emailAddress);

                    //execute command
                    int rows = (int)cmd.ExecuteScalar();
                    result = rows >= 1;
                }
            }
            catch (OleDbException exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, emailAddress));
                throw;
            }

            return result;
        }


        internal static bool EmailCanBeUsedAsLoginForCustomer(string emailAddress, int customerId)
            {
            bool result;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select count(*) from Klanten " +
                                      "where Email = @email and KlantID <> @customerId and EmailIsWSLogin = true and DeleteDttm Is Null";
                    cmd.Parameters.AddWithValue("@email", emailAddress);
                    cmd.Parameters.AddWithValue("@customerId", customerId);

                    //execute command
                    result = cmd.ExecuteScalar().Equals(0);
                }
            }
            catch (OleDbException exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, emailAddress));
                throw;
            }

            return result;
        }


        /// <summary>
        /// Returns a Customer object from the database
        /// </summary>
        /// <param name="customerStoreId">WebshopId of the customer to be retrieved</param>
        /// <returns>Returns a Customer object populated with data</returns>
        internal static Customer GetActiveCustomerByWebshopId(int webshopId)
        {
            Customer customer = null;

            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "select * from Klanten where KlantID_WS = @webshopId and DeleteDttm is null order by EmailIsWSLogin";
                    cmd.Parameters.AddWithValue("@webshopId", webshopId);

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            List<Customer> customers = LoadCustomerListFromDataReader(dr);
                            if (customers.Count >= 1)
                                customer = customers[0];
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                    }
                }
            }
            catch (OleDbException exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, webshopId));
                throw;
            }

            return customer;
        }


        /// <summary>
        /// Persist a Customer object to the database
        /// </summary>
        /// <param name="customer">Customer object to be persisted</param>
        /// <returns>Boolean true on success, false on failure</returns>
        internal static bool UpdateCustomer(Customer customer)
        {
            bool customerExists = false;
            int customerStoreId = 0;

            try
            {
                customerExists = CustomerExists(customer.StoreId, customer.WebshopId);

                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    //create command
                    var cmd = conn.CreateCommand();
                    string sql;

                    if (customerExists)
                    {
                        //create Update command
                        sql = "update Klanten set " +
                              "KlantID_WS = @webshopId, " +
                              "Naam = @lastName, " +
                              "Voornaam = @firstName, " +
                              "Straat = @shippingAddressStreet, " +
                              "Huisnummer = @shippingAddressHomeNumber, " +
                              "ZIP = @shippingAddressZip, " +
                              "Stad = @shippingAddressCity, " +
                              "Staat = @shippingAddressState, " +
                              "Land = @shippingAddressCountry, " +
                              "Telefoon = @phone, " +
                              "GSM = @mobile, " +
                              "Email = @email, " +
                              "EmailIsWSLogin = @emailIsWSLogin, " +
                              "FaktuurNaam = @billingName, " +
                              "FaktuurContact = @billingContact, " +
                              "FaktuurAdres1 = @billingAddress1, " +
                              "FaktuurAdres2 = @billingAddress2, " +
                              "FaktuurAdres3 = @billingAdddres3, " +
                              "[BTW Nummer] = @vatNumber, " +
                              "WebshopDiscount_6 = @webshopDiscount6, " +
                              "WebshopDiscount_21 = @webshopDiscount21, " +
                              "IsLeraarOfAcademie = @isTeacher, " +
                              "IsReseller = @isReseller, " +
                              "Academie = @institution, " +
                              "Discipline = @teachingSubjects, " +
                              "TeacherCardNumber = @teacherCardNumber, " +
                              "TeacherCardValidFrom = @teacherCardValidFrom, " +
                              "TeacherCardValidTo = @teacherCardValidTo, " +
                              "TeacherRegistrationNote = @teacherRegistrationNote, " +
                              "TeacherConfirmed = @teacherConfirmed, " +
                              "LastLoginDttm = @lastLoginDttm, " +
                              "UpdateDttm = @lastUpdateDttm, " +
                              "Naam4Sort = @name4Sort, " +
                              "Fullname = @fullName, " +
                              "Test = @test ";
                        if (customer.StoreId == null)
                        {
                            sql += ", SyncWS = null ";             //clear SyncWS value because at least the StoreId will be updated, which needs to be synched again to WS on the next pass
                            customer.UpdatedDttm = DateTime.Now;   //set UpdateDttm because at least the StoreId will be updated, which needs to be synched again to WS on the next pass
                        }
                        sql += "where " + (customer.StoreId != null ? "KlantID = @storeId" : "KlantID_WS = @webshopId");

                    }
                    else
                    {
                        //create Insert command
                        sql = "insert into Klanten " +
                              "(KlantID, " +
                              "KlantID_WS, Naam, Voornaam, Straat, Huisnummer, ZIP, Stad, Staat, Land, Telefoon, GSM, Email, EmailIsWSLogin, " +
                              "FaktuurNaam, FaktuurContact, FaktuurAdres1, FaktuurAdres2, FaktuurAdres3, [BTW Nummer], WebshopDiscount_6, WebshopDiscount_21, " +
                              "IsLeraarOfAcademie, IsReseller, Academie, Discipline, TeacherCardNumber, TeacherCardValidFrom, TeacherCardValidTo, " +
                              "TeacherRegistrationNote, TeacherConfirmed, UpdateDttm, LastLoginDttm, Naam4Sort, Fullname, Test " +
                              ") " +
                              "values (" +
                              "@storeId, " +
                              "@webshopId, " +
                              "@lastName, " +
                              "@firstName, " +
                              "@shippingAddressStreet, " +
                              "@shippingAddressHomeNumber, " +
                              "@shippingAddressZip, " +
                              "@shippingAddressCity, " +
                              "@shippingAddressState, " +
                              "@shippingAddressCountry, " +
                              "@phone, " +
                              "@mobile, " +
                              "@email, " +
                              "@emailIsWSLogin, " +
                              "@billingName, " +
                              "@billingContact, " +
                              "@billingAddress1, " +
                              "@billingAddress2, " +
                              "@billingAddress3, " +
                              "@vatNumber, " +
                              "@webshopDiscount6, " +
                              "@webshopDiscount21, " +
                              "@isTeacher, " +
                              "@isReseller, " +
                              "@institution, " +
                              "@teachingSubjects, " +
                              "@teacherCardNumber, " +
                              "@teacherCardValidFrom, " +
                              "@teacherCardValidTo, " +
                              "@teacherRegistrationNote, " +
                              "@teacherConfirmed, " +
                              "@lastLoginDttm, " +
                              "@lastUpdateDttm, " +
                              "@name4Sort, " +
                              "@fullName, " +
                              "@test" +
                              ")";
                        customerStoreId = Utility.GetLastPrimaryKeyValueInTable<int>("Klanten", "KlantID") + 1;
                        cmd.Parameters.AddWithValue("@storeId", customerStoreId);
                        customer.UpdatedDttm = DateTime.Now;    //set UpdateDttm because at least the StoreId will be updated, which needs to be synched again to WS on the next pass
                    }

                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@webshopId", customer.WebshopId ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastName", customer.LastName);
                    cmd.Parameters.AddWithValue("@firstName", customer.FirstName ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressStreet", customer.ShippingAddressStreet ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressHomeNumber", customer.ShippingAddressHomeNumber ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressZip", customer.ShippingAddressZip ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressCity", customer.ShippingAddressCity ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressState", customer.ShippingAddressState ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@shippingAddressCountry", customer.ShippingAddressCountry ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", customer.Phone ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@mobile", customer.Mobile ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", customer.Email ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@emailIsWSLogin", customer.Email != null && EmailCanBeUsedAsLoginForCustomer(customer.Email, customer.StoreId ?? 0));
                    cmd.Parameters.AddWithValue("@billingName", customer.BillingName ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@billingContact", customer.BillingContact ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@billingAddress1", customer.BillingAddress1 ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@billingAddress2", customer.BillingAddress2 ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@billingAddress3", customer.BillingAddress3 ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@vatNumber", customer.VatNumber ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@webshopDiscount6", customer.WebshopDiscount6);
                    cmd.Parameters.AddWithValue("@webshopDiscount21", customer.WebshopDiscount21);
                    cmd.Parameters.AddWithValue("@isTeacher", customer.IsTeacher);
                    cmd.Parameters.AddWithValue("@isReseller", customer.IsReseller);
                    cmd.Parameters.AddWithValue("@institution", customer.Institution ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teachingSubjects", customer.TeachingSubjects ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teacherCardNumber", customer.TeacherCardNumber ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teacherCardValidFrom", customer.TeacherCardValidFrom ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teacherCardValidTo", customer.TeacherCardValidTo ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teacherRegistrationNote", customer.TeacherRegistrationNote ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@teacherConfirmed", customer.TeacherConfirmed ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastUpdateDttm", customer.UpdatedDttm ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@lastLoginDttm", customer.LastLoginDttm ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@name4Sort", customer.Name4Sort ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@fullName", customer.FullName ?? (object) DBNull.Value);
                    cmd.Parameters.AddWithValue("@test", customer.Test);

                    if (customerExists && customer.StoreId != null)
                        cmd.Parameters.AddWithValue("@storeId", customer.StoreId);

                    cmd.ExecuteNonQuery();

                    if (!customerExists)
                        customer.StoreId = customerStoreId;
                    else if (customerExists && customer.StoreId == null && customer.WebshopId != null)
                        customer.StoreId = GetActiveCustomerByWebshopId((int)customer.WebshopId).StoreId;

                    return true;
                }
            }
            catch (OleDbException exception)
            {
                log.ErrorFormat("Database error occurred when updating {0} customer.\n{1}\nException={2}", customerExists ? "existing" : "new", customer, exception);
                throw;
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customer));
                throw;
            }
        }


        /// <summary>
        /// Retrieve a list of customers whose data has been changed or deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active database connection</param>
        /// <param name="action">Identifying name of the synchronization action</param>
        /// <returns>List of customers</returns>
        private static List<Customer> GetCustomerChangesByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn, Enums.UpdateActions action)
        {
            bool bLocalConnection = false;
            var customers = new List<Customer>();

            //create a connection if none exists
            if (conn == null)
            {
                conn = DAL.GetConnection();
                bLocalConnection = true;
            }
            else if (conn.State == ConnectionState.Closed)
                conn.Open();

            string sqlWhere = "";
            switch (action)
            {
                case Enums.UpdateActions.customer_SMS2WS_update:
                    sqlWhere = "(SyncWS Is Null " +
                               "or UpdateDttm > SyncWS) " +
                               "and Email Is Not Null " +
                               "and ((CreateDttm between @timestampStart and @timestampEnd) or " +
                               "(UpdateDttm between @timestampStart and @timestampEnd)) ";
                    break;

                case Enums.UpdateActions.customer_SMS2WS_delete:
                    sqlWhere = "(SyncWS Is Null " +
                               "or UpdateDttm > SyncWS) " +
                               "and Email Is Not Null " +
                               "and (DeleteDttm between @timestampStart and @timestampEnd) ";
                    break;

                case Enums.UpdateActions.customer_SMS2WS_password_reset:
                    sqlWhere = "ForcePasswordReset = true " + 
                               "and Email Is Not Null ";
                    break;

                case Enums.UpdateActions.customer_SMS2WS_teacher_approval:
                    sqlWhere = "SendTeacherConfirmedEmail = true " +
                               "and Email Is Not Null ";
                    break;
            }

            //create command
            OleDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "select * from Klanten " +
                              "where " + sqlWhere + 
                              "order by KlantID";

            cmd.Parameters.AddWithValue("@timestampStart", timestampStart.ToString());
            cmd.Parameters.AddWithValue("@timestampEnd", timestampEnd.ToString());

            try
            {
                //execute a datareader, closing the connection when all the data is read from it
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    if (dr != null && dr.HasRows)
                        customers = LoadCustomerListFromDataReader(dr);
                }
            }
            catch (Exception exception)
            {
                throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
            }

            if (bLocalConnection)
                conn.Close();

            return customers;
        }


        /// <summary>
        /// Retrieve a list of customers whose data has been changed in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active database connection</param>
        /// <returns>List of customers</returns>
        internal static List<Customer> GetUpdatedCustomersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetCustomerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.customer_SMS2WS_update);
        }


        /// <summary>
        /// Retrieve a list of customers that have been deleted in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active database connection</param>
        /// <returns>List of customers</returns>
        internal static List<Customer> GetDeletedCustomersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetCustomerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.customer_SMS2WS_delete);
        }


        /// <summary>
        /// Retrieve a list of customers with a password reset request in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active database connection</param>
        /// <returns>List of customers</returns>
        internal static List<Customer> GetPasswordResetCustomersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetCustomerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.customer_SMS2WS_password_reset);
        }



        /// <summary>
        /// Retrieve a list of customers whose teacher status request was approved in a given time frame
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active database connection</param>
        /// <returns>List of customers</returns>
        internal static List<Customer> GetTeacherStatusApprovedCustomersByTimestamp(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            return GetCustomerChangesByTimestamp(timestampStart, timestampEnd, conn, Enums.UpdateActions.customer_SMS2WS_teacher_approval);
        }



        /// <summary>
        /// Set the Synchronization status in the SMS after a customer object has been synchronized from or to the SMS
        /// </summary>
        /// <param name="customerStoreId">storeId of the customer that was successfully synchronized</param>
        /// <param name="status">Boolean synchronization status</param>
        /// <returns></returns>
        public static bool SetCustomerSyncStatus(int customerStoreId, bool status)
        {
            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    string sql = "update Klanten set " +
                                 "SyncWS = @syncDttm " +
                                 "where KlantID = @customerStoreId";

                    //create and execute command
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@syncDttm", DateTime.Parse(DateTime.Now.ToString()));
                    cmd.Parameters.AddWithValue("@customerStoreId", customerStoreId);
                    int affectedRecords = cmd.ExecuteNonQuery();

                    if (affectedRecords == 1)
                    {
                        string msg = String.Format("Customer synched: {0}", customerStoreId);
                        Console.WriteLine(msg);
                        log.Info(msg);
                    }
                    else
                    {
                        string msg = String.Format("Customer to be synched could not be found: {0}", customerStoreId);
                        Console.WriteLine(msg);
                        log.Info(msg);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customerStoreId, status));
                throw;
            }

            return true;
        }


        /// <summary>
        /// Clears the flag that indicates that an approval confirmation email has to be sent
        /// </summary>
        /// <param name="customerStoreId">storeId of the customer to whom an approval confirmation email was sent</param>
        /// <returns></returns>
        public static bool ConfirmPasswordReset(int customerStoreId)
        {
            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    string sql = "update Klanten set " +
                                 "ForcePasswordReset = false " +
                                 "where KlantID = @customerStoreId";

                    //create and execute command
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@customerStoreId", customerStoreId);
                    int affectedRecords = cmd.ExecuteNonQuery();

                    if (affectedRecords == 1)
                    {
                        string msg = String.Format("Customer password reset flag cleared for customer: {0}", customerStoreId);
                        Console.WriteLine(msg);
                        log.Info(msg);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customerStoreId));
                throw;
            }

            return true;
        }

        /// <summary>
        /// Clears the flag that indicates that an approval confirmation email has to be sent
        /// </summary>
        /// <param name="customerStoreId">storeId of the customer to whom an approval confirmation email was sent</param>
        /// <returns></returns>
        public static bool ConfirmSendTeacherConfirmedEmail(int customerStoreId)
        {
            try
            {
                //create and open connection
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    string sql = "update Klanten set " +
                                 "SendTeacherConfirmedEmail = false " +
                                 "where KlantID = @customerStoreId";

                    //create and execute command
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@customerStoreId", customerStoreId);
                    int affectedRecords = cmd.ExecuteNonQuery();

                    if (affectedRecords == 1)
                    {
                        string msg = String.Format("Teacher confirmation email flag cleared for customer: {0}", customerStoreId);
                        Console.WriteLine(msg);
                        log.Info(msg);
                    }
                }
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, customerStoreId));
                throw;
            }

            return true;
        }
        


        /// <summary>
        /// Populates a list with customer objects from a data reader object
        /// </summary>
        /// <param name="reader">Data Reader containing one or more customer records</param>
        /// <returns></returns>
        private static List<Customer> LoadCustomerListFromDataReader(OleDbDataReader reader)
        {
            var customers = new List<Customer>();
            int customerStoreId = -1;

            try
            {
                while (reader.Read())
                {
                    var customer = new Customer();
                    customer.StoreId = reader.GetNullableInt32("KlantID");
                    if (customer.StoreId != null) customerStoreId = (int)customer.StoreId;
                    customer.WebshopId = reader.GetNullableInt32("KlantID_WS");
                    customer.LastName = reader.GetStringSafe(reader.GetOrdinal("Naam"));
                    customer.FirstName = reader.GetStringSafe(reader.GetOrdinal("Voornaam"));
                    customer.ShippingAddressStreet = reader.GetStringSafe(reader.GetOrdinal("Straat"));
                    customer.ShippingAddressHomeNumber = reader.GetStringSafe(reader.GetOrdinal("Huisnummer"));
                    customer.ShippingAddressZip = reader.GetStringSafe(reader.GetOrdinal("ZIP"));
                    customer.ShippingAddressCity = reader.GetStringSafe(reader.GetOrdinal("Stad"));
                    customer.ShippingAddressState = reader.GetStringSafe(reader.GetOrdinal("Staat"));
                    customer.ShippingAddressCountry = reader.GetStringSafe(reader.GetOrdinal("Land"));
                    customer.Phone = reader.GetStringSafe(reader.GetOrdinal("Telefoon"));
                    customer.Mobile = reader.GetStringSafe(reader.GetOrdinal("GSM"));
                    customer.Email = reader.GetStringSafe(reader.GetOrdinal("Email"));
                    customer.BillingName = reader.GetStringSafe(reader.GetOrdinal("FaktuurNaam"));
                    customer.BillingContact = reader.GetStringSafe(reader.GetOrdinal("FaktuurContact"));
                    customer.BillingAddress1 = reader.GetStringSafe(reader.GetOrdinal("FaktuurAdres1"));
                    customer.BillingAddress2 = reader.GetStringSafe(reader.GetOrdinal("FaktuurAdres2"));
                    customer.BillingAddress3 = reader.GetStringSafe(reader.GetOrdinal("FaktuurAdres3"));
                    customer.VatNumber = reader.GetStringSafe(reader.GetOrdinal("BTW Nummer"));
                    customer.WebshopDiscount6 = reader.GetByte(reader.GetOrdinal("WebshopDiscount_6"));
                    customer.WebshopDiscount21 = reader.GetByte(reader.GetOrdinal("WebshopDiscount_21"));
                    customer.IsTeacher = reader.GetBoolean(reader.GetOrdinal("IsLeraarOfAcademie"));
                    customer.IsReseller = reader.GetBoolean(reader.GetOrdinal("IsReseller"));
                    customer.Institution = reader.GetStringSafe(reader.GetOrdinal("Academie"));
                    customer.TeachingSubjects = reader.GetStringSafe(reader.GetOrdinal("Discipline"));
                    customer.TeacherCardNumber = reader.GetStringSafe(reader.GetOrdinal("TeacherCardNumber"));
                    customer.TeacherCardValidFrom = reader.GetNullableDateTime(reader.GetOrdinal("TeacherCardValidFrom"));
                    customer.TeacherCardValidTo = reader.GetNullableDateTime(reader.GetOrdinal("TeacherCardValidTo"));
                    customer.TeacherRegistrationNote = reader.GetStringSafe(reader.GetOrdinal("TeacherRegistrationNote"));
                    customer.TeacherConfirmed = reader.GetNullableDateTime(reader.GetOrdinal("TeacherConfirmed"));
                    customer.LastLoginDttm = reader.GetNullableDateTime(reader.GetOrdinal("LastLoginDttm"));
                    customer.CreatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("CreateDttm"));
                    customer.UpdatedDttm = reader.GetNullableDateTime(reader.GetOrdinal("UpdateDttm"));
                    customer.DeletedDttm = reader.GetNullableDateTime(reader.GetOrdinal("DeleteDttm"));
                    customer.ForcePasswordReset = reader.GetBoolean(reader.GetOrdinal("ForcePasswordReset"));
                    customer.Test = reader.GetBoolean(reader.GetOrdinal("Test"));

                    customers.Add(customer);
                }
            }
            catch (Exception exception)
            {
                log.ErrorFormat("Error in {0} (data record with KlantID={1}, exception={2})", MethodBase.GetCurrentMethod().Name, customerStoreId, exception);
                throw;
            }


            return customers;
        }
        
        /// <summary>
        /// Converts a customer represented in an Xml string to a Customer object
        /// </summary>
        /// <param name="xmlString">Xml definition of the customer</param>
        /// <returns>Returns a Customer object populated with data</returns>
        internal static Customer LoadCustomerFromXml(string xmlString)
        {
            var customer = new Customer();

            try
            {
                var xml = XElement.Parse(xmlString);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("store_id")))
                    customer.StoreId = Convert.ToInt32(xml.Element("store_id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("id")))
                    customer.WebshopId = Convert.ToInt32(xml.Element("id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("last_name")))
                    customer.LastName = xml.Element("last_name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("first_name")))
                    customer.FirstName = xml.Element("first_name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_street")))
                    customer.ShippingAddressStreet = xml.Element("shipping_address_street").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_home_number")))
                    customer.ShippingAddressHomeNumber = xml.Element("shipping_address_home_number").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_zip")))
                    customer.ShippingAddressZip = xml.Element("shipping_address_zip").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_city")))
                    customer.ShippingAddressCity = xml.Element("shipping_address_city").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_state_id")))
                    customer.ShippingAddressStateId = Convert.ToInt32(xml.Element("shipping_address_state_id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_state_name")))
                    customer.ShippingAddressState = xml.Element("shipping_address_state_name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("shipping_address_country_id")))
                    customer.ShippingAddressCountryId = Convert.ToInt32(xml.Element("shipping_address_country_id").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("phone")))
                    customer.Phone = xml.Element("phone").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("mobile")))
                    customer.Mobile = xml.Element("mobile").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("email")))
                    customer.Email = xml.Element("email").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("billing_name")))
                    customer.BillingName = xml.Element("billing_name").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("billing_contact")))
                    customer.BillingContact = xml.Element("billing_contact").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("billing_address1")))
                    customer.BillingAddress1 = xml.Element("billing_address1").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("billing_address2")))
                    customer.BillingAddress2 = xml.Element("billing_address2").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("billing_address3")))
                    customer.BillingAddress3 = xml.Element("billing_address3").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("vat_number")))
                    customer.VatNumber = xml.Element("vat_number").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("std_discount_for_tax_rate_id1")))
                    customer.WebshopDiscount6 = Convert.ToByte(xml.Element("std_discount_for_tax_rate_id1").Value);
                else
                    customer.WebshopDiscount6 = 0;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("std_discount_for_tax_rate_id2")))
                    customer.WebshopDiscount21 = Convert.ToByte(xml.Element("std_discount_for_tax_rate_id2").Value);
                else
                    customer.WebshopDiscount21 = 0;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("is_teacher")))
                    customer.IsTeacher = Convert.ToBoolean(int.Parse(xml.Element("is_teacher").Value));

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("is_reseller")))
                    customer.IsReseller = Convert.ToBoolean(int.Parse(xml.Element("is_reseller").Value));

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("institution")))
                    customer.Institution = xml.Element("institution").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("teaching_subjects")))
                    customer.TeachingSubjects = xml.Element("teaching_subjects").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("card_number")))
                    customer.TeacherCardNumber = xml.Element("card_number").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("card_valid_from"), "0000-00-00 00:00:00"))
                    customer.TeacherCardValidFrom = Convert.ToDateTime(xml.Element("card_valid_from").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("card_valid_to"), "0000-00-00 00:00:00"))
                    customer.TeacherCardValidTo = Convert.ToDateTime(xml.Element("card_valid_to").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("teacher_registration_note")))
                    customer.TeacherRegistrationNote = xml.Element("teacher_registration_note").Value;

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("teacher_confirmed"), "0000-00-00 00:00:00"))
                    customer.TeacherConfirmed = Convert.ToDateTime(xml.Element("teacher_confirmed").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("last_login")))
                    customer.LastLoginDttm = Convert.ToDateTime(xml.Element("last_login").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("test")))
                    customer.Test = xml.Element("test").Value.Equals("1");

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("created")))
                    customer.CreatedDttm = Convert.ToDateTime(xml.Element("created").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("updated")))
                    customer.UpdatedDttm = Convert.ToDateTime(xml.Element("updated").Value);

                if (!Utility.XmlElementIsEmptyOrSpecialValue(xml.Element("deleted")))
                    customer.DeletedDttm = Convert.ToDateTime(xml.Element("deleted").Value);
            }
            catch (Exception exception)
            {
                log.Error(Utility.GetExceptionWithMethodSignatureDetails(MethodBase.GetCurrentMethod(), exception, xmlString));
                throw;
            }

            return customer;
        }


        /// <summary>
        /// Converts a list of customers represented in an Xml string to a List<Customer> object
        /// </summary>
        /// <param name="xmlString">Xml definition of the customer list</param>
        /// <returns>Returns a List<Customer> object populated with data</returns>
        internal static List<Customer> LoadCustomerListFromXml(string xmlString)
        {
            XElement xml = XElement.Parse(xmlString);

            return xml.Nodes().Select(node => LoadCustomerFromXml(node.ToString())).ToList();
        }
    }
}
