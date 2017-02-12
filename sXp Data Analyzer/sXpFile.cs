using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Charting;
using Telerik.WinControls.UI;

namespace sXp_Data_Analyzer
{
    class sXpFile
    {
        public sXpFile(string filepath)
        {
            this.FilePath = filepath;

            string[] delimeters = { "\\", "." };
            string[] array = filepath.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

            this.Name = array[array.Length - 2];
            this.Type = array[array.Length - 1];

            this._configureChannels();
        }

        public string FilePath { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public List<sXpChannel> Channels { get; set; }

        //Private helper methods

        private void _configureChannels()
        {
            this.Channels = new List<sXpChannel>();
            switch (this.Type)
            {
                case "s1p":
                    this.Channels.Add(new sXpChannel("S11"));
                    break;
                case "s2p":
                    this.Channels.Add(new sXpChannel("S11"));
                    this.Channels.Add(new sXpChannel("S21"));
                    this.Channels.Add(new sXpChannel("S12"));
                    this.Channels.Add(new sXpChannel("S22"));
                    break;
                case "s3p":
                    this.Channels.Add(new sXpChannel("S11"));
                    this.Channels.Add(new sXpChannel("S12"));
                    this.Channels.Add(new sXpChannel("S13"));
                    this.Channels.Add(new sXpChannel("S21"));
                    this.Channels.Add(new sXpChannel("S22"));
                    this.Channels.Add(new sXpChannel("S23"));
                    this.Channels.Add(new sXpChannel("S31"));
                    this.Channels.Add(new sXpChannel("S32"));
                    this.Channels.Add(new sXpChannel("S33"));
                    break;
                case "s4p":
                    this.Channels.Add(new sXpChannel("S11"));
                    this.Channels.Add(new sXpChannel("S12"));
                    this.Channels.Add(new sXpChannel("S13"));
                    this.Channels.Add(new sXpChannel("S14"));
                    this.Channels.Add(new sXpChannel("S21"));
                    this.Channels.Add(new sXpChannel("S22"));
                    this.Channels.Add(new sXpChannel("S23"));
                    this.Channels.Add(new sXpChannel("S24"));
                    this.Channels.Add(new sXpChannel("S31"));
                    this.Channels.Add(new sXpChannel("S32"));
                    this.Channels.Add(new sXpChannel("S33"));
                    this.Channels.Add(new sXpChannel("S34"));
                    this.Channels.Add(new sXpChannel("S41"));
                    this.Channels.Add(new sXpChannel("S42"));
                    this.Channels.Add(new sXpChannel("S43"));
                    this.Channels.Add(new sXpChannel("S44"));
                    break;
            }
        }

        //Public helper methods

        public void plotChannels(RadChartView chart)
        {
            foreach (sXpChannel channel in this.Channels)
            {
                //Mag
                if (channel.DisplayMag == true && !chart.Series.Contains(channel.MagSeries))
                {
                    chart.Series.Add(channel.MagSeries);
                }
                else if (channel.DisplayMag == false && chart.Series.Contains(channel.MagSeries))
                {
                    chart.Series.Remove(channel.MagSeries);
                }

                //Phase
                if (channel.DisplayPhase == true && !chart.Series.Contains(channel.PhaseSeries))
                {
                    chart.Series.Add(channel.PhaseSeries);
                }
                else if (channel.DisplayPhase == false && chart.Series.Contains(channel.PhaseSeries))
                {
                    chart.Series.Remove(channel.PhaseSeries);
                }
            }
        }

        public void configureLineSeries()
        {
            foreach (sXpChannel channel in this.Channels)
            {
                channel.MagSeries.LegendTitle = this.Name + ": " + channel.Name + "Mag";
                channel.PhaseSeries.LegendTitle = this.Name + ": " + channel.Name + "Phase";
            }
        }
    }
}
