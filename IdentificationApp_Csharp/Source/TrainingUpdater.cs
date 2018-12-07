using System;
using System.Timers;

namespace IdentificationApp.Source
{
    class TrainingUpdater
    {
        private Timer timer;
        private MainWindow main;
        private TimeSpan updateTime;
        private bool needUpdate;
        TimeSpan now = DateTime.Now.TimeOfDay;

        public TrainingUpdater(MainWindow main)
        {
            this.main = main;
            needUpdate = false;
            updateTime = new TimeSpan(23, 00, 0);
            timer = new Timer(60000)
            {
                AutoReset = true
            };
            timer.Elapsed += new ElapsedEventHandler(Check);
            timer.Start();
        }

        public void CheckForUpdate()
        {
            needUpdate = true;
        }

        private void Check(object sender, EventArgs e)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            if (needUpdate && now > updateTime && main.GetStage() == MainWindow.CaptureStage.Idle)
            {
                needUpdate = false;
                main.StartTraining();
            }
        }
    }
}
