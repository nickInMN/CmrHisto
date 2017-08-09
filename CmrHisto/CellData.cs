// <copyright file="CellData.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace CmrHisto
{
	/// <summary>
	/// A class used to encapsulate information about each cell of the histogram.
	/// </summary>
	internal sealed class CellData
	{
		#region Private Data Members
		private SortedDictionary<int, double> mAverageValues = new SortedDictionary<int, double>();
		private SortedDictionary<int, List<double>> mData = new SortedDictionary<int, List<double>>();
		private SortedDictionary<int, double> mMaxValues = new SortedDictionary<int, double>();
		private SortedDictionary<int, double> mMinValues = new SortedDictionary<int, double>();
		private int mSampleSize;
		#endregion

		#region Internal Methods
		/// <summary>
		/// Add sample data to the cell.
		/// </summary>
		/// <param name="columnNumber">The column number (PID) to add the data for.</param>
		/// <param name="value">The value of the data.</param>
		internal void AddDataSample(int columnNumber, double value)
		{
			List<double> curentValues = new List<double>();
			if (!this.mData.TryGetValue(columnNumber, out curentValues))
			{
				this.mData.Add(columnNumber, new List<double> { value });
			}
			else
			{
				curentValues.Add(value);
			}
		}

		/// <summary>
		/// Gets the average value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column to get the average for.</param>
		/// <returns>The average value.</returns>
		internal double GetAverageValue(int columnNumber)
		{
			return this.mAverageValues[columnNumber];
		}

		/// <summary>
		/// Gets the last recorded value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column to get the last recorded value for.</param>
		/// <returns>The last recorded value.</returns>
		internal double GetLastValue(int columnNumber)
		{
			List<double> list = this.mData[columnNumber];
			return list[list.Count - 1];
		}

		/// <summary>
		/// Gets the maximum value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column to get the maximum for.</param>
		/// <returns>The highest value.</returns>
		internal double GetMaximumValue(int columnNumber)
		{
			return this.mMaxValues[columnNumber];
		}

		/// <summary>
		/// Gets the minimum value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column to get the minimum for.</param>
		/// <returns>The lowest value.</returns>
		internal double GetMinimumValue(int columnNumber)
		{
			return this.mMinValues[columnNumber];
		}

		/// <summary>
		/// Gets all of the PID values for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column to get the values for.</param>
		/// <returns>A <see cref="List"/> of <see cref="doubles"/> with all the values for the specified PID.</returns>
		internal List<double> GetPidValues(int columnNumber)
		{
			List<double> list = new List<double>();
			this.mData.TryGetValue(columnNumber, out list);
			return list;
		}

		/// <summary>
		/// Adds one to the sample size.
		/// </summary>
		internal void IncrementSampleSize()
		{
			this.mSampleSize++;
		}

		/// <summary>
		/// Sets the average value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column number to set the average for.</param>
		/// <param name="averageValue">The average value.</param>
		internal void SetAverageValue(int columnNumber, double averageValue)
		{
			this.mAverageValues.Add(columnNumber, averageValue);
		}

		/// <summary>
		/// Sets the maximum value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column number to set the maximum for.</param>
		/// <param name="maxValue">The maximum value.</param>
		internal void SetMaximumValue(int columnNumber, double maxValue)
		{
			this.mMaxValues.Add(columnNumber, maxValue);
		}

		/// <summary>
		/// Sets the minimum value for the specified column (PID).
		/// </summary>
		/// <param name="columnNumber">The column number to set the minimum for.</param>
		/// <param name="minValue">The minimum value.</param>
		internal void SetMinimumValue(int columnNumber, double minValue)
		{
			this.mMinValues.Add(columnNumber, minValue);
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets the number of samples for this cell.
		/// </summary>
		/// <value>The number of samples in this cell.</value>
		internal int SampleSize
		{
			get
			{
				return this.mSampleSize;
			}
		}
		#endregion
	}
}