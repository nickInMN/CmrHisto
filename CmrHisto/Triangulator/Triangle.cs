// <copyright file="Triangle.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto.Triangulator
{
	/// <summary>
	/// A class used to represent a triangle.
	/// </summary>
	public sealed class Triangle
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets the index of first point of the triangle.
		/// </summary>
		/// <value>The index of the first point in the triangle.</value>
		internal int PointOne { get; set; }

		/// <summary>
		/// Gets or sets the index of second point of the triangle.
		/// </summary>
		/// <value>The index of the second point in the triangle.</value>
		internal int PointTwo { get; set; }

		/// <summary>
		/// Gets or sets the index of third point of the triangle.
		/// </summary>
		/// <value>The index of the third point in the triangle.</value>
		internal int PointThree { get; set; }
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Triangle"/> class.
		/// </summary>
		/// <param name="pointOne">The first point of the triangle.</param>
		/// <param name="pointTwo">The second point of the triangle.</param>
		/// <param name="pointThree">The third point of the triangle.</param>
		internal Triangle(int pointOne, int pointTwo, int pointThree)
		{
			this.PointOne = pointOne;
			this.PointTwo = pointTwo;
			this.PointThree = pointThree;
		}
		#endregion
	}
}
