// <copyright file="HeaderInformation.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

namespace CmrHisto
{
	/// <summary>
	/// A class used to allow the headers to have tooltips.
	/// </summary>
	public sealed class HeaderInformation
	{
		#region Public Methods
		/// <summary>
		/// Overridden to string to show the desired text instead of the default.
		/// </summary>
		/// <returns>The text to display.</returns>
		public override string ToString()
		{
			return this.Text;
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the text to display.
		/// </summary>
		/// <value>The text of the header.</value>
		public string Text { get; internal set; }

		/// <summary>
		/// Gets the tooltip value.
		/// </summary>
		/// <value>The tooltip value.</value>
		public string ToolTip { get; internal set; }
		#endregion
	}
}
