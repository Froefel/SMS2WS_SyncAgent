using System;
using System.Configuration;

namespace SMS2WS_SyncAgent
{
    class Program
    {
        private static Log4NetWrapper log = LogManager.GetLogger();

        [STAThread]
        static void Main(string[] args)
        {
            int tmp;
            int.TryParse(ConfigurationManager.AppSettings["WaitBetweenSyncSessionsInSeconds"], out tmp);
            if (tmp == 0) tmp = 60;
            AppSettings.WaitBetweenSyncSessionsInSeconds = tmp;
            int waitTimeInSeconds = AppSettings.WaitBetweenSyncSessionsInSeconds;

            try
            {
                string msg;
                string appTitle = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " +
                                  "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                Console.Title = appTitle;

                log.InfoFormat("********** Application started: {0}  ({1})", appTitle, Environment.CommandLine);
                log.InfoFormat("Using database connection \"{0}\"", DAL.ConnectionString);
                Utility.UpdatePresence("");

                bool rerun = true;

                while (rerun)
                {
                    bool wasConnected = true;

                    while (true)
                    {
                        bool isConnected = Utility.IsConnectedToInternet();
                        
                        if (!wasConnected && isConnected)
                        {
                            msg = "Connection to the Internet restored.";
                            log.Info(msg);
                            Console.WriteLine(msg);
                            Utility.UpdatePresence(msg);
                        }
                        wasConnected = isConnected;

                        if (isConnected)
                        {
                            msg = ">>>>> New synchronization session started";
                            log.Info(msg);
                            Console.WriteLine(msg + " at {0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                            // run the main synchronization process
                            var engine = new AppEngine();
                            try
                            {
                                engine.Main(args);

                                msg = ">>>>> Synchronization session ended";
                                log.Info(msg);
                                Console.WriteLine(msg);
                            }
                            catch (Exception exception)
                            {
                                msg = "Error in main process\n" + exception.ToString();
                                log.Error(exception.ToString());
                                Console.WriteLine(msg);
                                //throw;
                            }
                            finally
                            {
                                Console.WriteLine("\nPress 'R' or 'r' to rerun the synchronization immediately,\n" +
                                                  "press 'P' or 'p' to pause the application,\n" +
                                                  "press 'C' or 'c' to close the application,\n" +
                                                  "or wait for the next automatic synchronization.");
                            }
                        }
                        else
                        {
                            msg = "Error: No internet connection available.";
                            log.Error(msg);
                            Console.WriteLine("\n" + msg + "\n" +
                                              "Press 'R' or 'r' to retry to connect immediately,\n" +
                                              "press 'C' or 'c' to close the application,\n" +
                                              "or wait until the application detects a connection again.\n");
                            Utility.UpdatePresence(msg);
                        }

                        // start a loop in which we accept certain key strokes
                        bool validKeyPressed = false;
                        bool pauseApplication = false;
                        int waitProgressInSeconds = 0;

                        while (true)
                        {
                            if (Console.KeyAvailable)
                            {
                                ConsoleKeyInfo c = Console.ReadKey(true);
                                switch (c.Key)
                                {
                                    case ConsoleKey.P:
                                        validKeyPressed = true;
                                        pauseApplication = true;
                                        break;
                                    case ConsoleKey.R:
                                        validKeyPressed = true;
                                        pauseApplication = false;
                                        break;
                                    case ConsoleKey.C:
                                        rerun = false;
                                        validKeyPressed = true;
                                        pauseApplication = false;
                                        break;
                                }
                            }

                            //if the user pressed a valid key stroke, jump out of the loop
                            if (validKeyPressed && !pauseApplication)
                                break;

                            if (!pauseApplication)
                            {
                                //otherwise continue the countdown
                                if (isConnected)
                                    Console.Write("\rThe next synchronization will be run in {0:D2} seconds.", (waitTimeInSeconds - waitProgressInSeconds));
                                                   //The next synchronization will be run in 60 seconds.
                                                   //The application is being paused: 00:00:00          
                                else
                                    Console.Write("\rRetrying to connect to the internet in {0:D2} seconds.", (waitTimeInSeconds - waitProgressInSeconds));


                                System.Threading.Thread.Sleep(1000); //sleep for 1 second

                                if (waitProgressInSeconds++ >= waitTimeInSeconds)
                                    break;
                            }
                            else
                            {
                                var span = new TimeSpan(0, 0, waitProgressInSeconds);
                                msg = "The application is being paused";
                                Console.Write("\r{0}: {1}           ", msg, 
                                                                       String.Format("{0:0}:{1:00}:{2:00}", span.Hours, span.Minutes, span.Seconds));
                                Utility.UpdatePresence(msg);

                                System.Threading.Thread.Sleep(1000); //sleep for 1 second
                                waitProgressInSeconds++;
                            }
                        }

                        if (rerun == false)
                            break;

                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }
            }

            catch (Exception ex)
            {
                string msg = String.Format("Fatal error causing application to terminate: {0}", ex);
                Console.WriteLine(msg);
                log.Fatal(msg);
            }

            finally
            {
                log.Info("********** Application ended");
            }
        }
    }
}
