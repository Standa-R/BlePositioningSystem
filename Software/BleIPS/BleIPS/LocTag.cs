using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Figure;

namespace BleIPS
{
    public class AverageCl : INotifyPropertyChanged, IComparable<AverageCl>
    {
        double average;
        double std;
        public Brush Brush { get; set; }
        public double Average
        {
            get => average;
            set
            {
                average = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Average"));
            }
        }

        public double Std
        {
            get => std;
            set
            {
                std = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Std"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int CompareTo(AverageCl obj)
        {
            return average.CompareTo(obj.average);
        }
    }

    public class LocTag : IPlotData, INotifyPropertyChanged
    {
        /// <summary>
        /// Stores data from 4 antennas
        /// Ant0 indexes 0 1 2 for channels 0 1 2
        /// Amt1 indexes 3 4 5 for channels 0 1 2
        /// 
        /// </summary>
        private ObservableCollection<double>[] data;
        private List<double>[] avg;


        public event PropertyChangedEventHandler PropertyChanged;

        public ulong AdvAddres { get; }
        public int ReportsCount { get => (data[0].Count + data[1].Count + data[2].Count); }

        public ObservableCollection<AverageCl> Average { get; set; }

        public List<ObservableCollection<double>> lines => data.ToList();

        public LocTag(ulong advAddr)
        {
            this.AdvAddres = advAddr;
            data = new ObservableCollection<double>[12];
            avg = new List<double>[12];
            Average = new ObservableCollection<AverageCl>();

            for (int i = 0; i < 12; i++)
            {
                data[i] = new ObservableCollection<double>();
                avg[i] = new List<double>();
                Average.Add(new AverageCl { Average = 0, Brush = Plot.brushes2[i] });
            }
            
        }

        public void AddMeasure(LocReport locReport)
        {
            if ((locReport.Ant1Ch > 2) ||
                (locReport.Ant2Ch > 2) ||
                (locReport.Ant3Ch > 2) ||
                (locReport.Ant4Ch > 2) )
            {
                return;
            }

            if (avg[0 + locReport.Ant1Ch].Count >= 10)
            {
                avg[0 + locReport.Ant1Ch].RemoveAt(0);
            }
            avg[0 + locReport.Ant1Ch].Add(locReport.Ant1);

            if (avg[3 + locReport.Ant2Ch].Count >= 10)
            {
                avg[3 + locReport.Ant2Ch].RemoveAt(0);
            }
            avg[3 + locReport.Ant2Ch].Add(locReport.Ant2);

            if (avg[6 + locReport.Ant3Ch].Count >= 10)
            {
                avg[6 + locReport.Ant3Ch].RemoveAt(0);
            }
            avg[6 + locReport.Ant3Ch].Add(locReport.Ant3);

            if (avg[9 + locReport.Ant4Ch].Count >= 10)
            {
                avg[9 + locReport.Ant4Ch].RemoveAt(0);
            }
            avg[9 + locReport.Ant4Ch].Add(locReport.Ant4);


            for (int i = 0; i < avg.Length; i++)
            {
                Average[i].Average = avg[i].Sum() / 10.0;
                double sum = avg[i].Sum(d => Math.Pow(d - Average[i].Average, 2));
                Average[i].Std = Math.Sqrt((sum) / 10);
            }
            

            data[0 + locReport.Ant1Ch].Add(locReport.Ant1);
            data[3 + locReport.Ant2Ch].Add(locReport.Ant2);
            data[6 + locReport.Ant3Ch].Add(locReport.Ant3);
            data[9 + locReport.Ant4Ch].Add(locReport.Ant4);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ReportsCount"));
        }


    }
}
