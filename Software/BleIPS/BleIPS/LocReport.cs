using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BleIPS
{
    public class LocReport : INotifyPropertyChanged
    {
        DateTime time;
        ulong advAddr;
        ulong eui;
        int ant1;
        int ant2;
        int ant3;
        int ant4;

        public DateTime Time
        {
            get => time;
        }

        public ulong AdvAddr
        {
            get => advAddr;
        }

        public ulong Eui
        {
            get => eui;
        }

        public int Ant1
        {
            get => ant1;
        }

        public int Ant2
        {
            get => ant2;
        }

        public int Ant3
        {
            get => ant3;
        }

        public int Ant4
        {
            get => ant4;
        }

        public int Ant1Ch { get; }
        public int Ant2Ch { get; }
        public int Ant3Ch { get; }
        public int Ant4Ch { get; }

        public LocReport(uint time, ulong eui, ulong advAddr, int[] rssi, int[] channel)
        {
            this.time = this.time.AddMilliseconds(time);
            this.eui = eui;
            this.advAddr = advAddr;
            ant1 = rssi[0];
            ant2 = rssi[1];
            ant3 = rssi[2];
            ant4 = rssi[3];

            Ant1Ch = channel[0];
            Ant2Ch = channel[1];
            Ant3Ch = channel[2];
            Ant4Ch = channel[3];
        }

        /* ======================== Property change ======================== */
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
