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
using Telerik.Charting;
using Telerik.WinControls.UI;

namespace sXp_Data_Analyzer
{
    public partial class MainForm : Telerik.WinControls.UI.RadForm
    {
        List<sXpFile> files;
        string[] validFileTypes = { "s1p", "s2p", "s3p", "s4p" };

        public MainForm()
        {
            InitializeComponent();

            this.files = new List<sXpFile>();

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

            //for (int i = 0; i < this.radChartView1.Controllers.Count; i++)
            //{
            //    if (this.radChartView1.Controllers[i] is ChartTrackballController)
            //    {
            //        this.radChartView1.Controllers.RemoveAt(i);
            //        this.radChartView1.Controllers.Add(new CustomTrackballController());
            //        break;
            //    }
            //}

            //Test chart
            //LineSeries series = new LineSeries();
            //series.DataPoints.Add(new CategoricalDataPoint(500, "Jan"));
            //series.DataPoints.Add(new CategoricalDataPoint(300, "Apr"));
            //series.DataPoints.Add(new CategoricalDataPoint(400, "Jul"));
            //series.DataPoints.Add(new CategoricalDataPoint(250, "Oct"));
            //this.radChartView1.Series.Add(series);

            //setup the Cartesian Grid
            CartesianArea area = this.radChartView1.GetArea<CartesianArea>();
            area.ShowGrid = true;
            CartesianGrid grid = area.GetGrid<CartesianGrid>();
            grid.DrawHorizontalFills = true;
            grid.BorderDashStyle = System.Drawing.Drawing2D.DashStyle.DashDot;
        }

        /***************************************************Private Helper Methods****************************************************/

        private void parseS2P(sXpFile file)
        {
            try
            {
                using (StreamReader sr = new StreamReader(file.FilePath))
                {
                    //Skip 6 lines of header info
                    for (int i = 0; i < 6; i++)
                    {
                        Console.WriteLine(sr.ReadLine());
                    }
                    String line = sr.ReadToEnd();
                    string[] delimeters = { " ", "\r", "\n" };
                    var array = line.Split(delimeters, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string s in array)
                    {
                        Console.WriteLine(s);
                    }

                    //Parse data into multiple line series
                    for (int i = 1; i <= 8; i++)
                    {
                        string currentFrequency = "";
                        LineSeries currentSeries = new LineSeries();

                        for (int k = 0; k < array.Length; k += 9)
                        {
                            currentFrequency = ((int.Parse(array[k], CultureInfo.InvariantCulture.NumberFormat)) / 1000000).ToString();
                            currentSeries.DataPoints.Add(new CategoricalDataPoint(double.Parse(array[k + i], CultureInfo.InvariantCulture.NumberFormat), currentFrequency));
                        }
                        if (i % 2 == 1)
                        {
                            file.Channels[(i - 1) / 2].MagSeries = currentSeries;
                            //if (file.Channels[(i - 1) / 2].DisplayMag == true)
                            //{
                            //this.radChartView1.Series.Add(currentSeries);
                            //}
                        }
                        else
                        {
                            file.Channels[(i - 1) / 2].PhaseSeries = currentSeries;
                            //if (file.Channels[(i - 1) / 2].DisplayPhase == true)
                            //{
                            //    this.radChartView1.Series.Add(currentSeries);
                            //}
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Parse File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /***************************************************GUI Event Handlers********************************************************/

        private void radBrowseEditor1_ValueChanged(object sender, EventArgs e)
        {
            //Import selected file
            try
            {
                using (StreamReader sr = new StreamReader(radBrowseEditor1.Value))
                {
                    Console.WriteLine(radBrowseEditor1.Value);
                    sXpFile newFile = new sXpFile(radBrowseEditor1.Value);
                    if (!this.validFileTypes.Contains(newFile.Type))
                    {
                        MessageBox.Show("The selected file type is not supported.\r\nPlease select a .s1p, .s2p, .s3p, or .s4p file.", "ERROR: Import File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        this.files.Add(newFile);
                        this.radGridView1.DataSource = this.files.ToArray().ToList();

                        //parse file data
                        switch (newFile.Type)
                        {
                            case "s1p":
                                break;
                            case "s2p":
                                this.parseS2P(newFile);
                                break;
                            case "s3p":
                                break;
                            case "s4p":
                                break;
                            default:
                                Console.WriteLine("ERROR: INVALID CASE IN PARSING SWITCH STATEMENT");
                                break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("The file could not be read: " + err.Message, "ERROR: Import File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void radGridView1_CurrentRowChanged(object sender, CurrentRowChangedEventArgs e)
        {
            this.radGridView2.DataSource = this.files[this.radGridView1.CurrentRow.Index].Channels;
        }

        private void radGridView2_CellValueChanged(object sender, GridViewCellEventArgs e)
        {
            this.files[this.radGridView1.CurrentRow.Index].plotChannels(radChartView1);

        }

    }
}
