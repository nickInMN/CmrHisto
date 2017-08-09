// <copyright file="CellDisplayData.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>
using System.Collections.Generic;

namespace CmrHisto
{
	/// <summary>
	/// A class that is used to make displaying the data in the data grid easier.
	/// </summary>
	public sealed class CellDisplayData
	{
		#region Public Properties
		/// <summary>
		/// Gets the background color for the cell.
		/// </summary>
		/// <value>The cell's background color.</value>
		public string Color { get; internal set; }

		/// <summary>
		/// Gets the text to display.
		/// </summary>
		/// <value>The text that is shown to the user.</value>
		public string DisplayText { get; internal set; }

		/// <summary>
		/// Gets the tooltip to display for the Y-Axis.
		/// </summary>
		/// <value>The PID used for the Y-Axis.</value>
		public string YAxisToolTip { get; internal set; }

		/// <summary>
		/// Gets the value for the Y-Axis.
		/// </summary>
		/// <value>The Y-Axis value.</value>
		public string YAxisValue { get; internal set; }

		/// <summary>
		/// Gets the list of selected pids.
		/// </summary>
		/// <value>These pids are used to build a context menu in the cell.</value>
		public List<string> Pids { get; internal set; }
		#endregion
	}
}