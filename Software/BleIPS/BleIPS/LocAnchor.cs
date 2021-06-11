using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BleIPS
{
    public class LocAnchor : INotifyPropertyChanged
    {
        private ObservableCollection<LocTag> tags;
        string anchorState;

        public ulong Eui64 { get; }
        public ObservableCollection<LocTag> Tags { get => tags; }
        public int TagsCount { get => tags.Count; }

        public string AnchorState
        {
            get => anchorState;
            set
            {
                anchorState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AnchorState"));
            }
        }

        public LocAnchor(ulong eui64)
        {
            Eui64 = eui64;
            tags = new ObservableCollection<LocTag>();

        }

        public void ProcessAnchorState(byte anchorState)
        {
            if (anchorState != 0)
            {
                AnchorState = "Self";
            }
            else
            {
                AnchorState = "Remote";
            }
        }
        
        public void ProcessReport(LocReport locReport)
        {
            bool notListed = true;
            int i;

            for (i = 0; i < tags.Count; i++)
            {
                if (tags[i].AdvAddres == locReport.AdvAddr)
                {
                    notListed = false;
                    break;
                }
            }

            if (notListed)
            {
                tags.Add(new LocTag(locReport.AdvAddr));
                tags[tags.Count - 1].AddMeasure(locReport);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TagsCount"));
            }
            else
            { 
                tags[i].AddMeasure(locReport);
            }
            
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
