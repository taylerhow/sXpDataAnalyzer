using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.WinControls.UI;

namespace sXp_Data_Analyzer
{
    class sXpChannel
    {
        public sXpChannel(string name)
        {
            this.Name = name;
            this.DisplayMag = false;
            this.DisplayPhase = false;
        }

        public string Name { get; set; }

        public bool DisplayMag { get; set; }

        public bool DisplayPhase { get; set; }

        public ScatterLineSeries MagSeries { get; set; }

        public ScatterLineSeries PhaseSeries { get; set; }

        public double MagMinFrequency { get; set; }

        public double MagMaxFrequency { get; set; }

        public double MagMinReading { get; set; }

        public double MagMaxReading { get; set; }

        public double PhaseMinFrequency { get; set; }

        public double PhaseMaxFrequency { get; set; }

        public double PhaseMinReading { get; set; }

        public double PhaseMaxReading { get; set; }

    }
}
