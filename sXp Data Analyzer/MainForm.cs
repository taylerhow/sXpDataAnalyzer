using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Telerik.Charting;
using Telerik.WinControls.UI;

namespace sXp_Data_Analyzer
{
    public partial class MainForm : Telerik.WinControls.UI.RadForm
    {
        List<sXpFile> files;
        string[] validFileTypes = { "s1p", "s2p", "s3p", "s4p" };
        double minimumFrequency;
        double maximumFrequency;
        double minimumReading;
        double maximumReading;
        bool customScalingEnabled;
        bool currentlyReconfiguringGraph;
        ScatterLineSeries horizontalMarker;
        ScatterLineSeries verticalMarker1;
        ScatterLineSeries verticalMarker2;
        sXpFile fileToExport;

        public MainForm()
        {
            InitializeComponent();

            this.files = new List<sXpFile>();
            this.customScalingEnabled = false;
            this.currentlyReconfiguringGraph = false;

            this.radGridView1.AutoGenerateColumns = false;
            this.radGridView1.SearchRowPosition = SystemRowPosition.Bottom;
            GridViewTextBoxColumn colName = new GridViewTextBoxColumn("File Name", "Name");
            colName.Width = 100;
            GridViewTextBoxColumn colType = new GridViewTextBoxColumn("Type", "Type");
            colType.Width = 50;
            GridViewTextBoxColumn colFilePath = new GridViewTextBoxColumn("File Path", "FilePath");
            colFilePath.Width = 300;
            this.radGridView1.MasterTemplate.Columns.Add(colName);
            this.radGridView1.MasterTemplate.Columns.Add(colType);
            this.radGridView1.MasterTemplate.Columns.Add(colFilePath);
            this.radGridView1.DataSource = this.files;

            this.radGridView2.AutoGenerateColumns = false;
            GridViewTextBoxColumn colChannelName = new GridViewTextBoxColumn("Channel Name", "Name");
            colChannelName.Width = 100;
            GridViewCheckBoxColumn colDisplayMag = new GridViewCheckBoxColumn("Mag", "DisplayMag");
            colDisplayMag.Width = 50;
            GridViewCheckBoxColumn colDisplayPhase = new GridViewCheckBoxColumn("Phase", "DisplayPhase");
            colDisplayPhase.Width = 50;
            this.radGridView2.MasterTemplate.Columns.Add(colChannelName);
            this.radGridView2.MasterTemplate.Columns.Add(colDisplayMag);
            this.radGridView2.MasterTemplate.Columns.Add(colDisplayPhase);

            //Setup RadChartView

            //not using TrackBallControllers anymore
            //for (int i = 0; i < this.radChartView1.Controllers.Count; i++)
            //{
            //    if (this.radChartView1.Controllers[i] is ChartTrackballController)
            //    {
            //        this.radChartView1.Controllers.RemoveAt(i);
            //        this.radChartView1.Controllers.Add(new CustomTrackballController(this));
            //        break;
            //    }
            //}

            LassoZoomController lassoZoomController = new LassoZoomController();
            radChartView1.Controllers.Add(lassoZoomController);

            this.radChartView1.ShowLegend = true;
            this.radChartView1.ChartElement.LegendPosition = LegendPosition.Right;

            //setup the Cartesian Grid
            CartesianArea area = this.radChartView1.GetArea<CartesianArea>();
            area.ShowGrid = true;
            CartesianGrid grid = area.GetGrid<CartesianGrid>();
            grid.DrawHorizontalFills = true;
            grid.BorderDashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;

            //Prepopulate label text boxes
            this.radTextBoxChartLabel.Text = "sXp Data Analyzation";
            this.radTextBoxHorizontalAxisLabel.Text = "Frequency (MHz)";
            this.radTextBoxVerticalAxisLabel.Text = "Reading";
            this.radLabelChartTitle.Location = new Point((this.radChartView1.Location.X + (this.radChartView1.Size.Width / 2) - (this.radLabelChartTitle.Size.Width/2)), this.radLabelChartTitle.Location.Y);

            //configure persistence settings/files
            this._initializeConfiguration("../../../resources/config.txt");
            this._reconfigureGraph();
        }

        /***************************************************Private Helper Methods****************************************************/

        private void _initializeConfiguration(string configFilePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(configFilePath))
                {
                    String line = sr.ReadToEnd();
                    string[] delimeters = {"\r\n" };
                    var array = line.Split(delimeters, StringSplitOptions.None);
                    int settingsIndex = -1;

                    for (int i = 0; i < array.Length - 1; i += 2)
                    {
                        if (array[i] == "-----SETTINGS-----")
                        {
                            settingsIndex = i + 1;
                            break;
                        }

                        sXpFile newFile = new sXpFile(array[i]);
                        if (!this.validFileTypes.Contains(newFile.Type))
                        {
                            MessageBox.Show("The import file type is not supported.\r\nPlease select a .s1p, .s2p, .s3p, or .s4p file.", "ERROR: Config File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            this.files.Add(newFile);
                            this.radGridView1.DataSource = this.files.ToArray().ToList();
                            this.radGridView1.CurrentRow = this.radGridView1.Rows[this.radGridView1.Rows.Count - 1];

                            //parse file data
                            switch (newFile.Type)
                            {
                                case "s1p":
                                    this._configParseS1P(newFile, array[i + 1]);
                                    break;
                                case "s2p":
                                    this._configParseS2P(newFile, array[i + 1]);
                                    break;
                                case "s3p":
                                    this._configParseS3P(newFile, array[i + 1]);
                                    break;
                                case "s4p":
                                    this._configParseS4P(newFile, array[i + 1]);
                                    break;
                                default:
                                    Console.WriteLine("ERROR: INVALID CASE IN PARSING SWITCH STATEMENT");
                                    break;
                            }
                            newFile.configureLineSeries();
                        }
                    }
                    this._plotAllFiles();

                    //configure settings
                    //Haxis, VAxis Min&MAx - bool CustomScalingEnabled
                    this.minimumFrequency = double.Parse(array[settingsIndex]);
                    this.maximumFrequency = double.Parse(array[++settingsIndex]);
                    this.minimumReading = double.Parse(array[++settingsIndex]);
                    this.maximumReading = double.Parse(array[++settingsIndex]);
                    this.customScalingEnabled = bool.Parse(array[++settingsIndex]);
                    //fill text boxes
                    this.currentlyReconfiguringGraph = true;
                    this.radTextBoxHAxisMin.Text = this.minimumFrequency.ToString();
                    this.radTextBoxHAxisMax.Text = this.maximumFrequency.ToString();
                    this.radTextBoxVAxisMin.Text = this.minimumReading.ToString();
                    this.radTextBoxVAxisMax.Text = this.maximumReading.ToString();
                    this.currentlyReconfiguringGraph = false;
                    //Horizontal, Vertical 1, Vertical 2 Markers
                    this.radTextBoxHMarker.Text = array[++settingsIndex];
                    this.radCheckBoxHMarker.Checked = bool.Parse(array[++settingsIndex]);
                    this.radTextBoxVMarker1.Text = array[++settingsIndex];
                    this.radCheckBoxVMarker1.Checked = bool.Parse(array[++settingsIndex]);
                    this.radTextBoxVMarker2.Text = array[++settingsIndex];
                    this.radCheckBoxVMarker2.Checked = bool.Parse(array[++settingsIndex]);
                    //Chart & Axis labels
                    this.radTextBoxChartLabel.Text = array[++settingsIndex];
                    this.radTextBoxHorizontalAxisLabel.Text = array[++settingsIndex];
                    this.radTextBoxVerticalAxisLabel.Text = array[++settingsIndex];

                    this._reconfigureGraph();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Config File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _parseS1P(sXpFile file)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    //Parse data into multiple line series
                    for (int i = 1; i <= 2; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 3)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _parseS2P(sXpFile file)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 8; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 9)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _parseS3P(sXpFile file)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 18; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 19)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _parseS4P(sXpFile file)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 32; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 33)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _configParseS1P(sXpFile file, string config)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    //Parse data into multiple line series
                    for (int i = 1; i <= 2; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 3)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }

                    //Setup channels using config string
                    string[] configDelimeters = { "," };
                    var configArray = config.Split(configDelimeters, StringSplitOptions.RemoveEmptyEntries);
                    int configIndex = 0;
                    foreach (sXpChannel channel in file.Channels)
                    {
                        //check Mag
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayMag = true;
                        }

                        configIndex++;

                        //check Phase
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayPhase = true;
                        }

                        configIndex++;
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _configParseS2P(sXpFile file, string config)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 8; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 9)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }

                    //Setup channels using config string
                    string[] configDelimeters = { "," };
                    var configArray = config.Split(configDelimeters, StringSplitOptions.RemoveEmptyEntries);
                    int configIndex = 0;
                    foreach (sXpChannel channel in file.Channels)
                    {
                        //check Mag
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayMag = true;
                        }

                        configIndex++;

                        //check Phase
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayPhase = true;
                        }

                        configIndex++;
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _configParseS3P(sXpFile file, string config)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 18; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 19)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }

                    //Setup channels using config string
                    string[] configDelimeters = { "," };
                    var configArray = config.Split(configDelimeters, StringSplitOptions.RemoveEmptyEntries);
                    int configIndex = 0;
                    foreach (sXpChannel channel in file.Channels)
                    {
                        //check Mag
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayMag = true;
                        }

                        configIndex++;

                        //check Phase
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayPhase = true;
                        }

                        configIndex++;
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _configParseS4P(sXpFile file, string config)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    //foreach (string s in array)
                    //{
                    //    Console.WriteLine(s);
                    //}

                    //Parse data into multiple line series
                    for (int i = 1; i <= 32; i++)
                    {
                        double currentFrequency = 0;
                        double currentReading = 0;
                        double currentMinimumFrequency = 999999999;
                        double currentMaximumFrequency = -999999999;
                        double currentMinimumReading = 999999999;
                        double currentMaximumReading = -999999999;
                        ScatterLineSeries currentSeries = new ScatterLineSeries();

                        for (int k = 0; k < array.Length; k += 33)
                        {
                            currentFrequency = ((double.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000);
                            currentReading = double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat);
                            currentSeries.DataPoints.Add(new ScatterDataPoint(currentFrequency, currentReading));

                            if (currentFrequency < currentMinimumFrequency) currentMinimumFrequency = currentFrequency;
                            if (currentFrequency > currentMaximumFrequency) currentMaximumFrequency = currentFrequency;
                            if (currentReading < currentMinimumReading) currentMinimumReading = currentReading;
                            if (currentReading > currentMaximumReading) currentMaximumReading = currentReading;
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            file.Channels[(i - 1) / 2].MagMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].MagMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].MagMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].MagMaxReading = currentMaximumReading;
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            file.Channels[(i - 1) / 2].PhaseMinFrequency = currentMinimumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMaxFrequency = currentMaximumFrequency;
                            file.Channels[(i - 1) / 2].PhaseMinReading = currentMinimumReading;
                            file.Channels[(i - 1) / 2].PhaseMaxReading = currentMaximumReading;
                        }
                    }

                    //Setup channels using config string
                    string[] configDelimeters = { "," };
                    var configArray = config.Split(configDelimeters, StringSplitOptions.RemoveEmptyEntries);
                    int configIndex = 0;
                    foreach (sXpChannel channel in file.Channels)
                    {
                        //check Mag
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayMag = true;
                        }

                        configIndex++;

                        //check Phase
                        if (configArray[configIndex] == "1")
                        {
                            channel.DisplayPhase = true;
                        }

                        configIndex++;
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _exportS1P(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.fileToExport.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    using (StreamWriter writer = new StreamWriter(filePath, false))
                    {
                        writer.WriteLine("Frequency,S11 Mag,S11 Phase");
                        for (int i = 1; i <= array.Length; i++)
                        {
                            writer.Write(array[i - 1]);
                            if (i % 3 != 0)
                            {
                                writer.Write(",");
                            }
                            else if (i % 3 == 0)
                            {
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _exportS2P(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.fileToExport.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    using (StreamWriter writer = new StreamWriter(filePath, false))
                    {
                        writer.WriteLine("Frequency,S11 Mag,S11 Phase,S21 Mag,S21 Phase,S12 Mag,S12 Phase,S22 Mag,S22 Phase");
                        for (int i = 1; i <= array.Length; i++)
                        {
                            writer.Write(array[i - 1]);
                            if (i % 9 != 0)
                            {
                                writer.Write(",");
                            }
                            else if (i % 9 == 0)
                            {
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _exportS3P(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.fileToExport.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    using (StreamWriter writer = new StreamWriter(filePath, false))
                    {
                        writer.WriteLine("Frequency,S11 Mag,S11 Phase,S12 Mag,S12 Phase,S13 Mag,S13 Phase,S21 Mag,S21 Phase,S22 Mag,S22 Phase,S23 Mag,S23 Phase,S31 Mag,S31 Phase,S32 Mag,S32 Phase,S33 Mag,S33 Phase");
                        for (int i = 1; i <= array.Length; i++)
                        {
                            writer.Write(array[i - 1]);
                            if (i % 19 != 0)
                            {
                                writer.Write(",");
                            }
                            else if (i % 19 == 0)
                            {
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _exportS4P(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.fileToExport.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        sr.ReadLine();
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);

                    using (StreamWriter writer = new StreamWriter(filePath, false))
                    {
                        writer.WriteLine("Frequency,S11 Mag,S11 Phase,S12 Mag,S12 Phase,S13 Mag,S13 Phase,S14 Mag,S14 Phase,S21 Mag,S21 Phase,S22 Mag,S22 Phase,S23 Mag,S23 Phase,S24 Mag,S24 Phase,S31 Mag,S31 Phase,S32 Mag,S32 Phase,S33 Mag,S33 Phase,S34 Mag,S34 Phase,S41 Mag,S41 Phase,S42 Mag,S42 Phase,S43 Mag,S43 Phase,S44 Mag,S44 Phase");
                        for (int i = 1; i <= array.Length; i++)
                        {
                            writer.Write(array[i - 1]);
                            if (i % 33 != 0)
                            {
                                writer.Write(",");
                            }
                            else if (i % 33 == 0)
                            {
                                writer.WriteLine();
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void _reconfigureGraph()
        {
            this.currentlyReconfiguringGraph = true;

            if (!this.customScalingEnabled)
            {
                this.minimumFrequency = 999999999;
                this.maximumFrequency = -999999999;
                this.minimumReading = 999999999;
                this.maximumReading = -999999999;

                foreach (sXpFile file in this.files)
                {
                    foreach (sXpChannel channel in file.Channels)
                    {
                        if (channel.DisplayMag)
                        {
                            if (channel.MagMinFrequency < this.minimumFrequency) this.minimumFrequency = channel.MagMinFrequency;
                            if (channel.MagMaxFrequency > this.maximumFrequency) this.maximumFrequency = channel.MagMaxFrequency;
                            if (channel.MagMinReading < this.minimumReading) this.minimumReading = channel.MagMinReading;
                            if (channel.MagMaxReading > this.maximumReading) this.maximumReading = channel.MagMaxReading;
                        }

                        if (channel.DisplayPhase)
                        {
                            if (channel.PhaseMinFrequency < this.minimumFrequency) this.minimumFrequency = channel.PhaseMinFrequency;
                            if (channel.PhaseMaxFrequency > this.maximumFrequency) this.maximumFrequency = channel.PhaseMaxFrequency;
                            if (channel.PhaseMinReading < this.minimumReading) this.minimumReading = channel.PhaseMinReading;
                            if (channel.PhaseMaxReading > this.maximumReading) this.maximumReading = channel.PhaseMaxReading;
                        }
                    }
                }
                if (this.radChartView1.Axes.Count > 0)
                {
                    LinearAxis horizontalAxis = this.radChartView1.Axes[0] as LinearAxis;
                    LinearAxis verticalAxis = this.radChartView1.Axes[1] as LinearAxis;

                    horizontalAxis.Minimum = this.minimumFrequency;
                    horizontalAxis.Maximum = this.maximumFrequency;
                    verticalAxis.Minimum = this.minimumReading;
                    verticalAxis.Maximum = this.maximumReading;
                }

                this.radChartView1.Zoom(1, 1);

                this.radTextBoxHAxisMin.Text = this.minimumFrequency.ToString();
                this.radTextBoxHAxisMax.Text = this.maximumFrequency.ToString();
                this.radTextBoxVAxisMin.Text = this.minimumReading.ToString();
                this.radTextBoxVAxisMax.Text = this.maximumReading.ToString();
            }
            else if (this.customScalingEnabled
                && double.TryParse(this.radTextBoxHAxisMin.Text, out this.minimumFrequency)
                && double.TryParse(this.radTextBoxHAxisMax.Text, out this.maximumFrequency) 
                && double.TryParse(this.radTextBoxVAxisMin.Text, out this.minimumReading)
                && double.TryParse(this.radTextBoxVAxisMax.Text, out this.maximumReading))
            {
                if (this.radChartView1.Axes.Count > 0)
                {
                    LinearAxis horizontalAxis = this.radChartView1.Axes[0] as LinearAxis;
                    LinearAxis verticalAxis = this.radChartView1.Axes[1] as LinearAxis;

                    horizontalAxis.Minimum = this.minimumFrequency;
                    horizontalAxis.Maximum = this.maximumFrequency;
                    verticalAxis.Minimum = this.minimumReading;
                    verticalAxis.Maximum = this.maximumReading;
                }

                this.radChartView1.Zoom(1, 1);
            }

            this._updateHorizontalMarker();
            this._updateVerticalMarker1();
            this._updateVerticalMarker2();
            this._updateChartAndAxisLabels();

            this.currentlyReconfiguringGraph = false;
        }

        private void _updateChartAndAxisLabels()
        {
            this.radChartView1.Title = this.radTextBoxChartLabel.Text;
            this.radLabelChartTitle.Text = this.radTextBoxChartLabel.Text;
            this.radLabelChartTitle.Location = new Point((this.radChartView1.Location.X + (this.radChartView1.Size.Width / 2) - (this.radLabelChartTitle.Size.Width / 2)), this.radLabelChartTitle.Location.Y);

            if (this.radChartView1.Axes.Count > 0)
            {
                //configure axis labels
                LinearAxis horizontalAxis = this.radChartView1.Axes[0] as LinearAxis;
                LinearAxis verticalAxis = this.radChartView1.Axes[1] as LinearAxis;
                //VAxis.LabelFormatProvider = new CustomFormatProvider();

                horizontalAxis.Title = this.radTextBoxHorizontalAxisLabel.Text;
                horizontalAxis.TitleElement.Font = new Font("Segoe UI Symbol", 11.0f);
                verticalAxis.Title = this.radTextBoxVerticalAxisLabel.Text;
                verticalAxis.TitleElement.Font = new Font("Segoe UI Symbol", 11.0f);
                verticalAxis.LabelFormat = "{0:0.00}";
            }
        }

        private void _plotAllFiles()
        {
            foreach (sXpFile file in this.files)
            {
                file.plotChannels(this.radChartView1);
            }
        }

        private void _updateHorizontalMarker()
        {
            //clear existing marker
            if (this.radChartView1.Series.Contains(this.horizontalMarker))
            {
                this.radChartView1.Series.Remove(this.horizontalMarker);
            }

            //add new marker
            double HMarkerValue = 0;

            if (this.radCheckBoxHMarker.Checked)
            {
                if (double.TryParse(this.radTextBoxHMarker.Text, out HMarkerValue))
                {
                    this.horizontalMarker = new ScatterLineSeries();
                    this.horizontalMarker.DataPoints.Add(new ScatterDataPoint(this.minimumFrequency, HMarkerValue));
                    this.horizontalMarker.DataPoints.Add(new ScatterDataPoint(this.maximumFrequency, HMarkerValue));
                    this.horizontalMarker.LegendTitle = "Horizontal Marker";
                    this.horizontalMarker.ForeColor = Color.DarkSlateGray;
                    this.radChartView1.Series.Add(this.horizontalMarker);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Horizontal Marker placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void _updateVerticalMarker1()
        {
            //clear exsiting marker
            if (this.radChartView1.Series.Contains(this.verticalMarker1))
            {
                this.radChartView1.Series.Remove(this.verticalMarker1);
            }

            //add new marker
            double VMarker1Value = 0;

            if (this.radCheckBoxVMarker1.Checked)
            {
                if (double.TryParse(this.radTextBoxVMarker1.Text, out VMarker1Value))
                {
                    this.verticalMarker1 = new ScatterLineSeries();
                    this.verticalMarker1.DataPoints.Add(new ScatterDataPoint(VMarker1Value, this.minimumReading));
                    this.verticalMarker1.DataPoints.Add(new ScatterDataPoint(VMarker1Value, this.maximumReading));
                    this.verticalMarker1.LegendTitle = "Vertical Marker 1";
                    this.verticalMarker1.ForeColor = Color.Red;
                    this.radChartView1.Series.Add(this.verticalMarker1);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Vertical Marker 1 placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            
        }

        private void _updateVerticalMarker2()
        {
            //clear exsiting marker
            if (this.radChartView1.Series.Contains(this.verticalMarker2))
            {
                this.radChartView1.Series.Remove(this.verticalMarker2);
            }

            //add new marker
            double VMarker2Value = 0;

            if (this.radCheckBoxVMarker2.Checked)
            {
                if (double.TryParse(this.radTextBoxVMarker2.Text, out VMarker2Value))
                {
                    this.verticalMarker2 = new ScatterLineSeries();
                    this.verticalMarker2.DataPoints.Add(new ScatterDataPoint(VMarker2Value, this.minimumReading));
                    this.verticalMarker2.DataPoints.Add(new ScatterDataPoint(VMarker2Value, this.maximumReading));
                    this.verticalMarker2.LegendTitle = "Vertical Marker 2";
                    this.verticalMarker2.ForeColor = Color.Red;
                    this.radChartView1.Series.Add(this.verticalMarker2);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Vertical Marker 2 placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void _saveToConfigFile()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter("config.txt", false))
                {
                    //write file configurations
                    foreach (sXpFile file in this.files)
                    {
                        writer.WriteLine(file.FilePath);
                        foreach (sXpChannel channel in file.Channels)
                        {
                            //Write Mag
                            if (channel.DisplayMag)
                            {
                                writer.Write("1");
                            }
                            else
                            {
                                writer.Write("0");
                            }
                            writer.Write(",");

                            //Write Phase
                            if (channel.DisplayPhase)
                            {
                                writer.Write("1");
                            }
                            else
                            {
                                writer.Write("0");
                            }
                            writer.Write(",");
                        }
                        writer.WriteLine();
                    }

                    //write etc settings
                    writer.WriteLine("-----SETTINGS-----");
                    //Haxis, VAxis Min&MAx - bool CustomScalingEnabled
                    writer.WriteLine(this.minimumFrequency);
                    writer.WriteLine(this.maximumFrequency);
                    writer.WriteLine(this.minimumReading);
                    writer.WriteLine(this.maximumReading);
                    writer.WriteLine(this.customScalingEnabled.ToString());

                    //Horizontal, Vertical 1, Vertical 2 Markers
                    writer.WriteLine(this.radTextBoxHMarker.Text);
                    writer.WriteLine(this.radCheckBoxHMarker.Checked.ToString());
                    writer.WriteLine(this.radTextBoxVMarker1.Text);
                    writer.WriteLine(this.radCheckBoxVMarker1.Checked.ToString());
                    writer.WriteLine(this.radTextBoxVMarker2.Text);
                    writer.WriteLine(this.radCheckBoxVMarker2.Checked.ToString());

                    //Chart & Axis labels
                    writer.WriteLine(this.radTextBoxChartLabel.Text);
                    writer.WriteLine(this.radTextBoxHorizontalAxisLabel.Text);
                    writer.WriteLine(this.radTextBoxVerticalAxisLabel.Text);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The config file could not be written: " + err.Message, "ERROR: Config File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /***************************************************GUI Event Handlers********************************************************/

        private void radBrowseEditor1_ValueChanged(object sender, EventArgs e)
        {
            if (this.radBrowseEditor1.Value == null) return;

            //Import selected file
            try
            {
                using (StreamReader sr = new StreamReader(radBrowseEditor1.Value))
                {
                    sXpFile newFile = new sXpFile(radBrowseEditor1.Value);
                    if (!this.validFileTypes.Contains(newFile.Type))
                    {
                        MessageBox.Show("The selected file type is not supported.\r\nPlease select a .s1p, .s2p, .s3p, or .s4p file.", "ERROR: Import File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        this.files.Add(newFile);
                        this.radGridView1.DataSource = this.files.ToArray().ToList();
                        this.radGridView1.CurrentRow = this.radGridView1.Rows[this.radGridView1.Rows.Count - 1];

                        //parse file data
                        switch (newFile.Type)
                        {
                            case "s1p":
                                this._parseS1P(newFile);
                                break;
                            case "s2p":
                                this._parseS2P(newFile);
                                break;
                            case "s3p":
                                this._parseS3P(newFile);
                                break;
                            case "s4p":
                                this._parseS4P(newFile);
                                break;
                            default:
                                Console.WriteLine("ERROR: INVALID CASE IN PARSING SWITCH STATEMENT");
                                break;
                        }

                        newFile.configureLineSeries();
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Import File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            this.radBrowseEditor1.Value = null;
        }

        private void radGridView1_CurrentRowChanged(object sender, CurrentRowChangedEventArgs e)
        {
            if (this.radGridView1.CurrentRow.Index > -1)
            {
                this.radGridView2.DataSource = this.files[this.radGridView1.CurrentRow.Index].Channels;
            }
            else
            {
                this.radGridView2.DataSource = null;
            }
        }

        private void radGridView2_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            this.files[this.radGridView1.CurrentRow.Index].plotChannels(radChartView1);
            this._reconfigureGraph();
            //LinearAxis horizontalAxis = this.radChartView1.Axes[0] as LinearAxis;
            //LinearAxis verticalAxis = this.radChartView1.Axes[1] as LinearAxis;

            //this.radTextBoxHAxisMin.Text = horizontalAxis.Minimum.ToString();
            //this.radTextBoxHAxisMax.Text = horizontalAxis.Maximum.ToString();
            //this.radTextBoxVAxisMin.Text = verticalAxis.Minimum.ToString();
            //this.radTextBoxVAxisMax.Text = verticalAxis.Maximum.ToString();
        }

        private void radGridView2_MouseLeave(object sender, EventArgs e)
        {
            radGridView2.EndEdit();
            radChartView1.Invalidate();
        }

        private void radGridView2_MouseMove(object sender, MouseEventArgs e)
        {
            radGridView2.EndEdit();
            radChartView1.Invalidate();
        }

        private void radButtonResetZoomAndScale_Click(object sender, EventArgs e)
        {
            this.customScalingEnabled = false;
            this._reconfigureGraph();
        }

        private void radCheckBoxHMarker_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            double HMarkerValue = 0;

            if (this.radCheckBoxHMarker.Checked)
            {
                if (double.TryParse(this.radTextBoxHMarker.Text, out HMarkerValue))
                {
                    this.horizontalMarker = new ScatterLineSeries();
                    this.horizontalMarker.DataPoints.Add(new ScatterDataPoint(this.minimumFrequency, HMarkerValue));
                    this.horizontalMarker.DataPoints.Add(new ScatterDataPoint(this.maximumFrequency, HMarkerValue));
                    this.horizontalMarker.LegendTitle = "Horizontal Marker";
                    this.horizontalMarker.ForeColor = Color.DarkSlateGray;
                    this.radChartView1.Series.Add(this.horizontalMarker);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Horizontal Marker placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!this.radCheckBoxHMarker.Checked && this.radChartView1.Series.Contains(this.horizontalMarker))
            {
                this.radChartView1.Series.Remove(this.horizontalMarker);
            }
        }

        private void radCheckBoxVMarker1_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            double VMarker1Value = 0;

            if (this.radCheckBoxVMarker1.Checked)
            {
                if (double.TryParse(this.radTextBoxVMarker1.Text, out VMarker1Value))
                {
                    this.verticalMarker1 = new ScatterLineSeries();
                    this.verticalMarker1.DataPoints.Add(new ScatterDataPoint(VMarker1Value, -300));
                    this.verticalMarker1.DataPoints.Add(new ScatterDataPoint(VMarker1Value, 300));
                    this.verticalMarker1.LegendTitle = "Vertical Marker 1";
                    this.verticalMarker1.ForeColor = Color.Red;
                    this.radChartView1.Series.Add(this.verticalMarker1);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Vertical Marker 1 placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!this.radCheckBoxVMarker1.Checked && this.radChartView1.Series.Contains(this.verticalMarker1))
            {
                this.radChartView1.Series.Remove(this.verticalMarker1);
            }
        }

        private void radCheckBoxVMarker2_ToggleStateChanged(object sender, StateChangedEventArgs args)
        {
            double VMarker2Value = 0;

            if (this.radCheckBoxVMarker2.Checked)
            {
                if (double.TryParse(this.radTextBoxVMarker2.Text, out VMarker2Value))
                {
                    this.verticalMarker2 = new ScatterLineSeries();
                    this.verticalMarker2.DataPoints.Add(new ScatterDataPoint(VMarker2Value, -300));
                    this.verticalMarker2.DataPoints.Add(new ScatterDataPoint(VMarker2Value, 300));
                    this.verticalMarker2.LegendTitle = "Vertical Marker 2";
                    this.verticalMarker2.ForeColor = Color.Blue;
                    this.radChartView1.Series.Add(this.verticalMarker2);
                }
                else
                {
                    MessageBox.Show("Invalid value entered for Vertical Marker 2 placement", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (!this.radCheckBoxVMarker2.Checked && this.radChartView1.Series.Contains(this.verticalMarker2))
            {
                this.radChartView1.Series.Remove(this.verticalMarker2);
            }
        }

        private void radTextBoxHMarker_TextChanged(object sender, EventArgs e)
        {
            this._updateHorizontalMarker();
        }

        private void radTextBoxVMarker1_TextChanged(object sender, EventArgs e)
        {
            this._updateVerticalMarker1();
        }

        private void radTextBoxVMarker2_TextChanged(object sender, EventArgs e)
        {
            this._updateVerticalMarker2();
        }

        private void radGridView1_UserDeletingRow(object sender, GridViewRowCancelEventArgs e)
        {
            this.radChartView1.Series.Clear();
            this.files.RemoveAt(this.radGridView1.CurrentRow.Index);
        }

        private void radGridView1_UserDeletedRow(object sender, GridViewRowEventArgs e)
        {
            this.radGridView1.DataSource = this.files;
            this._plotAllFiles();
        }

        private void radButtonSaveConfiguration_Click(object sender, EventArgs e)
        {
            this._saveToConfigFile();   
        }

        private void radTextBoxANYLabel_TextChanged(object sender, EventArgs e)
        {
            this._updateChartAndAxisLabels();
        }

        private void radTextBoxANY_AXIS_MIN_MAX_TextChanged(object sender, EventArgs e)
        {
            if (!this.currentlyReconfiguringGraph)
            {
                this.customScalingEnabled = true;
                this._reconfigureGraph();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._saveToConfigFile();
        }

        private void radChartView1_Resize(object sender, EventArgs e)
        {
            this.radLabelChartTitle.Location = new Point((this.radChartView1.Location.X + (this.radChartView1.Size.Width / 2) - (this.radLabelChartTitle.Size.Width / 2)), this.radLabelChartTitle.Location.Y);
        }

        private void radButtonExportFile_Click(object sender, EventArgs e)
        {
            if (this.radGridView1.CurrentRow != null)
            {
                this.fileToExport = this.files[this.radGridView1.CurrentRow.Index];
                this.saveFileDialog1.FileName = this.fileToExport.Name + " (" + this.fileToExport.Type + ")" + " [sXp Export]";
                this.saveFileDialog1.Filter = "Text File (*.txt) | *.txt";
                this.saveFileDialog1.DefaultExt = "txt";
                this.saveFileDialog1.ShowDialog();
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if (this.fileToExport != null)
            {
                switch (this.fileToExport.Type)
                {
                    case "s1p":
                        this._exportS1P(this.saveFileDialog1.FileName);
                        break;
                    case "s2p":
                        this._exportS2P(this.saveFileDialog1.FileName);
                        break;
                    case "s3p":
                        this._exportS3P(this.saveFileDialog1.FileName);
                        break;
                    case "s4p":
                        this._exportS4P(this.saveFileDialog1.FileName);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
