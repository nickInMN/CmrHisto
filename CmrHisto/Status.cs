// <copyright file="Status.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto.Enums
{
	/// <summary>
	/// An enumeration used to define the status of various operations.
	/// </summary>
	internal enum Status
	{
		/// <summary>
		/// Value: 0 - None
		/// </summary>
		None = 0,

		/// <summary>
		/// Value: 1 - The operation was columnWidth result.
		/// </summary>
		Success = 1,

		/// <summary>
		/// Value: 2 - A general error occurred.
		/// </summary>
		GeneralError = 2,

		/// <summary>
		/// Value: 3 - There was an error opening the file.
		/// </summary>
		ErrorOpeningFile = 3,

		/// <summary>
		/// Value: 4 - The supplied file was empty.
		/// </summary>
		EmptyFile = 4,

		/// <summary>
		/// Value: 5 - The supplied file was large.
		/// </summary>
		LargeFile = 5,

		/// <summary>
		/// Value: 6 - There was columnWidth problem with the cellDisplayData.
		/// </summary>
		DataError = 6,

		/// <summary>
		/// Value: 7 - No RPM pid found.
		/// </summary>
		RpmNotFound = 7,

		/// <summary>
		/// Value: 8 - No PRatio or PIDs used to calculate PRatio were found.
		/// </summary>
		RatioNotFound = 8,

		/// <summary>
		/// Value: 9 - The operation was cancelled.
		/// </summary>
		Canceled = 9
	}
}
