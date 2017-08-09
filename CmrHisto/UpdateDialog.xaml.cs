// <copyright file="UpdateDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for UpdateDialog.xaml
	/// </summary>
	public sealed partial class UpdateDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handles the user clicking on the version hyperlink and directs the user to the versions webpage.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void VersionsHyperlinkClicked(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}
		#endregion

		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdateDialog"/> class.
		/// </summary>
		public UpdateDialog()
		{
			this.InitializeComponent();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Tell the user that they are using the current version.
		/// </summary>
		public void Current()
		{
			this.mProgressBar.Visibility = Visibility.Hidden;
			this.mUpdateTextBlock.Text = CmrHisto.Properties.Resources.VersionUpToDate;
			this.Title = "Current Version";
		}

		/// <summary>
		/// Tell the user their version is out of date and display a link to the updated version.
		/// </summary>
		/// <param name="currentVersion">The newest version number.</param>
		/// <param name="executingVersion">The current version the user is running.</param>
		public void NotCurrent(string currentVersion, string executingVersion)
		{
			this.mProgressBar.Visibility = Visibility.Hidden;
			this.mUpdateTextBlock.Text = string.Empty;
			this.mUpdateTextBlock.TextWrapping = TextWrapping.Wrap;
			this.mUpdateTextBlock.TextAlignment = TextAlignment.Center;
			Hyperlink newVersionLink = new Hyperlink(new Run(CmrHisto.Properties.Resources.ClickHere))
			{
				NavigateUri = new Uri("http://cmrhisto.candnbrett.com/download.html")
			};
			newVersionLink.RequestNavigate += new RequestNavigateEventHandler(this.VersionsHyperlinkClicked);
			this.mUpdateTextBlock.Inlines.Add(new Run(string.Format(CultureInfo.CurrentCulture, CmrHisto.Properties.Resources.VersionNotCurrent, executingVersion, currentVersion)));
			this.mUpdateTextBlock.Inlines.Add(newVersionLink);
			this.mUpdateTextBlock.Inlines.Add(new Run(CmrHisto.Properties.Resources.Period));
			this.Title = "Not Current Version";
		}

		/// <summary>
		/// Set the text displayed in the dialog.
		/// </summary>
		/// <param name="text">The text to display.</param>
		public void SetText(string text)
		{
			this.mUpdateTextBlock.Text = text;
		}
		#endregion
	}
}
