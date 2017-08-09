// <copyright file="DataType.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto.Enums
{
	/// <summary>
	/// Enumeration used to tell which cell value to highlight on.
	/// </summary>
	public enum DataType
	{
		/// <summary>
		/// Value: 0 - Highlight on the minimum value.
		/// </summary>
		Minimum = 0,

		/// <summary>
		/// Highlight on the maximum value.
		/// </summary>
		Maximum = 1,

		/// <summary>
		/// Highlight on the average value.
		/// </summary>
		Average = 2,

		/// <summary>
		/// Highlight on the last value.
		/// </summary>
		Last = 3
	}
}