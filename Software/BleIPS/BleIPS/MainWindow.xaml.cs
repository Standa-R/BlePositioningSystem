using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MahApps.Metro.Controls;

namespace BleIPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private UdpClient commServer;

        public ObservableCollection<LocReport> locReports;
        //public ObservableCollection<AdvAddres> advAddres;

        ObservableCollection<LocAnchor> anchors;
        //LocAnchor anchor;

        Window2 logControl;

        IPAddress IP { get; set; }


        struct Anchor
        {
            public ulong Eui;
            public int[] Chs;
            public int[] Rssis;
        }

        struct CompleteReport
        {
            public DateTime Received;
            public DateTime Time;
            public ulong AdvAddr;
            public Anchor[] Anchors;
            public int AnchorsCnt;
        }

        List<CompleteReport> completeReports;

        public MainWindow()
        {
            InitializeComponent();

            anchors = new ObservableCollection<LocAnchor>();
            //anchor = new LocAnchor();

            locReports = new ObservableCollection<LocReport>();
            DataGridIPS.ItemsSource = locReports;

            ListBoxAnchors.ItemsSource = anchors;
            //advAddres = new ObservableCollection<AdvAddres>();

            //commServer.JoinMulticastGroup()
            logControl = new Window2(this);

            completeReports = new List<CompleteReport>();

            IP = IPAddress.Parse("172.25.96.35");
            //IP = IPAddress.Parse("10.59.9.29");
            //IP = IPAddress.Parse("192.168.1.200");

            try
            {
                commServer = new UdpClient();
                commServer.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                commServer.Client.Bind(new IPEndPoint(IPAddress.Any, 50000));
                commServer.JoinMulticastGroup(IPAddress.Parse("239.255.0.200"));
                ServerListening();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Weapon dicovery requires listenig to UDP port 50 000. This UDP Port is already used! Therefore, Weapon discovery will not show any weapons.");
            }

        }


        private async void ServerListening()
        {
            //OnServerStart(this.udpPort);
            while (true)
            {
                try
                {
                    var receivedResults = await commServer.ReceiveAsync();
                    OnDataReception(receivedResults.RemoteEndPoint, receivedResults.Buffer);
                }
                catch (ObjectDisposedException ex)                      //when UDP client is closed while listening for data the Dispose Exception is fired; so catch it
                {
                    //System.Windows.MessageBox.Show(ex.ToString());
                    break;
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                    
                    commServer.Close();
                    break;
                }
            }
            //OnServerStop(this.udpPort);
        }

        DateTime lastScroll = DateTime.Now;
        bool autoScroll = true;
        double[] rssiData = new double[300000];
        int rssiDataIndex;
        protected virtual void OnDataReception(IPEndPoint ip, byte[] receivedData)
        {
            if (receivedData.Length >= 32)
            {
                if ((receivedData[0] == (byte)'B') && (receivedData[1] == (byte)'L') && (receivedData[2] == (byte)'L') && (receivedData[3] == (byte)'\n'))
                {
                    int[] rssi = new int[4];
                    int[] channel = new int[4];

                    rssi[0] = (sbyte)receivedData[29];
                    rssi[1] = (sbyte)receivedData[30];
                    rssi[2] = (sbyte)receivedData[31];
                    rssi[3] = (sbyte)receivedData[32];
                                       
                    channel[0] = (sbyte)receivedData[25];
                    channel[1] = (sbyte)receivedData[26];
                    channel[2] = (sbyte)receivedData[27];
                    channel[3] = (sbyte)receivedData[28];

                    ulong advAddr = BitConverter.ToUInt64(receivedData, 13);
                    ulong eui = BitConverter.ToUInt64(receivedData, 5);
                    
                    LocReport locReport = new LocReport(BitConverter.ToUInt32(receivedData, 21),eui, advAddr, rssi, channel);

                    //BuildCompleteReport(locReport);
                    BuildCompleteReportSelf(locReport);

                    bool notListed = true;
                    int i;
                    for (i = 0; i < anchors.Count; i++)
                    {
                        if (anchors[i].Eui64 == eui)
                        {
                            notListed = false;
                            break;
                        }
                    }
                    if (notListed)
                    {
                        anchors.Add(new LocAnchor(eui));
                        anchors[anchors.Count - 1].ProcessReport(locReport);
                        anchors[anchors.Count - 1].ProcessAnchorState(receivedData[4]);
                    }
                    else
                    {
                        anchors[i].ProcessReport(locReport);
                        anchors[i].ProcessAnchorState(receivedData[4]);
                    }


                    locReports.Add(locReport);
                    if (autoScroll)
                    {
                        if ((DateTime.Now - lastScroll).TotalMilliseconds > 50)
                        {
                            isUserScroll = false;
                            DataGridIPS.ScrollIntoView(DataGridIPS.Items[DataGridIPS.Items.Count - 1]);
                            lastScroll = DateTime.Now;
                        }
                    }
                    TextBlockMessagesCount.Text = string.Format("{0}/\u200B{1}", DataGridIPS.SelectedItems.Count, DataGridIPS.Items.Count);
                }
            }
            else if (receivedData.Length == 13)
            {
                ulong eui = BitConverter.ToUInt64(receivedData, 5);
                bool notListed = true;
                int i;

                for (i = 0; i < anchors.Count; i++)
                {
                    if (anchors[i].Eui64 == eui)
                    {
                        notListed = false;
                        break;
                    }
                }
                if (notListed)
                {
                    anchors.Add(new LocAnchor(eui));
                    //anchors[anchors.Count - 1].ProcessReport(locReport);
                    anchors[anchors.Count - 1].ProcessAnchorState(receivedData[4]);
                }
                else
                {
                    //anchors[i].ProcessReport(locReport);
                    anchors[i].ProcessAnchorState(receivedData[4]);
                }
            }
        }


        int EuiToIndex(ulong eui)
        {
            switch(eui)
            {
                case 0xD6F000E285E00:
                    return 0;
                    
                case 0xD6F000E29165A:
                    return 1;

                case 0xD6F000E292DF9:
                    return 2;

                case 0xD6F000E29384D:
                    return 3;
            }
            return 4;
        }


        void BuildCompleteReportSelf(LocReport locReport)
        {
            int[] chnls = new int[4];
            int[] rssis = new int[4];


            chnls[0] = locReport.Ant1Ch;
            chnls[1] = locReport.Ant2Ch;
            chnls[2] = locReport.Ant3Ch;
            chnls[3] = locReport.Ant4Ch;

            rssis[0] = locReport.Ant1;
            rssis[1] = locReport.Ant2;
            rssis[2] = locReport.Ant3;
            rssis[3] = locReport.Ant4;

            Anchor anchor = new Anchor() { Eui = locReport.AdvAddr, Chs = chnls, Rssis = rssis };

            if (completeReports.Count == 0)
            {
                Anchor[] anchors = new Anchor[4];

                for (int i = 0; i < 4; i++)
                {
                    anchors[i].Chs = new int[4];
                    anchors[i].Rssis = new int[4];
                }

                int anchorNumber = (int)anchor.Eui & 0x03;
                anchors[anchorNumber] = anchor;
                completeReports.Add(new CompleteReport() { Received = DateTime.Now, Time = locReport.Time, AdvAddr = locReport.AdvAddr, Anchors = anchors });
            }
            else
            {
                int i;
                if (Math.Abs(completeReports[completeReports.Count - 1].Time.Subtract(locReport.Time).TotalMilliseconds) > 120)     //reports received 100 ms later then 1st report
                {
                    int goFrom = completeReports.Count - 80;
                    if (goFrom < 0)
                    {
                        goFrom = 0;
                    }

                    for (i = goFrom; i < completeReports.Count; i++)
                    {
                        double ms = completeReports[i].Time.Subtract(locReport.Time).TotalMilliseconds;
                        if (Math.Abs(ms) < 200)
                        {
                            int anchorNumberAux = (int)anchor.Eui & 0x03;
                            if (completeReports[i].Anchors[anchorNumberAux].Rssis[0] != 0 )
                            {
                                //Console.WriteLine("REPORT OVERWRITTEN!");
                                break;
                            }
                            else
                            {
                                //Console.WriteLine("Correct ADD");
                            }

                            completeReports[i].Anchors[anchorNumberAux] = anchor;
                            Console.WriteLine("Adding report form " + anchor.Eui.ToString("X") + " to " + completeReports[i].Time.ToString("HH:mm:ss:fff") + "; -" + Math.Abs(completeReports[completeReports.Count - 1].Time.Subtract(locReport.Time).TotalMilliseconds) + " ms");
                            return;
                        }
                    }



                    Anchor[] anchors = new Anchor[4];
                    for (int j = 0; j < 4; j++)
                    {
                        anchors[j].Chs = new int[4];
                        anchors[j].Rssis = new int[4];
                    }

                    int anchorNumber = (int)anchor.Eui & 0x03;
                    anchors[anchorNumber] = anchor;
                    completeReports.Add(new CompleteReport() { Received = DateTime.Now, Time = locReport.Time, AdvAddr = locReport.AdvAddr, Anchors = anchors });



                }
                else
                {
                    int anchorNumber = (int)anchor.Eui & 0x03;
                    completeReports[completeReports.Count - 1].Anchors[anchorNumber] = anchor;
                }

            }
        }


        void BuildCompleteReport(LocReport locReport)
        {
            int[] chnls = new int[4];
            int[] rssis = new int[4];
            

            chnls[0] = locReport.Ant1Ch;
            chnls[1] = locReport.Ant2Ch;
            chnls[2] = locReport.Ant3Ch;
            chnls[3] = locReport.Ant4Ch;

            rssis[0] = locReport.Ant1;
            rssis[1] = locReport.Ant2;
            rssis[2] = locReport.Ant3;
            rssis[3] = locReport.Ant4;

            Anchor anchor = new Anchor() { Eui = locReport.Eui, Chs = chnls, Rssis = rssis };

            if (completeReports.Count == 0)
            {
                Anchor[] anchors = new Anchor[5];

                for (int i = 0; i < 4; i++)
                {
                    anchors[i].Chs = new int[4];
                    anchors[i].Rssis = new int[4];
                }


                anchors[EuiToIndex(anchor.Eui)] = anchor;
                completeReports.Add(new CompleteReport() {Received = DateTime.Now, Time = locReport.Time, AdvAddr = locReport.AdvAddr, Anchors = anchors });
            }
            else
            {
                int i;
                
                if (Math.Abs(completeReports[completeReports.Count - 1].Time.Subtract(locReport.Time).TotalMilliseconds) > 40)
                {
                    int goFrom = completeReports.Count - 80;
                    if (goFrom < 0)
                    {
                        goFrom = 0;
                    }

                    for (i = goFrom; i < completeReports.Count; i++)
                    {
                        double ms = completeReports[i].Time.Subtract(locReport.Time).TotalMilliseconds;
                        if (Math.Abs(ms) < 40)
                        {
                            completeReports[i].Anchors[EuiToIndex(anchor.Eui)] = anchor;
                            Console.WriteLine("Adding report form " + anchor.Eui.ToString("X") + " to " + completeReports[i].Time.ToString("HH:mm:ss:fff") + "; -" + Math.Abs(completeReports[completeReports.Count - 1].Time.Subtract(locReport.Time).TotalMilliseconds) + " ms");
                            return;
                        }
                    }

                    Anchor[] anchors = new Anchor[5];
                    for (int j = 0; j < 4; j++)
                    {
                        anchors[j].Chs = new int[4];
                        anchors[j].Rssis = new int[4];
                    }

                    anchors[EuiToIndex(anchor.Eui)] = anchor;
                    completeReports.Add(new CompleteReport() { Received = DateTime.Now, Time = locReport.Time, AdvAddr = locReport.AdvAddr, Anchors = anchors });

                }
                else
                {
                    completeReports[completeReports.Count - 1].Anchors[EuiToIndex(anchor.Eui)] = anchor;
                }
            }

            
        }


        private void ButtonAutoScroll_Click(object sender, RoutedEventArgs e)
        {
            autoScroll = !autoScroll;
        }


        string Path;

        public void SetPath(string path)
        {
            Path = path;
            completeReports.Clear();
        }


        public void SaveData()
        {
            int range = 0;
            StringBuilder sb = new StringBuilder();

            //range = completeReports.Count - 1;

            for (int i = 0; i < completeReports.Count; i++)
            {
                if (DateTime.Now.Subtract(completeReports[i].Received).TotalMilliseconds >= 8000)
                {
                    range = i;
                }
            }

            if (range > 0)
            {
                for (int i = 0; i < range; i++)
                {
                    if(completeReports[i].Anchors[0].Eui != 0 && completeReports[i].Anchors[1].Eui != 0 && completeReports[i].Anchors[2].Eui != 0 && completeReports[i].Anchors[3].Eui != 0)
                    {
                        sb.AppendLine(string.Join(",", completeReports[i].Time.ToString("HH:mm:ss:fff"), completeReports[i].AdvAddr.ToString("X"), 
                            completeReports[i].Anchors[0].Eui.ToString("X"),
                            completeReports[i].Anchors[1].Eui.ToString("X"),
                            completeReports[i].Anchors[2].Eui.ToString("X"),
                            completeReports[i].Anchors[3].Eui.ToString("X"),

                            completeReports[i].Anchors[0].Chs[0], completeReports[i].Anchors[0].Chs[1], completeReports[i].Anchors[0].Chs[2], completeReports[i].Anchors[0].Chs[3],
                            completeReports[i].Anchors[0].Rssis[0], completeReports[i].Anchors[0].Rssis[1], completeReports[i].Anchors[0].Rssis[2], completeReports[i].Anchors[0].Rssis[3],

                            
                            completeReports[i].Anchors[1].Chs[0],   completeReports[i].Anchors[1].Chs[1],   completeReports[i].Anchors[1].Chs[2],   completeReports[i].Anchors[1].Chs[3],
                            completeReports[i].Anchors[1].Rssis[0], completeReports[i].Anchors[1].Rssis[1], completeReports[i].Anchors[1].Rssis[2], completeReports[i].Anchors[1].Rssis[3],

                            
                            completeReports[i].Anchors[2].Chs[0],   completeReports[i].Anchors[2].Chs[1],   completeReports[i].Anchors[2].Chs[2],   completeReports[i].Anchors[2].Chs[3],
                            completeReports[i].Anchors[2].Rssis[0], completeReports[i].Anchors[2].Rssis[1], completeReports[i].Anchors[2].Rssis[2], completeReports[i].Anchors[2].Rssis[3],

                            
                            completeReports[i].Anchors[3].Chs[0],   completeReports[i].Anchors[3].Chs[1],   completeReports[i].Anchors[3].Chs[2],   completeReports[i].Anchors[3].Chs[3],
                            completeReports[i].Anchors[3].Rssis[0], completeReports[i].Anchors[3].Rssis[1], completeReports[i].Anchors[3].Rssis[2], completeReports[i].Anchors[3].Rssis[3]

                            ));
                    }
                    else
                    {
                        Console.WriteLine("Dicarting report " + completeReports[i].Time.ToString("HH:mm:ss:fff") + " missing: " + (completeReports[i].Anchors[0].Eui != 0 ? "": "A0")  +
                                                                                                                                (completeReports[i].Anchors[1].Eui != 0 ? "" : "A1") +
                                                                                                                                (completeReports[i].Anchors[2].Eui != 0 ? "" : "A2") +
                                                                                                                                (completeReports[i].Anchors[3].Eui != 0 ? "" : "A3"));
                    }
                }

                try
                {
                    File.AppendAllText(System.IO.Path.Combine("a:\\IPS\\", Path + ".txt"), sb.ToString());
                    //File.WriteAllText(sfd.FileName, ); ;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("File could not be open for write operation.");
                    return;
                }

                completeReports.RemoveRange(0, range);
            }

        }



        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            locReports.Clear();
            for (int i = 0; i < anchors.Count; i++)
            {
                anchors[i].Tags.Clear();
            }
            anchors.Clear();
        }

        private void ListBoxAdvAddrs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxAdvAddrs.SelectedItem != null)
            {
                //anchors[ListBoxAnchors.SelectedIndex].Tags[ListBoxAdvAddrs.SelectedIndex].PropertyChanged += MainWindow_PropertyChanged;
                Window1 win = new Window1(anchors[ListBoxAnchors.SelectedIndex].Eui64, anchors[ListBoxAnchors.SelectedIndex].Tags[ListBoxAdvAddrs.SelectedIndex]);
                win.Show();
                //double[] plotArray = new double[rssiDataIndex];
                //Array.Copy(rssiData, plotArray, rssiDataIndex);
                //win.PlotData(plotArray);
            }
        }


        int repCnt;
        //private void MainWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    Console.WriteLine("GotReport" + ((LocTag)sender).ReportsCount);
        //}

        bool isUserScroll = true;
        private void DataGridIPS_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (e.VerticalChange == 0.0)
                return;

            if (isUserScroll)
            {
                if (e.VerticalChange > 0.0)
                {
                    double scrollerOffset = e.VerticalOffset + e.ViewportHeight;
                    if (Math.Abs(scrollerOffset - e.ExtentHeight) < 5.0)
                    {
                        // The user has tried to move the scroll to the bottom, activate autoscroll.
                        autoScroll = true;
                        ButtonAutoScroll.IsChecked = true;
                    }
                }
                else
                {
                    // The user has moved the scroll up, deactivate autoscroll.
                    autoScroll = false;
                    ButtonAutoScroll.IsChecked = false;
                }
            }
            isUserScroll = true;
        }

        private void ListBoxAnchors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxAnchors.SelectedIndex != -1)
            {
                ListBoxAdvAddrs.ItemsSource = anchors[ListBoxAnchors.SelectedIndex].Tags;
            }
            
        }
            
        

        async private void ButtonFilterDisable_Click(object sender, RoutedEventArgs e)
        {
            byte[] filter = new byte[] { (byte)'B', (byte)'F', (byte)'D', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(filter, filter.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonFilterEnable_Click(object sender, RoutedEventArgs e)
        {
            byte[] filter = new byte[] { (byte)'B', (byte)'F', (byte)'E', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(filter, filter.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonFilterAdd_Click(object sender, RoutedEventArgs e)
        {
            byte[] filter = new byte[] { (byte)'B', (byte)'F', (byte)'A', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            byte[] advaddr = BitConverter.GetBytes(0x1FED7CEA75CBE);

            Array.Copy(advaddr, 0, filter, 4, 8);

            await commServer.SendAsync(filter, filter.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonSubscribe_Click(object sender, RoutedEventArgs e)
        {
            byte[] subscribe = new byte[] { (byte)'B', (byte)'C', (byte)'N', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(subscribe, subscribe.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonResetTime_Click(object sender, RoutedEventArgs e)
        {
            byte[] timeReset = new byte[] { (byte)'B', (byte)'T', (byte)'R', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(timeReset, timeReset.Length, new IPEndPoint(IP, 50000));
        }

        
        private void ButtonLogging_Click(object sender, RoutedEventArgs e)
        {
            logControl.Show();
        }

        async private void ButtonFilterAddFrom_Click(object sender, RoutedEventArgs e)
        {
            byte[] filter = new byte[] { (byte)'B', (byte)'F', (byte)'A', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            byte[] advaddr = BitConverter.GetBytes(((LocTag)ListBoxAdvAddrs.SelectedItem).AdvAddres);

            Array.Copy(advaddr, 0, filter, 4, 8);

            await commServer.SendAsync(filter, filter.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonSetRemote_Click(object sender, RoutedEventArgs e)
        {
            byte[] timeReset = new byte[] { (byte)'B', (byte)'R', (byte)'P', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(timeReset, timeReset.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonSetSelf_Click(object sender, RoutedEventArgs e)
        {
            byte[] timeReset = new byte[] { (byte)'B', (byte)'S', (byte)'P', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(timeReset, timeReset.Length, new IPEndPoint(IP, 50000));
        }

        async private void ButtonDbg_Click(object sender, RoutedEventArgs e)
        {
            byte[] dbg = new byte[] { (byte)'B', (byte)'D', (byte)'G', (byte)'\n', 0, 0, 0, 0, 0, 0, 0, 0 };

            await commServer.SendAsync(dbg, dbg.Length, new IPEndPoint(IP, 50000));
        }
    }
}
