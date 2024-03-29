﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using System.Reflection;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Gopher360GameBar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private TaskCompletionSource<bool> bridgeConnectedTcs = new TaskCompletionSource<bool>();
        private bool bridgeLaunched = false;
        private bool bridgeActive
        {
            get { return bridgeConnectedTcs.Task.IsCompleted && bridgeConnectedTcs.Task.Result; }
        }
        private bool gopherActive = false;

        public MainPage()
        {
            this.InitializeComponent();
            App.AppServiceConnected += MainPage_AppServiceConnected;

            // Debugging focus issues in Game Bar
            // https://docs.microsoft.com/en-us/windows/apps/design/input/gamepad-and-remote-interactions
            // this.GotFocus += (object sender, RoutedEventArgs e) =>
            // {
            //     FrameworkElement focus = FocusManager.GetFocusedElement() as FrameworkElement;
            //     if (focus != null)
            //     {
            //         Log("Got focus: " + focus.Name + " (" + focus.GetType().ToString() + ")");
            //     }
            // };

            Assembly assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            String shortVersion = version.Major + "." + version.Minor + "." + version.MajorRevision;
            Log(assembly.GetName().Name + " v" + shortVersion);
            Log("");

            // Since we can't seem to get focus properly in Game Bar
            // start Gopher by default
            StartStopButton_Click(null, null);
        }

        private async void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            bool connected = await ConnectBridge();
            if (!connected)
                return;

            if (gopherActive)
            {
                await StopGopher();
            }
            else
            {
                await StartGopher();
            }
        }

        private async Task<bool> StartGopher()
        {
            bool success = await SendCommandAsync("start");
            if (success)
            {
                gopherActive = true;
                StartStopButton.Content = "Stop Gopher360";
            }

            return success;
        }

        private async Task<bool> StopGopher()
        {
            bool success = await SendCommandAsync("stop");
            if (success)
            {
                gopherActive = false;
                StartStopButton.Content = "Start Gopher360";
            }

            return success;
        }

        private async Task<bool> ConnectBridge()
        {
            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                if (!bridgeLaunched)
                {
                    bridgeLaunched = true;

                    Log("STATUS: \tLaunching Gopher360Bridge...");
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                }

                if (!bridgeActive)
                {
                    // Wait for the connected callback to run
                    await bridgeConnectedTcs.Task;
                }
            }
            else
            {
                Log("FullTrustProcessLauncher is not available on this platform");
                return false;
            }

            return true;
        }

        private async Task<bool> SendCommandAsync(string command)
        {
            Log("COMMAND: \t" + command);

            ValueSet values = new ValueSet();
            values.Add("COMMAND", command);

            AppServiceResponse response = await App.Connection.SendMessageAsync(values);

            if (response.Message == null)
            {
                Log("FAIL: \t\tBridge hung up or sent null response");
                return false;
            }

            object status;
            object msg;
            response.Message.TryGetValue("STATUS", out status);
            response.Message.TryGetValue("MESSAGE", out msg);

            Log(status.ToString() + ":\t\t" + msg.ToString());

            return status.ToString() == "OK";
        }

        private async void Log(string msg)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () => {
                LogBlock.Text = LogBlock.Text + "\n" + msg;
            });
        }

        private void MainPage_AppServiceConnected(object sender, EventArgs args)
        {
            Log("STATUS: \tConnected to Gopher360Bridge");
            bridgeConnectedTcs.TrySetResult(true);
            App.Connection.RequestReceived += Connection_RequestReceived;
        }

        private void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var messageDeferral = args.GetDeferral();

            object status;
            object msg;
            args.Request.Message.TryGetValue("STATUS", out status);
            args.Request.Message.TryGetValue("MESSAGE", out msg);

            Log(status?.ToString() + ":\t\t" + msg?.ToString());

            messageDeferral.Complete();
        }
    }
}
