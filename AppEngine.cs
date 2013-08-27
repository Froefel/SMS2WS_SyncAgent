using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Data.OleDb;
using System.Xml.Linq;

namespace SMS2WS_SyncAgent
{
    class AppEngine
    {
        private static Log4NetWrapper log = LogManager.GetLogger();
        private int cntProductsAffected;
        private int sessionId;
        private string[] m_args;
        protected bool ReadInput = true;
        protected TextReader In = null;
        protected TextWriter Out = null;
        protected TextWriter Error = null;
        private readonly string[] updateableObjectNames = { "author",
                                                            "binding",
                                                            "customer_SMS2WS",
                                                            "customer_WS2SMS",
                                                            "instrument",
                                                            "manufacturer",
                                                            "product",
                                                            "productCategory",
                                                            "productPicture",
                                                            "productSeries",
                                                            "supplier"
                                                          };
        
        //constructor
        public AppEngine()
        {
            //by default, read from/write to standard streams
            this.In = Console.In;
            this.Out = Console.Out;
            this.Error = Console.Error;
        }

                
        public void Main(string[] args)
        {
            m_args = args;

            GetApplicationSettings();

            using (OleDbConnection conn = DAL.GetConnection())
            {
                if (conn != null)
                {
                    DateTime timestampStart;
                    DateTime timestampNow = DateTime.Now;
                    sessionId = SyncSessionLogger.GetNextSessionId();

                    bool syncSuccess = true;
                    bool syncSuccess_customer_ws2sms = true;

                    //build dependency tree
                    List<string> actionDependencies = GetUpdateActionDependencies(AppSettings.ObjectToBeProcessed);

                    //loop through the UpdateActions (they are already in order of execution)
                    foreach (Enums.UpdateActions action in Enum.GetValues(typeof (Enums.UpdateActions)))
                    {
                        if (actionDependencies.Contains(action.ToString()))
                        {
                            log.InfoFormat("Processing {0}...", action.ToString());
                            this.Out.WriteLine("Processing {0}...", action.ToString());
                            
                            switch (action)
                            {
                                case Enums.UpdateActions.customer_WS2SMS_update:
                                    timestampStart = AppSettings.LastSyncTimestamp_customer_ws2sms;
                                    break;
                                default:
                                    timestampStart = AppSettings.LastSyncTimestamp;
                                    break;
                            }
                            bool tmpSyncSuccess = SynchronizeData(action, timestampStart, timestampNow, conn);
                            if (syncSuccess && tmpSyncSuccess == false)
                                syncSuccess = false;
                            if (action == Enums.UpdateActions.customer_WS2SMS_update && syncSuccess_customer_ws2sms && tmpSyncSuccess == false)
                                syncSuccess_customer_ws2sms = false;

                            Utility.UpdatePresence("");
                        }
                    }
                    
                    if (syncSuccess)
                        AppSettings.LastSyncTimestamp = timestampNow;

                    if (syncSuccess_customer_ws2sms)
                        AppSettings.LastSyncTimestamp_customer_ws2sms = timestampNow;
                }

                /*
                this.PreProcess();
                if (this.ReadInput)
                {
                    string currentLine = this.In.ReadLine();
                    while (currentLine != null)
                    {
                        this.ProcessLine(currentLine);
                        currentLine = this.In.ReadLine();
                    }
                }
                this.PostProcess();
                */
            }
        }


        public void Main(string[] args, TextReader In, TextWriter Out, TextWriter Error)
        {
            //this version of Main allows alternate streams
            this.In = In;
            this.Out = Out;
            this.Error = Error;

            this.Main(args);
        }



        /// <summary>
        /// Get a list of actions that need to be performed, in the order in which they are to be performed
        /// </summary>
        /// <param name="argumentValue">
        /// String value containing the name of the base object to be updated.
        /// If this value is blank then all objects will be included
        /// </param>
        /// <returns>A list of update action strings</returns>
        private List<string> GetUpdateActionDependencies(string argumentValue)
        {
            var result = new List<string>();
            string sql;

            using (OleDbConnection conn = DAL.GetConnection())
            {
                if (conn != null)
                {
                    //create command
                    var cmd = conn.CreateCommand();

                    if (argumentValue.Length > 0)
                    {
                        sql = "select id, name " +
                              "from UpdateActions " +
                              "where object = @objectName " +
                              "union " +
                              "select UpdateActions.id, UpdateActions.name " +
                              "from UpdateActions " +
                              "inner join UpdateActionDependencies on UpdateActions.id = UpdateActionDependencies.depends_on_id " +
                              "where UpdateActionDependencies.action_id in (select distinct id from UpdateActions where object = @objectName)";
                        cmd.Parameters.AddWithValue("@objectName", argumentValue);
                    }
                    else
                        sql = "select name, id " +
                              "from UpdateActions";

                    cmd.CommandText = sql;

                    try
                    {
                        //execute a datareader, closing the connection when all the data is read from it
                        using (OleDbDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                                result.Add(dr.GetStringSafe("name"));
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Reads application settings from a settings file and from command-line arguments
        /// and stores them in a dedicated AppSettings class
        /// </summary>
        public void GetApplicationSettings()
        {
            //read settings from SMS
            int tmpInt;
            Int32.TryParse(GetSharedSettingFromSMS("SyncAgentWaitBetweenSyncSessionsInSeconds"), out tmpInt);
            if (tmpInt == 0) tmpInt = 60;
            AppSettings.WaitBetweenSyncSessionsInSeconds = tmpInt;

            //read settings from Settings file
            /*    
            if (ConfigurationManager.AppSettings["WaitBetweenSyncSessionsInSeconds"] != null)
            {
                int tmp;
                if (int.TryParse(ConfigurationManager.AppSettings["WaitBetweenSyncSessionsInSeconds"], out tmp))
                    AppSettings.WaitBetweenSyncSessionsInSeconds = tmp;
            }
            */

            if (ConfigurationManager.AppSettings["LastSyncTimestamp_customer_ws2sms"] != null)
            {
                DateTime tmp;
                if (DateTime.TryParse(ConfigurationManager.AppSettings["LastSyncTimestamp_customer_ws2sms"], out tmp))
                    AppSettings.LastSyncTimestamp_customer_ws2sms = tmp;
            }

            if (ConfigurationManager.AppSettings["LastSyncTimestamp"] != null)
            {
                DateTime tmp;
                if (DateTime.TryParse(ConfigurationManager.AppSettings["LastSyncTimestamp"], out tmp))
                    AppSettings.LastSyncTimestamp = tmp;
            }

            //read settings from command-line arguments
            //note: if command-line arguments are specified, they must be valid and will overwrite the values from the Settings file
            if (m_args.Length > 0)
            {
                var arguments = new CommandLineArguments(m_args);

                //validate argument values
                foreach (string arg in arguments.Parameters.Keys)
                {
                    switch (arg)
                    {
                        case "o":
                        {
                            if (updateableObjectNames.Contains(arguments.Single(arg)))
                                AppSettings.ObjectToBeProcessed = arguments.Single(arg);
                            else
                                throw new Exception(String.Format("Invalid command-line argument value for -o: {0}", arguments.Single(arg)));
                            break;
                        }

                        case "t":
                        {
                            DateTime timestamp;

                            if (DateTime.TryParse(arguments.Single(arg), out timestamp))
                                AppSettings.LastSyncTimestamp = DateTime.Parse(arguments.Single(arg));
                            else
                                throw new Exception(String.Format("Invalid command-line argument value for -t: {0}", arguments.Single(arg)));
                            break;
                        }

                        case "tc":
                        {
                            DateTime timestamp;

                            if (DateTime.TryParse(arguments.Single(arg), out timestamp))
                                AppSettings.LastSyncTimestamp_customer_ws2sms = DateTime.Parse(arguments.Single(arg));
                            else
                                throw new Exception(String.Format("Invalid command-line argument value for -tc: {0}", arguments.Single(arg)));
                            break;
                        }

                        default:
                        {
                            throw new Exception(String.Format("Invalid command-line argument {0}", arg));
                        }
                    }
                }
            }

            string msg = String.Format("Session parameters: waitBetweenSyncSessionsInSeconds={0}, lastSyncTimestamp={1}, lastSyncTimestamp_customer_ws2sms={2}, objectToBeProcessed={3}",
                                       AppSettings.WaitBetweenSyncSessionsInSeconds, 
                                       AppSettings.LastSyncTimestamp, 
                                       AppSettings.LastSyncTimestamp_customer_ws2sms, 
                                       AppSettings.ObjectToBeProcessed);
            this.Out.WriteLine(msg);
            log.Info(msg);
        }


        private string GetSharedSettingFromSMS(string key)
        {
            string sql;
            string value = "";

            try
            {
                using (OleDbConnection conn = DAL.GetConnection())
                {
                    if (conn != null)
                    {
                        //create command
                        var cmd = conn.CreateCommand();

                        sql = "select Waarde " +
                              "from Settings_Shared " +
                              "where Description = @key";
                        cmd.Parameters.AddWithValue("@key", key);

                        cmd.CommandText = sql;

                        try
                        {
                            //execute a datareader, closing the connection when all the data is read from it
                            using (OleDbDataReader dr = cmd.ExecuteReader())
                            {
                                while (dr.Read())
                                {
                                    value = dr.GetStringSafe(dr.GetOrdinal("Waarde"));
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            throw new Exception("Error while executing the following Sql statement:\n" + cmd.ToStringExtended(), exception);
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                throw;
            }
            return value;
        }


        /// <summary>
        /// Construct a formatted string explaining the application's command line arguments
        /// </summary>
        /// <returns></returns>
        private string Usage()
        {
            int cntObjectNames = updateableObjectNames.Length;

            string result = "Usage: Execute the update process in the SMS or the webshop (or both)\n" + 
                            "       for a single or all object types.\n\n" +
                            "SMS2WS_SyncAgent [-t[:]timestamp] [-tc[:]timestamp] [-o[:]objecttype]\n\n" +
                            "-t   Specifies the timestamp from where SMS -> WS processing should start\n" +
                            "-tc  Specifies the timestamp from where WS -> SMS processing should start\n" +
                            "-o   Specifies an object type to be processed. If this paramater is omitted,\n" + 
                            "     all object types are processed\n";

            for (int i = 0; i < cntObjectNames; i = i + 3)
            {
                if (cntObjectNames - i >= 3)
                    result += String.Format("      {0, -24}{1, -24}{2, -24}", updateableObjectNames[i], updateableObjectNames[i + 1], updateableObjectNames[i + 2]) + "\n";
                else if (cntObjectNames - i == 1)
                    result += String.Format("      {0, -24}", updateableObjectNames[i]) + "\n";
                else if (cntObjectNames - i == 2)
                    result += String.Format("      {0, -24}{1, -24}", updateableObjectNames[i], updateableObjectNames[i + 1]) + "\n";
            }

            result += "Press any key to continue...";

            return result;
        }

        private void PreProcess()
        {
            //override this to add custom logic that 
            //executes just before standard in is processed
            return;
        }

        private void PostProcess()
        {
            //override this to add custom logic that 
            //executes just after standard in is processed
            return;
        }

        private void ProcessLine(string line)
        {
            //override this to add custom processing 
            //on each line of input
            return;
        }


        protected string[] Arguments
        {
            get { return this.m_args; }
        }


        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the various object types
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeData(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            bool errorOccurred = false;

            try
            {
                switch (action)
                {
                    case Enums.UpdateActions.author_delete:
                    case Enums.UpdateActions.author_update:
                        errorOccurred = SynchronizeAuthors(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.binding_delete:
                    case Enums.UpdateActions.binding_update:
                        errorOccurred = SynchronizeBindings(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.customer_SMS2WS_delete:
                    case Enums.UpdateActions.customer_SMS2WS_update:
                        errorOccurred = SynchronizeCustomers_sms2ws(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.customer_SMS2WS_password_reset:
                        errorOccurred = ResetPasswordForCustomers_sms2ws(timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.customer_SMS2WS_teacher_approval:
                        errorOccurred = ConfirmTeacherStatusForCustomers_sms2ws(timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.customer_WS2SMS_update:
                        errorOccurred = SynchronizeCustomers_ws2sms(timestampStart);
                        break;


                    case Enums.UpdateActions.instrument_delete:
                    case Enums.UpdateActions.instrument_update:
                        errorOccurred = SynchronizeInstruments(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.manufacturer_delete:
                    case Enums.UpdateActions.manufacturer_update:
                        errorOccurred = SynchronizeManufacturers(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.product_delete:
                    case Enums.UpdateActions.product_update:
                        errorOccurred = SynchronizeProducts(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.productCategory_delete:
                    case Enums.UpdateActions.productCategory_update:
                        errorOccurred = SynchronizeProductCategories(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.productCategory_product_count:
                        errorOccurred = UpdateProductCategoryCount();
                        break;


                    case Enums.UpdateActions.productSeries_delete:
                    case Enums.UpdateActions.productSeries_update:
                        errorOccurred = SynchronizeProductSeries(action, timestampStart, timestampEnd, conn);
                        break;


                    case Enums.UpdateActions.supplier_delete:
                    case Enums.UpdateActions.supplier_update:
                        errorOccurred = SynchronizeSuppliers(action, timestampStart, timestampEnd, conn);
                        break;
                }
            }

            catch (Exception exception)
            {
                errorOccurred = true;
                string msg = String.Format("Unexpected error while processing {0}: {1}", action.ToString(), exception);
                this.Error.WriteLine(msg);
                log.Error(msg);
            }

            return (!errorOccurred);
        }
        
        
        
        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the author object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeAuthors(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var authors = new List<Author>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.author_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        authors = AuthorRepository.GetDeletedAuthorsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.author_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        authors = AuthorRepository.GetUpdatedAuthorsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (authors.Count > 0)
                {
                    foreach (var author in authors)
                    {
                        msg = "";

                        try //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.author_delete:
                                    result = WebMethods.AuthorMethods.DeleteAuthorById(author.Id);
                                    break;
                                case Enums.UpdateActions.author_update:
                                    result = WebMethods.AuthorMethods.UpdateAuthor(author);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                author.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("Author {0}: {1} (id={2})", actionVerbPastTense, author.Name, author.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("Author could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, author.Name, author.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} author in webshop: {1} (id={2}) : {3}",
                                                                                        actionVerbContinuousTense,
                                                                                        author.Name,
                                                                                        author.Id.ToString("D4"),
                                                                                        exception);
                            OutputSynchronizationError(msg, author);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} author in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }


        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the binding object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeBindings(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var bindings = new List<Binding>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.binding_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        bindings = BindingRepository.GetDeletedBindingsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.binding_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        bindings = BindingRepository.GetUpdatedBindingsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (bindings.Count > 0)
                {
                    foreach (var binding in bindings)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.binding_delete:
                                    result = WebMethods.BindingMethods.DeleteBindingById(binding.Id);
                                    break;
                                case Enums.UpdateActions.binding_update:
                                    result = WebMethods.BindingMethods.UpdateBinding(binding);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                binding.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("Binding {0}: {1} (id={2})", actionVerbPastTense, binding.Name_NL, binding.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("Binding could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, binding.Name_NL, binding.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} binding in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        binding.Name_NL, 
                                                                                        binding.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, binding);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} binding in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }



        /// <summary>
        /// Wrapper function that initiates the password reset method for the customer object type from SMS to WS
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool ResetPasswordForCustomers_sms2ws(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                List<Customer> customersFromSMS = CustomerRepository.GetPasswordResetCustomersByTimestamp(timestampStart, timestampEnd, conn);

                if (customersFromSMS.Count > 0)
                {
                    foreach (var customer in customersFromSMS)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = WebMethods.CustomerMethods.SendPasswordResetEmail(customer.Email);

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                customer.SetSyncStatus(true);
                                customer.ConfirmPasswordReset();

                                cntSuccess++;

                                msg = String.Format("Password reset for customer: {0} (webshopId={1}, storeId={2}, {3})",
                                                    (customer.FirstName + " " + customer.LastName).Trim(),
                                                    customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                    customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                    customer.Email);
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("Error resetting password for customer: {0} (webshopId={1}, storeId={2}, {3}) : {4}",
                                                    (customer.FirstName + " " + customer.LastName).Trim(),
                                                    customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                    customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                    customer.Email,
                                                    result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error resetting password for customer: {0} (webshopId={1}, storeId={2}, {3}) : {4}",
                                                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                                                        customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                                                        customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                                                        customer.Email,
                                                                                        exception);
                            OutputSynchronizationError(msg, customer);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error resetting customer password: {0}", exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, Enums.UpdateActions.customer_SMS2WS_password_reset, cntSuccess, cntFail);
            }

            return errorOccurred;
        }


        /// <summary>
        /// Wrapper function that initiates the teacher approval method for the customer object type from SMS to WS
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool ConfirmTeacherStatusForCustomers_sms2ws(DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                List<Customer> customersFromSMS = CustomerRepository.GetTeacherStatusApprovedCustomersByTimestamp(timestampStart, timestampEnd, conn);

                if (customersFromSMS.Count > 0)
                {
                    foreach (var customer in customersFromSMS)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            if (customer.WebshopId != null)
                            {
                                string result = WebMethods.CustomerMethods.ConfirmTeacherRegistration((int)customer.WebshopId);

                                if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                                {
                                    customer.SetSyncStatus(true);
                                    customer.ConfirmSendTeacherConfirmedEmail();

                                    cntSuccess++;

                                    msg = String.Format("Teacher status approval mail sent for customer: {0} (webshopId={1}, storeId={2}, {3})",
                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                        customer.WebshopId != null ? ((int) customer.WebshopId).ToString() : "{empty}",
                                                        customer.StoreId != null ? ((int) customer.StoreId).ToString("D6") : "{empty}",
                                                        customer.Email);
                                    this.Out.WriteLine(msg);
                                    log.Info(msg);
                                }
                                else
                                {
                                    cntFail++;

                                    msg = String.Format("Error sending teacher status approval mail for customer: {0} (webshopId={1}, storeId={2}, {3}) : {4}",
                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                        customer.WebshopId != null ? ((int) customer.WebshopId).ToString() : "{empty}",
                                                        customer.StoreId != null ? ((int) customer.StoreId).ToString("D6") : "{empty}",
                                                        customer.Email,
                                                        result);
                                    throw new Exception(msg);
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error sending teacher status approval mail for customer: {0} (webshopId={1}, storeId={2}, {3}) : {4}",
                                                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                                                        customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                                                        customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                                                        customer.Email,
                                                                                        exception);
                            OutputSynchronizationError(msg, customer);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error sending teacher status approval mail: {0}", exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, Enums.UpdateActions.customer_SMS2WS_teacher_approval, cntSuccess, cntFail);
            }

            return errorOccurred;
        }



        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the customer object type from SMS to WS
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeCustomers_sms2ws(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var customersFromSMS = new List<Customer>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.customer_SMS2WS_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        customersFromSMS = CustomerRepository.GetDeletedCustomersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.customer_SMS2WS_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        customersFromSMS = CustomerRepository.GetUpdatedCustomersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (customersFromSMS.Count > 0)
                {
                    foreach (var customer in customersFromSMS)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.customer_SMS2WS_delete:
                                    result = WebMethods.CustomerMethods.DeleteCustomerByWebshopId((int)customer.WebshopId);
                                    break;
                                case Enums.UpdateActions.customer_SMS2WS_update:
                                    result = WebMethods.CustomerMethods.UpdateCustomer(customer);
                                    break;
                            }

                            if (result.Length > 0)
                            {
                                switch (XElement.Parse(result).Value)
                                {
                                    case "ok":
                                        cntSuccess++;
                                        msg = String.Format("Customer {0} in webshop: {1} (webshopId={2}, storeId={3})",
                                                            actionVerbPastTense,
                                                            (customer.FirstName + " " + customer.LastName).Trim(),
                                                            customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                            customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}");
                                        this.Out.WriteLine(msg);
                                        log.Info(msg);

                                        customer.SetSyncStatus(true);

                                        break;

                                    case "error: unknown_customer":
                                        cntFail++;
                                        msg = String.Format("Customer to be {0} not found in webshop: {1} (webshopId={2}, storeId={3}{4})",
                                                            actionVerbPastTense,
                                                            (customer.FirstName + " " + customer.LastName).Trim(),
                                                            customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                            customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                            customer.Test ? ", test=1" : "");
                                        this.Out.WriteLine(msg);
                                        log.Info(msg);

                                        if (action == Enums.UpdateActions.customer_SMS2WS_delete ||
                                            action == Enums.UpdateActions.customer_SMS2WS_update && customer.Test)
                                            //if the deletion of a customer or the update for a test customer fails because the customer cannot be found, 
                                            //it is because meanwhile the customer was deleted via the DeleteTestData unit test.
                                            //Because we don't want to keep processing this customer indefinitely, mark it as synched.
                                            customer.SetSyncStatus(true);

                                        break;

                                    default:
                                        cntFail++;
                                        msg = String.Format("Customer could not be {0} in webshop: {1} (webshopId={2}, storeId={3}) : {4}",
                                                            actionVerbPastTense,
                                                            (customer.FirstName + " " + customer.LastName).Trim(),
                                                            customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                            customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                            result);
                                        throw new Exception(msg);
                                }
                            }
                            else
                            {
                                cntFail++;
                                msg = String.Format("Customer could not be {0} in webshop: {1} (webshopId={2}, storeId={3}) : {4}",
                                                    actionVerbPastTense,
                                                    (customer.FirstName + " " + customer.LastName).Trim(),
                                                    customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                    customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}",
                                                    "No return result from web method");
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            cntFail++;
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Customer could not be {0} in webshop: {1} (webshopId={2}, storeId={3}) : {4}",
                                                                                        actionVerbPastTense,
                                                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                                                        customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                                                        customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}", 
                                                                                        exception);
                            OutputSynchronizationError(msg, customer);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} customer in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }



        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the customer object type from WS to SMS
        /// </summary>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeCustomers_ws2sms(DateTime timestampStart)
        {
            string msg;
            bool errorOccurred = false;
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                string errorMsg;
                List<Customer> customersFromWS = WebMethods.CustomerMethods.GetCustomersUpdatedSinceDateTime(timestampStart, out errorMsg);

                if (customersFromWS.Count > 0)
                {
                    foreach (var customer in customersFromWS)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            bool bResult = CustomerRepository.UpdateCustomer(customer);

                            if (bResult)
                            {
                                cntSuccess++;
                                msg = String.Format("Customer updated in SMS: {0} (webshopId={1}, storeId={2})",
                                                    (customer.FirstName + " " + customer.LastName).Trim(),
                                                    customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                    customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}");
                                this.Out.WriteLine(msg);
                                log.Info(msg);

                                customer.SetSyncStatus(true);
                            }
                            else
                            {
                                cntFail++;
                                msg = String.Format("Error while updating customer in SMS: {0} (webshopId={1}, storeId={2})",
                                                    (customer.FirstName + " " + customer.LastName).Trim(),
                                                    customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                    customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}");
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            cntFail++;
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while updating customer in SMS: {0} (webshopId={1}, storeId={2})",
                                                                                        (customer.FirstName + " " + customer.LastName).Trim(),
                                                                                        customer.WebshopId != null ? ((int)customer.WebshopId).ToString() : "{empty}",
                                                                                        customer.StoreId != null ? ((int)customer.StoreId).ToString("D6") : "{empty}");
                            OutputSynchronizationError(msg, customer);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while updating customer in SMS: {0}", exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, Enums.UpdateActions.customer_WS2SMS_update, cntSuccess, cntFail);
            }

            return errorOccurred;
        }

        
        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the instrument object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeInstruments(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var instruments = new List<Instrument>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.instrument_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        instruments = InstrumentRepository.GetDeletedInstrumentsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.instrument_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        instruments = InstrumentRepository.GetUpdatedInstrumentsByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (instruments.Count > 0)
                {
                    foreach (var instrument in instruments)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.instrument_delete:
                                    result = WebMethods.InstrumentMethods.DeleteInstrumentById(instrument.Id);
                                    break;
                                case Enums.UpdateActions.instrument_update:
                                    result = WebMethods.InstrumentMethods.UpdateInstrument(instrument);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                instrument.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("instrument {0}: {1} (id={2})", actionVerbPastTense, instrument.Name_NL, instrument.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("instrument could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, instrument.Name_NL, instrument.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} instrument in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        instrument.Name_NL, 
                                                                                        instrument.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, instrument);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} instrument in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }


        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the manufacturer object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeManufacturers(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var manufacturers = new List<Manufacturer>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.manufacturer_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        manufacturers = ManufacturerRepository.GetDeletedManufacturersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.manufacturer_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        manufacturers = ManufacturerRepository.GetUpdatedManufacturersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (manufacturers.Count > 0)
                {
                    foreach (var manufacturer in manufacturers)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.manufacturer_delete:
                                    result = WebMethods.ManufacturerMethods.DeleteManufacturerById(manufacturer.Id);
                                    break;
                                case Enums.UpdateActions.manufacturer_update:
                                    result = WebMethods.ManufacturerMethods.UpdateManufacturer(manufacturer);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                manufacturer.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("manufacturer {0}: {1} (id={2})", actionVerbPastTense, manufacturer.Name, manufacturer.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("manufacturer could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, manufacturer.Name, manufacturer.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} manufacturer in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        manufacturer.Name, 
                                                                                        manufacturer.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, manufacturer);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} manufacturer in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }



        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the product object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeProducts(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var products = new List<Product>();
            var dictLogIds = new Dictionary<int, string>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;
            int cntNew = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.product_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        products = ProductRepository.GetDeletedProductsByTimestamp(timestampStart, timestampEnd, conn, dictLogIds);
                        break;
                    case Enums.UpdateActions.product_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        products = ProductRepository.GetUpdatedProductsByTimestamp(timestampStart, timestampEnd, conn, dictLogIds);
                        break;
                }

                if (products.Count > 0)
                {
                    foreach (var product in products)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            msg = String.Format("Processing product {0}", product.Id.ToString("D6"));
                            this.Out.WriteLine(msg);
                            log.Info(msg);

                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.product_delete:
                                    result = WebMethods.ProductMethods.DeleteProductById(product.Id);
                                    break;
                                
                                case Enums.UpdateActions.product_update:
                                    //first upload pictures
                                    bool uploadStatus = false;
                                    if (product.ProductPictures.Count > 0)
                                    {
                                        List<ProductPicture> productPicturesToBeUploaded = (from pic in product.ProductPictures
                                                                                            where pic.ToBeUploaded
                                                                                            orderby pic.FileName
                                                                                            select pic).ToList();

                                        if (productPicturesToBeUploaded.Count > 0)
                                        {
                                            uploadStatus = Ftp.UploadProductPictures(productPicturesToBeUploaded);
                                            //if anything went wrong while uploading the product pictures, skip updating this product
                                            if (!uploadStatus)
                                            {
                                                errorOccurred = true;
                                                break;
                                            }
                                        }
                                    }

                                    //next, update product definition
                                    result = WebMethods.ProductMethods.UpdateProduct(product);
                                    break;
                            }

                            if (result.Length > 0)
                            {
                                switch (XElement.Parse(result).Value)
                                {
                                    case "ok":
                                        product.SetSyncStatus(true, dictLogIds);
                                        cntProductsAffected++;
                                        cntSuccess++;
                                        if (product.ActiveInWebshop && product.LogBits.BitTest(Enums.Logfield.ActiveInWebshop))
                                            cntNew++;

                                        msg = String.Format("Product {0} in webshop: {1}", actionVerbPastTense, product.Id.ToString("D6"));
                                        this.Out.WriteLine(msg);
                                        log.Info(msg);

                                        break;

                                    case "error: unknown_product":
                                        cntFail++;
                                        msg = String.Format("Product to be {0} not found in webshop: {1}", actionVerbPastTense, product.Id.ToString("D6"));
                                        this.Out.WriteLine(msg);
                                        log.Info(msg);

                                        if (action == Enums.UpdateActions.product_delete ||
                                            action == Enums.UpdateActions.product_update && product.Test)
                                            //if the deletion of a product or the update for a test product fails because the product cannot be found, 
                                            //it is because meanwhile the product was deleted via the DeleteTestData unit test.
                                            //Because we don't want to keep processing this product indefinitely, mark it as synched.
                                            product.SetSyncStatus(true, dictLogIds);
                                            cntProductsAffected++;

                                        break;

                                    default:
                                        cntFail++;
                                        msg = String.Format("Product could not be {0} in webshop: {1} : {2}", actionVerbPastTense, product.Id.ToString("D6"), result);
                                        throw new Exception(msg);
                                }
                            }
                            else
                            {
                                cntFail++;
                                msg = String.Format("Product could not be {0} in webshop: {1} : {2}", actionVerbPastTense, product.Id.ToString("D6"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            cntFail++;
                            errorOccurred = true;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} product in webshop: {1} : {2}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        product.Id.ToString("D6"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, product);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} product in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail, cntNew);
            }

            return errorOccurred;
        }


        
        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the product category object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeProductCategories(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var productCategories = new List<ProductCategory>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.productCategory_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        productCategories = ProductCategoryRepository.GetDeletedProductCategoriesByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.productCategory_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        productCategories = ProductCategoryRepository.GetUpdatedProductCategoriesByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (productCategories.Count > 0)
                {
                    foreach (var productCategory in productCategories)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.productCategory_delete:
                                    result = WebMethods.ProductCategoryMethods.DeleteProductCategoryById(productCategory.Id);
                                    break;
                                case Enums.UpdateActions.productCategory_update:
                                    result = WebMethods.ProductCategoryMethods.UpdateProductCategory(productCategory);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                productCategory.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("Product category {0}: {1} (id={2})", actionVerbPastTense, productCategory.Name, productCategory.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("Product category could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, productCategory.Name, productCategory.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            cntFail++;
                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} product category in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        productCategory.Name, 
                                                                                        productCategory.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, productCategory);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} product category in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }


        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the product series object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeProductSeries(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var productSeries = new List<ProductSeries>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.productSeries_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        productSeries = ProductSeriesRepository.GetDeletedProductSeriesByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.productSeries_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        productSeries = ProductSeriesRepository.GetUpdatedProductSeriesByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (productSeries.Count > 0)
                {
                    foreach (var series in productSeries)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.productSeries_delete:
                                    result = WebMethods.ProductSeriesMethods.DeleteProductSeriesById(series.Id);
                                    break;
                                case Enums.UpdateActions.productSeries_update:
                                    result = WebMethods.ProductSeriesMethods.UpdateProductSeries(series);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                series.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("Product series {0}: {1} (id={2})", actionVerbPastTense, series.Name, series.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;
                                msg = String.Format("Product series could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, series.Name, series.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            cntFail++;

                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} product series in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        series.Name, 
                                                                                        series.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, series);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} product series in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }


        /// <summary>
        /// Wrapper function that initiates the synchronization methods for the instrument object type
        /// </summary>
        /// <param name="action">Identifying synchronization action</param>
        /// <param name="timestampStart">Time stamp of the oldest record to be synchronized</param>
        /// <param name="timestampEnd">Time stamp of the most recent record to be synchronized</param>
        /// <param name="conn">Active connection</param>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool SynchronizeSuppliers(Enums.UpdateActions action, DateTime timestampStart, DateTime timestampEnd, OleDbConnection conn)
        {
            string msg;
            bool errorOccurred = false;
            var suppliers = new List<Supplier>();
            string actionVerbPastTense = "";
            string actionVerbContinuousTense = "";
            int cntSuccess = 0;
            int cntFail = 0;

            try    //method-level exception handling
            {
                switch (action)
                {
                    case Enums.UpdateActions.supplier_delete:
                        actionVerbPastTense = "deleted";
                        actionVerbContinuousTense = "deleting";
                        suppliers = SupplierRepository.GetDeletedSuppliersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                    case Enums.UpdateActions.supplier_update:
                        actionVerbPastTense = "updated";
                        actionVerbContinuousTense = "updating";
                        suppliers = SupplierRepository.GetUpdatedSuppliersByTimestamp(timestampStart, timestampEnd, conn);
                        break;
                }

                if (suppliers.Count > 0)
                {
                    foreach (var supplier in suppliers)
                    {
                        msg = "";

                        try   //object-level exception handling, continues with next object on error
                        {
                            string result = "";
                            switch (action)
                            {
                                case Enums.UpdateActions.supplier_delete:
                                    result = WebMethods.SupplierMethods.DeleteSupplierById(supplier.Id);
                                    break;
                                case Enums.UpdateActions.supplier_update:
                                    result = WebMethods.SupplierMethods.UpdateSupplier(supplier);
                                    break;
                            }

                            if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                            {
                                supplier.SetSyncStatus(true);
                                cntSuccess++;

                                msg = String.Format("Supplier {0}: {1} (id={2})", actionVerbPastTense, supplier.Name, supplier.Id.ToString("D4"));
                                this.Out.WriteLine(msg);
                                log.Info(msg);
                            }
                            else
                            {
                                cntFail++;

                                msg = String.Format("Supplier could not be {0} in webshop: {1} (id={2}) : {3}", actionVerbPastTense, supplier.Name, supplier.Id.ToString("D4"), result);
                                throw new Exception(msg);
                            }
                        }
                        catch (Exception exception)
                        {
                            errorOccurred = true;
                            cntFail++;

                            msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while {0} supplier in webshop: {1} (id={2}) : {3}", 
                                                                                        actionVerbContinuousTense, 
                                                                                        supplier.Name, 
                                                                                        supplier.Id.ToString("D4"), 
                                                                                        exception);
                            OutputSynchronizationError(msg, supplier);
                        }
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = String.Format("Error while {0} supplier in webshop: {1}", actionVerbContinuousTense, exception);
                OutputSynchronizationError(msg);
            }
            finally
            {
                SyncSessionLogger.WriteResult(sessionId, action, cntSuccess, cntFail);
            }

            return errorOccurred;
        }

        
        /// <summary>
        /// Wrapper function that initiates the recalcultion of the product counts for all product categories
        /// </summary>
        /// <returns>Returns true if no errors were encountered, otherwise returns false</returns>
        private bool UpdateProductCategoryCount()
        {
            string msg = "";
            bool errorOccurred = false;

            try
            {
                if (cntProductsAffected > 0)
                {
                    string result = WebMethods.ProductCategoryMethods.UpdateProductCount();
                    if (result.Length > 0 && XElement.Parse(result).Value == "ok")
                    {
                        msg = String.Format("Product category counts updated in webshop");
                        this.Out.WriteLine(msg);
                        log.Info(msg);
                    }
                    else
                    {
                        msg = String.Format("Error updating product category counts in webshop: {0}", result);
                        throw new Exception(msg);
                    }
                }
                else
                {
                    msg = String.Format("   Nothing to be done.");
                    this.Out.WriteLine(msg);
                    log.Info(msg);
                }
            }
            catch (Exception exception)
            {
                errorOccurred = true;
                msg = msg.Length > 0 ? exception.ToString() : String.Format("Error while updating product category counts in webshop: {0}", 
                                                                            exception);
                this.Error.WriteLine(msg);
                log.Error(msg);
            }

            return errorOccurred;
        }



        /// <summary>
        /// Send a synchronization to one or more output destinations
        /// </summary>
        /// <param name="errorMsg">Error message to be displayed</param>
        /// <param name="syncObject">Synchronization object that caused the error</param>
        private void OutputSynchronizationError(string errorMsg, ISyncObject syncObject)
        {
            this.Error.WriteLine(errorMsg + "\n");
            log.Error(errorMsg);
            if (syncObject != null) log.Debug(syncObject.ToXml());
        }


        private void OutputSynchronizationError(string errorMsg)
        {
            OutputSynchronizationError(errorMsg, null);
        }
    }
}
