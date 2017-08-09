// <copyright file="MapData.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Collections.Generic;
using CmrHisto.Triangulator;

namespace CmrHisto
{
	/// <summary>
	/// A class to represent the data used to create a surface plot.
	/// </summary>
	public sealed class MapData
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets a list of points that contain the actual plot data. This will be triangulated later.
		/// </summary>
		/// <value>The list of points for the data.</value>
		internal List<Point> Data { get; set; }

		/// <summary>
		/// Gets or sets the lowest value of the data being plotted.
		/// </summary>
		/// <value>The data's lowest value.</value>
		internal double LowestValue { get; set; }

		/// <summary>
		/// Gets or sets the highest value of the data being plotted.
		/// </summary>
		/// <value>The data's highest value.</value>
		internal double HighestValue { get; set; }

		/// <summary>
		/// Gets or sets the lowest value of the data along the x axis.
		/// </summary>
		/// <value>The data's lowest x axis value.</value>
		internal double LowestXAxisValue { get; set; }

		/// <summary>
		/// Gets or sets the highest value of the data along the x axis.
		/// </summary>
		/// <value>The data's highest x axis value.</value>
		internal double HighestXAxisValue { get; set; }

		/// <summary>
		/// Gets or sets the lowest value of the data along the z axis.
		/// </summary>
		/// <value>The data's lowest z axis value.</value>
		internal double LowestZAxisValue { get; set; }

		/// <summary>
		/// Gets or sets the highest value of the data along the z axis.
		/// </summary>
		/// <value>The data's highest z axis value.</value>
		internal double HighestZAxisValue { get; set; }

		/// <summary>
		/// Gets or sets the text to display as the label for the x axis.
		/// </summary>
		/// <value>The label for the x axis.</value>
		internal string XAxisLabel { get; set; }

		/// <summary>
		/// Gets or sets the text to display as the label for the y axis.
		/// </summary>
		/// <value>The label for the Y axis.</value>
		internal string YAxisLabel { get; set; }

		/// <summary>
		/// Gets or sets the text to display as the label for the z axis.
		/// </summary>
		/// <value>The label for the z axis.</value>
		internal string ZAxisLabel { get; set; }

		/// <summary>
		/// Gets or sets the unit for the selected PID.
		/// </summary>
		/// <value>The selected PID's unit.</value>
		internal string YAxisPIDUnit { get; set; }
		#endregion
	}
}
