// <copyright file="ScalingInformation.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto
{
	/// <summary>
	/// A class used to encapsulate a row within a scale.
	/// </summary>
	internal sealed class ScalingInformation
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets the upper bound for this cell.
		/// </summary>
		/// <value>The highest value for this cell.</value>
		internal double MaxValue { get; set; }

		/// <summary>
		/// Gets or sets the lower bound for this cell.
		/// </summary>
		/// <value>The lowest value for this cell.</value>
		internal double MinValue { get; set; }

		/// <summary>
		/// Gets or sets the middle value for this cell.
		/// </summary>
		/// <value>The middle value for this cell.</value>
		internal double Value { get; set; }
		#endregion
	}
}