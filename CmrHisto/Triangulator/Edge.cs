// <copyright file="Edge.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;

namespace CmrHisto.Triangulator
{
	/// <summary>
	/// A class used to represent the edge of a polygon.
	/// </summary>
	internal sealed class Edge : IEquatable<Edge>
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets the starting point of the edge.
		/// </summary>
		/// <value>The starting point of this edge.</value>
		internal int StartingPoint { get; set; }

		/// <summary>
		/// Gets or sets the ending point of the edge.
		/// </summary>
		/// <value>The ending point of this edge.</value>
		internal int EndingPoint { get; set; }
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Edge"/> class.
		/// </summary>
		/// <param name="start">Start edge vertex index.</param>
		/// <param name="end">End edge vertex index.</param>
		internal Edge(int start, int end)
		{
			this.StartingPoint = start;
			this.EndingPoint = end;
		}
		#endregion

		#region IEquatable<dEdge> Members

		/// <summary>
		/// Determine if two edges are the same. This will be true even if the edges start at opposite ends.
		/// </summary>
		/// <param name="other">The <see cref="Edge"/> to compare to.</param>
		/// <returns><c>True</c> if the edges are the same, <c>False</c> otherwise.</returns>
		public bool Equals(Edge other)
		{
			if (other == null)
			{
				throw new ArgumentNullException("other", "The edge to compare may not be null.");
			}

			if (this.StartingPoint == other.StartingPoint && this.EndingPoint == other.EndingPoint)
			{
				return true;
			}
			else if (this.StartingPoint == other.EndingPoint && this.EndingPoint == other.StartingPoint)
			{
				return true;
			}

			return false;
		}

		#endregion
	}
}