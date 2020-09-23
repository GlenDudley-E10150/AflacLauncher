using Aflac.Updater.Contract;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AflacLauncher
{
    public partial class Form1 : Form
    {

        bool WaitingTimeOutExpired = false;
        bool AppIsUpdating = false;
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
        }


        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("Checking with Service Checkpoint : {0}",TimerCount));
            _ = CheckAppAsync();
            TimerCount += 1;

            if (TimerCount >= TimeOutMax)
            {
                //Throw Error
                myTimer.Stop();
                MessageBox.Show("Error : Updating CPS...Please Try Again");
                Application.Exit();
            }
            if (!AppIsUpdating)
            {
                LaunchApp();
                Application.Exit();
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
            if (result.State.Equals("Current"))
            {
                AppIsUpdating = false;                
            }
            else
            {
                AppIsUpdating = true;
            }
        }


    }
}
