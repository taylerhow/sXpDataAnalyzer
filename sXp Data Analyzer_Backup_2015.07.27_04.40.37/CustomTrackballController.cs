using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telerik.Charting;
using Telerik.WinControls.UI;

namespace sXp_Data_Analyzer
{
    public class CustomTrackballController : ChartTrackballController
    {
        protected override string GetTrackballText(List<DataPointInfo> points)
        {
            StringBuilder result = new StringBuilder("<html>");

            SortedDictionary<ChartSeries, List<DataPoint>> visiblePoints = new SortedDictionary<ChartSeries, List<DataPoint>>(new ChartSeriesComparer());

            foreach (DataPointInfo pointInfo in points)
            {
                if (visiblePoints.ContainsKey(pointInfo.Series))
                {
                    visiblePoints[pointInfo.Series].Add(pointInfo.DataPoint);
                }
                else
                {
                    visiblePoints.Add(pointInfo.Series, new List<DataPoint>() { pointInfo.DataPoint });
                }
            }

            int counter = 0;
            foreach (ChartSeries series in visiblePoints.Keys)
            {
                for (int i = 0; i < visiblePoints[series].Count; i++)
                {
                    Color pointColor = this.GetColorForDataPoint(series, visiblePoints[series][i]);
                    string color = string.Format("{0},{1},{2},{3}", pointColor.A, pointColor.R, pointColor.G, pointColor.B);
                    result.AppendFormat("<color={0}>{1}", color, this.GetPointText(visiblePoints[series][i]));

                    if (i < visiblePoints[series].Count)
                    {
                        result.Append(" ");
                    }
                }

                counter++;

                if (counter < visiblePoints.Keys.Count)
                {
                    result.Append("\n");
                }
            }

            result.Append("</html>");

            return result.ToString();
        }

        class ChartSeriesComparer : IComparer<ChartSeries>
        {
            public ChartSeriesComparer()
            {
            }

            public int Compare(ChartSeries x, ChartSeries y)
            {
                if (!(x is IndicatorBase) && y is IndicatorBase)
                {
                    return -1;
                }
                else if (x is IndicatorBase && !(y is IndicatorBase))
                {
                    return 1;
                }

                return x.GetHashCode().CompareTo(y.GetHashCode());
            }
        }
    }
}
