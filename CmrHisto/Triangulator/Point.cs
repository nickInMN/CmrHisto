// <copyright file="Point.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto.Triangulator
{
	/// <summary>
	/// A class used to represent a point.
	/// </summary>
	internal sealed class Point
	{
		#region Internal Properties
		/// <summary>
		/// Gets or sets the x coordinate of the point.
		/// </summary>
		/// <value>The x coordinate of the point.</value>
		internal double X { get; set; }

		/// <summary>
		/// Gets or sets the y coordinate of the point.
		/// </summary>
		/// <value>The y coordinate of the point.</value>
		internal double Y { get; set; }

		/// <summary>
		/// Gets or sets the value of the point at the x, y position.
		/// </summary>
		/// <value>The value of the point at the x, y position.</value>
		internal double Value { get; set; }
		#endregion

		#region Internal Methods
		/// <summary>
		/// Checks to see if the supplied x and y coordinates are the same as this one, ignoring the value.
		/// </summary>
		/// <param name="other">The point to compare against.</param>
		/// <returns><c>True</c> if the x and y coordinates are the same, <c>False</c> otherwise.</returns>
		internal bool Equals(Point other)
		{
			return this.X == other.X && this.Y == other.Y;
		}
		#endregion

		#region Internal Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Point"/> class.
		/// </summary>
		/// <param name="x">The x coordinate of the point.</param>
		/// <param name="y">The y coordinate of the point.</param>
		/// <param name="value">The value for the point.</param>
		internal Point(double x, double y, double value)
		{
			this.X = x;
			this.Y = y;
			this.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Point"/> class.
		/// </summary>
		/// <param name="x">The x coordinate of the point.</param>
		/// <param name="y">The y coordinate of the point.</param>
		internal Point(double x, double y) : this(x, y, 0)
		{
		}
        #endregion

        #region Public Overrides
        public override string ToString()
        {
            return $"{this.X}-{this.Y}";
        }
        #endregion
    }
}