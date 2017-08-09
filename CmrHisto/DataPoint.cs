// <copyright file="DataPoint.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto
{
	/// <summary>
	/// This class represents the value of a single PID for the given RPM and Y-Axis value.
	/// </summary>
	public sealed class DataPoint
	{
		/// <summary>
		/// Gets or sets the name of the pid.
		/// </summary>
		/// <value>The name of the pid.</value>
		public string PidName { get; set; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		public double Value { get; set; }

		/// <summary>
		/// Gets or sets the value for the x axis.
		/// </summary>
		/// <value>The x axis coordinate.</value>
		public double XAxis { get; set; }

		/// <summary>
		/// Gets or sets the value for the y axis.
		/// </summary>
		/// <value>The y axis coordinate.</value>
		public double YAxis { get; set; }
	}
}
