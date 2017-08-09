// <copyright file="PidInformation.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto
{
	/// <summary>
	/// A class used to encapsulate information about PIDs.
	/// </summary>
	public sealed class PidInformation
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets the column number.
		/// </summary>
		/// <value>The column number that this PID is associated with.</value>
		internal int ColumnNumber { get; set; }

		/// <summary>
		/// Gets or sets the custom name for the PID.
		/// </summary>
		/// <value>The custom name, if there is one, the user has assigned to this PID.</value>
		internal string CustomName { get; set; }

		/// <summary>
		/// Gets or sets the name of the PID.
		/// </summary>
		/// <value>The PIDs name as defined in the log/csv.</value>
		internal string Name { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the PID was selected by the user.
		/// </summary>
		/// <value><c>True</c> if it was selected, <c>False</c> otherwise.</value>
		internal bool IsSelected { get; set; }

		/// <summary>
		/// Gets or sets the unit of measure for this PID.
		/// </summary>
		/// <value>The PIDs unit as defined in the log/csv.</value>
		internal string Unit { get; set; }
		#endregion
	}
}