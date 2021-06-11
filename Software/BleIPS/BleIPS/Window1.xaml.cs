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

using MahApps.Metro.Controls;
using Figure;

namespace BleIPS
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : MetroWindow
    {
        Plot plot;
        LocTag locTag;

        public Window1(ulong Eui, LocTag locTag)
        {
            InitializeComponent();
            plot = new Plot(CanvasPlot);

            this.locTag = locTag;
            plot.PlotAdd(locTag);

            Title = string.Format("Details of {0:X} - {1:X}", Eui, locTag.AdvAddres);

            ListBoxAverage.ItemsSource = locTag.Average;
            ListBoxAverage.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Average", System.ComponentModel.ListSortDirection.Descending));
            ListBoxAverage.Items.IsLiveSorting = true;

        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            plot.Init(0, 120, -40, -40 );
            plot.ReRender();
            
        }

        public void PlotData(double[] data)
        {
            plot.PlotAdd(data, Plot.PLOT_TYPE.PLOT_MEASURE);
        }

        private void CanvasPlot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            plot.SizeUpdate(0, 0, 0, 0, 0000);
            plot.ReRender();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            plot.PlotRemove(locTag);
        }
    }
}
