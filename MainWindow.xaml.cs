using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace SerialPortAPP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        SerialPortObject portCOM1, portCOM2;

        public MainWindow()
        {
            // string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"app.ico");
            // Icon ico = new System.Drawing.Icon(path);

            // Uri iconUri = new Uri("pack://application:,,,/app.ico", UriKind.RelativeOrAbsolute);
            // this.Icon = BitmapFrame.Create(iconUri);            

            InitializeComponent();
            Closing += Window_OnClosing;

            this.UpdatePortName();

            cbCOM1.Tag = rtb1;
            cbCOM2.Tag = rtb2;

            bt1.Tag = cbCOM1;
            bt2.Tag = cbCOM2;

            rtb1.Tag = TYPE.LISTENER;
            rtb2.Tag = TYPE.WRITER;
        }

        void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string name = (sender as Button)?.Name;

            if (!button.Content.Equals("Execute"))
            {
                var portCom = name.Equals("bt1") ? portCOM1 : portCOM2;
                if (portCom == null || !portCom.IsOpen) return;
                bool status = button.Content.Equals("Pause") ? false : true;
                if (!status) portCom.PauseThreadSend(); else portCom.ResumeThreadSend();
                button.Content = status ? "Pause" : "Resume";
                return;
            }

            if (button.IsEnabled)
            {
                ComboBox cb = (ComboBox)(button.Tag);
                if (cb.SelectedIndex < 0 && (string)cb.SelectedValue != "") return;
                RichTextBox rtb = (RichTextBox)cb.Tag;
                if (name.Equals("bt1"))
                {
                    portCOM1 = new SerialPortObject((string)cb.SelectedValue, (TYPE)rtb.Tag);
                }
                else
                {
                    portCOM2 = new SerialPortObject((string)cb.SelectedValue, (TYPE)rtb.Tag);
                }

                var portCom = name.Equals("bt1") ? portCOM1 : portCOM2;
                if (portCom == null || !portCom.IsOpen) return;

                if (rtb.Tag.Equals(TYPE.LISTENER))
                {
                    button.IsEnabled = false;
                    portCom.messageReceived += (m) =>
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            rtb.Document.Blocks.Clear();
                            rtb.AppendText(m + (char)0x0D);
                        }));
                    };
                }
                else
                {
                    button.Content = "Pause";
                    portCom.messageSent += (m) =>
                    {
                        Dispatcher.BeginInvoke((Action)(() =>
                        {
                            rtb.AppendText(m + (char)0x0D);
                            rtb.ScrollToEnd();
                        }));
                    };
                }
                cb.IsEnabled = false;
                this.UpdatePortName();
            }
        }

        private void UpdatePortName()
        {
            var portNames = SerialPort.GetPortNames();
            List<string> portList = new List<string>();
            foreach (var port in portNames)
            {
                if (isValidPortCOM(port)) portList.Add(port);
            }

            if (cbCOM1.IsEnabled) cbCOM1.ItemsSource = portList.ToArray();
            if (cbCOM2.IsEnabled) cbCOM2.ItemsSource = portList.ToArray();
        }

        private bool isValidPortCOM(string portName)
        {
            try
            {
                SerialPort portCOM = new SerialPort(portName);
                portCOM.Open();
                portCOM.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening port: {0}", ex.Message);
            }

            return false;
        }

        public void Window_OnClosing(object sender, CancelEventArgs e)
        {
            portCOM1?.Dispose();
            portCOM2?.Dispose();
            Application.Current.Shutdown();            
        }
    }
}
