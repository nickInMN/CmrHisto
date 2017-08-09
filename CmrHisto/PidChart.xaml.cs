// <copyright file="PidChart.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls.DataVisualization.Charting;

namespace CmrHisto
{
	/// <summary>
	/// The window responsible for showing a column chart of the number of each times a particular value was in a data cell.
	/// </summary>
	public partial class PidChart : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PidChart"/> class.
		/// </summary>
		/// <param name="pidName">The name of the pid being displayed.</param>
		/// <param name="values">The list of values for that pid from the cell that was clicked.</param>
		public PidChart(string pidName, List<double> values)
		{
			this.InitializeComponent();
			this.mChart.Title = pidName;
			this.Title = "CmrHisto - " + pidName + " Chart";

			List<KeyValuePair<string, int>> data = new List<KeyValuePair<string, int>>();

			List<double> uniqueValues = values.Distinct().OrderBy(x => x).ToList();
			foreach (double value in uniqueValues)
			{
				data.Add(new KeyValuePair<string, int>(value.ToString(CultureInfo.CurrentCulture), values.Where(x => x == value).Count()));
			}

			((ColumnSeries)mChart.Series[0]).ItemsSource = data;
		}
	}
}
