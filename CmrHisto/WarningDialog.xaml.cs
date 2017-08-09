// <copyright file="WarningDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Windows;
using CmrHisto.Properties;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for WarningDialog.xaml
	/// </summary>
	public sealed partial class WarningDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handle the user clicking the ok button.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void OkayButton_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.DontShowScaleWarning = this.mDontShowAgainCheckbox.IsChecked.Value;
			Settings.Default.Save();
			this.Close();
		}
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="WarningDialog"/> class.
		/// </summary>
		internal WarningDialog()
		{
			this.InitializeComponent();
		}
		#endregion
	}
}
