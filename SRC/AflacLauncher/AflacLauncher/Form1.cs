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
using log4net;


namespace AflacLauncher
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            log.Debug("Form_Load()");
            log.Debug("Starting TimeOut Timer");
            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 5000;
            log.Debug(string.Format("myTimer.Interval = {0};", myTimer.Interval));
            log.Debug("ReadRegistry();");
            ReadRegistry();
            log.Debug("myTimer.Start();");
            myTimer.Start();

        }


        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {


            log.Debug(string.Format("Checking with Service Checkpoint : {0}",TimerCount));
            TimerCount += 1;

            if ((TimerCount >= TimeOutMax))
            {
                log.Debug("TimerMax Exceeded");
                if (!ResultRecieved)
                {
                    log.Debug("Service did not respond so we assume the current installed version is good.");
                    LaunchApp();
                    Application.Exit();
                }
                //Throw Error
                myTimer.Stop();
                MessageBox.Show("Error : Updating CPS...Please Try Again");
                log.Debug("Error : Updating CPS...Please Try Again");
                Application.Exit();
            }
            if (ServiceError)
            {
                //Throw Error
                myTimer.Stop();
                MessageBox.Show("Error : Updater Service response...Please Try Again");
                log.Debug("Error : Updater Service response...Please Try Again");
                Application.Exit();
            }
            _ = CheckAppAsync();
            if (ResultRecieved)
            {
                if (!AppIsUpdating && !ServiceError)
                {
                    log.Debug(string.Format("AppIsUpdating = {0}", AppIsUpdating));
                    log.Debug(string.Format("ServiceError = {0}", ServiceError));
                    LaunchApp();
                    Application.Exit();
                }
            }
        }
        private void LaunchApp()
        {
            log.Debug(String.Format("Starting Application = {0}", AppToStart));
            System.Diagnostics.Process.Start(AppToStart);
        }
        private async Task CheckAppAsync()
        {
            log.Debug("Entering - private async Task CheckAppAsync()");
            var pipeServer = new NamedPipeClientStream(".", "Aflac.AutoUpdator", PipeDirection.InOut, PipeOptions.Asynchronous);
            log.Debug("var pipeServer = new NamedPipeClientStream()");
            await pipeServer.ConnectAsync();
            log.Debug("await pipeServer.ConnectAsync();");
            var proxy = (IUpdateControler)JsonRpc.Attach(pipeServer).Attach(typeof(IUpdateControler));
            log.Debug("var proxy = (IUpdateControler)JsonRpc.Attach(pipeServer).Attach(typeof(IUpdateControler));");
            //var result = await proxy.HeartBeatAsync(new HeartBeatCriteria() { Info = "Test" });
            var result = await proxy.CheckAndUpdateAsync(new CheckAndUpdateCriteria() { Application = "CPS" });


            ResultRecieved = true;
            log.Debug(string.Format("ResultRecieved = {0}", ResultRecieved));
            log.Debug(string.Format("result.State = {0}", result.State.ToString()));
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
