// <copyright file="CmrHistoDataCollection.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CmrHisto
{
	/// <summary>
	/// A class used to represent the data displayed in the histogram.
	/// </summary>
	internal sealed class CmrHistoDataCollection : ObservableCollection<List<CellDisplayData>>
	{
		#region Internal Methods
		/// <summary>
		/// Adds a row of data.
		/// </summary>
		/// <param name="data">A <see cref="List"/> of <see cref="CellDisplayData"/> objects.</param>
		internal void AddDataRow(List<CellDisplayData> data)
		{
			this.Add(data);
		}
		#endregion
	}
}
