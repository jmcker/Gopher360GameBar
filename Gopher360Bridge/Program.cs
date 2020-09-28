using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace Gopher360Bridge
{
    class Program
    {
        static AppServiceConnection connection = null;
        static AutoResetEvent appServiceExit;

        static string home;
        static string installationFolder;
        static string assetFolder;
        static string gopherPath;
        static Process gopherProcess = null;

        static void Main(string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Name + " v" + assembly.GetName().Version);
            Console.WriteLine("");

            home = Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            installationFolder = Path.GetDirectoryName(assembly.Location);
            assetFolder = Path.Combine(installationFolder, "Assets");
            gopherPath = Path.Combine(assetFolder, "Gopher.exe");

            Console.WriteLine("CWD: \t" + Directory.GetCurrentDirectory());
            Console.WriteLine("Home: \t" + home);
            Console.WriteLine("Install: " + installationFolder);
            Console.WriteLine("Gopher:  " + gopherPath);
            Console.WriteLine("");

            appServiceExit = new AutoResetEvent(false);
            InitializeAppServiceConnection();
            appServiceExit.WaitOne();
        }

        static async void InitializeAppServiceConnection()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "Gopher360AppService";
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                Console.WriteLine("FAIL: \tApp service connection failed with status: " + status);
            }
        }

        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            // signal the event so the process can shut down
            appServiceExit.Set();
            Console.WriteLine("CLOSE: \tApplication connection closed");
        }

        private async static void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            object objValue;
            args.Request.Message.TryGetValue("COMMAND", out objValue);
            string command = objValue as string;
            string msg;
            string status;

            Console.WriteLine("COMMAND: \t" + command);

            switch (command)
            {
                case "start":
                    if (gopherProcess != null && !gopherProcess.HasExited)
                    {
                        status = "OK";
                        msg = "Gopher360 already running with PID " + gopherProcess.Id;
                        break;
                    }

                    try
                    {
                        // Stop other external Gopher processes
                        StopAllExistingGopherProcesses();

                        gopherProcess = StartGopherProcess();

                        if (gopherProcess.HasExited)
                        {
                            status = "FAIL";
                            msg = "Gopher360 crashed";
                            break;
                        }

                        status = "OK";
                        msg = "Gopher360 started with PID " + gopherProcess.Id;
                    }
                    catch (Exception e)
                    {
                        status = "FAIL";
                        msg = e.ToString();
                    }

                    break;

                case "stop":
                    if (gopherProcess == null)
                    {
                        status = "OK";
                        msg = "Gopher360 was not running";
                    }
                    else if (gopherProcess.HasExited)
                    {
                        status = "OK";
                        msg = "Gopher360 has already exited";
                    }
                    else
                    {
                        try
                        {
                            gopherProcess.Kill();
                            status = "OK";
                            msg = "Stopped Gopher360";
                        }
                        catch (Exception e)
                        {
                            status = "FAIL";
                            msg = e.ToString();
                        }
                    }

                    break;

                default:
                    status = "FAIL";
                    msg = "Unknown command '" + command + "'";
                    break;
            }

            ValueSet response = PrepMessage(status, msg);

            try
            {
                await args.Request.SendResponseAsync(response);
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                messageDeferral.Complete();
            }
        }

        private static Process StartGopherProcess()
        {
            ProcessStartInfo pi = new ProcessStartInfo();
            pi.FileName = gopherPath;
            pi.WorkingDirectory = home;
            pi.UseShellExecute = false;
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;
            pi.CreateNoWindow = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;

            Process p = Process.Start(pi);

            // Log the output to the UWP display
            // Task.Run(() => {
            //     while (!p.StandardOutput.EndOfStream)
            //     {
            //         string line = p.StandardOutput.ReadLine();
            //         Console.WriteLine(line);
            //         SendMessage("LOG", line);
            //     }
            // });
            // Task.Run(() => {
            //     while (!p.StandardError.EndOfStream)
            //     {
            //         string line = p.StandardError.ReadLine();
            //         Console.WriteLine(line);
            //         SendMessage("LOG", line);
            //     }
            // });

            return p;
        }

        private static void StopAllExistingGopherProcesses()
        {
            foreach (Process p in Process.GetProcessesByName("Gopher"))
            {
                SendMessage("LOG", "Stopping existing Gopher360 instance with PID " + p.Id);
                p.Kill();
            }
        }

        private static void SendMessage(string status, string msg)
        {
            ValueSet prepped = PrepMessage(status, msg);
            _ = connection.SendMessageAsync(prepped); // Don't await since we don't care about the response
        }

        private static ValueSet PrepMessage(string status, string msg)
        {
            ValueSet response = new ValueSet();
            response.Add("STATUS", status);
            response.Add("MESSAGE", msg);

            Console.WriteLine(status + ": \t" + msg);

            return response;
        }
    }
}
