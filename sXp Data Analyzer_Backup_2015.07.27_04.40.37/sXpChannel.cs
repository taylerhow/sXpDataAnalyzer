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

        public LineSeries MagSeries { get; set; }

        public LineSeries PhaseSeries { get; set; }
    }
}
