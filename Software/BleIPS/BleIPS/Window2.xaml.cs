using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BleIPS
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : MetroWindow
    {
        System.Timers.Timer tim;
        DateTime loggingTime;
        DateTime loggingLength;
        MainWindow win;

        public Window2(MainWindow window)
        {
            InitializeComponent();

            win = window;
            tim = new System.Timers.Timer(100);
            tim.Elapsed += Tim_Elapsed;
        }

        private void Tim_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            loggingTime = loggingTime.AddMilliseconds(100);
            Dispatcher.BeginInvoke((Action)delegate
            {
                TextBlockTime.Text = loggingTime.ToString("mm:ss:ff");
                win.SaveData();

                double.TryParse(TextBoxLogLength.Text, out double loggingLengthSeconds);

                loggingLength = new DateTime();
                loggingLength = loggingLength.AddSeconds(loggingLengthSeconds);

                if (loggingLength.Subtract(loggingTime).TotalSeconds < 0)
                {
                    tim.Stop();
                    stop = false;
                    ButtonLogging.Content = "Start Logging";
                }
            });


           
        }

        double distace;
        

        void HistoryAddEntry(string entry)
        {
            TextBoxHistory.Text += entry + Environment.NewLine;
            TextBoxHistory.ScrollToEnd();
        }

        bool stop;
        private void ButtonLogging_Click(object sender, RoutedEventArgs e)
        {
            if (stop == false)
            {
                if (double.TryParse(TextBoxDistace.Text, out distace))
                {
                    loggingTime = new DateTime();
                    currentStep = int.Parse(TextBoxIncreaseXAfterYSteps.Text) - 1;

                    distanceY = double.Parse(TextBoxDistaceY.Text);

                    tim.Start();
                    win.SetPath("dist." + distace.ToString() + "mX" + distanceY.ToString() + "m");
                    HistoryAddEntry("Starting new log at distacne: " + TextBoxDistace.Text + " m x " + TextBoxDistaceY.Text + " m");
                }

                stop = true;
                ButtonLogging.Content = "Stop Logging";
            }
            else
            {
                stop = false;
                ButtonLogging.Content = "Start Logging";
                tim.Stop();
            }
            
        }


        int currentStep;
        double distanceY;
        private void ButtonDistanceIncrese_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 0)
            {
                currentStep--;
                distanceY += 0.5;
                TextBoxDistaceY.Text = distanceY.ToString();
            }
            else
            {
                distanceY = double.Parse(TextBoxYStart.Text);
                TextBoxDistaceY.Text = distanceY.ToString();
                distace += 0.5;
                TextBoxDistace.Text = distace.ToString();
                currentStep = int.Parse(TextBoxIncreaseXAfterYSteps.Text) - 1;
            }
            loggingTime = new DateTime();
            
            win.SetPath("dist." + distace.ToString() + "mX" + distanceY.ToString() + "m");
            tim.Start();
            HistoryAddEntry("Starting new log at distacne: " + TextBoxDistace.Text + " m x " + TextBoxDistaceY.Text + " m");
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }
    }
}
