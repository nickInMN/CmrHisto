// <copyright file="ProgressDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Windows;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public sealed partial class ProgressDialog : Window
	{
		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialog"/> class.
		/// </summary>
		public ProgressDialog()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgressDialog"/> class.
		/// </summary>
		/// <param name="text">The text to display to the user.</param>
		public ProgressDialog(string text)
		{
			this.InitializeComponent();
			this.mProgressLabel.Content = text;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Update the text of the progress label.
		/// </summary>
		/// <param name="text">The text to display in the label.</param>
		public void SetProgressText(string text)
		{
			this.mProgressLabel.Content = text;
		}
		#endregion
	}
}