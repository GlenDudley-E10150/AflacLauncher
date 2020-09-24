using Aflac.Updater.Contract;
using Microsoft.Win32;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AflacLauncher
{
    public partial class Form1 : Form
    {

        bool WaitingTimeOutExpired = false;
        bool AppIsUpdating = false;
        bool ServiceError = false;
        bool ResultRecieved = false;
        string AppToStart = @"C:\Program Files (x86)\AflacApps\CPS\Aflac.ClaimsPaymentSystem.Shell.exe";
        int TimeOutMax = 10;
        int TimerCount = 0;


        System.Windows.Forms.Timer myTimer = new Timer();


        public Form1()
        {
            InitializeComponent();
            this.textBox1.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 1000;
            myTimer.Start();
            ReadRegistry();

        }


        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Checking with Service Checkpoint : {0}",TimerCount));
            TimerCount += 1;

            if ((TimerCount >= TimeOutMax))
            {
                if (!ResultRecieved)
                {
                    //Service did not respond so we assume the current installed version is good.
                    LaunchApp();
                    Application.Exit();
                }
                //Throw Error
                myTimer.Stop();
                MessageBox.Show("Error : Updating CPS...Please Try Again");
                Application.Exit();
            }
            if (ServiceError)
            {
                //Throw Error
                myTimer.Stop();
                MessageBox.Show("Error : Updater Service response...Please Try Again");
                Application.Exit();
            }
            _ = CheckAppAsync();
            if (ResultRecieved)
            {
                if (!AppIsUpdating && !ServiceError)
                {
                    LaunchApp();
                    Application.Exit();
                }
            }
        }
        private void LaunchApp()
        {
            System.Diagnostics.Process.Start(AppToStart);
        }
        private async Task CheckAppAsync()
        {
            //this.Show();
            //Call  Service and Loop until timeout or service respond True
            var pipeServer = new NamedPipeClientStream(".", "Aflac.AutoUpdator", PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeServer.ConnectAsync();
            var proxy = (IUpdateControler)JsonRpc.Attach(pipeServer).Attach(typeof(IUpdateControler));
            //var result = await proxy.HeartBeatAsync(new HeartBeatCriteria() { Info = "Test" });
            var result = await proxy.CheckAndUpdateAsync(new CheckAndUpdateCriteria() { Application = "CPS" });


            ResultRecieved = true;
            MessageBox.Show(result.State.ToString().ToLower());
            switch (result.State.ToString().ToLower())
            {
                case "current":
                    AppIsUpdating = false;
                    break;
                case "updating":
                    AppIsUpdating = true;
                    break;
                case "error":
                    ServiceError = true;
                    break;
                default:
                    AppIsUpdating = false;
                    break;
            }
        }

        private void ReadRegistry()
        {
            //RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Aflac\Updater");
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Aflac\Updater");
            if (key != null)
            {
                string[] availableapp = key.GetSubKeyNames();

                if (key.GetValue("TimeOut") != null)
                {
                    TimeOutMax = int.Parse(key.GetValue("TimeOut").ToString());
                }
            }
        }
    }
}
