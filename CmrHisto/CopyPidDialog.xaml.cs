// <copyright file="CopyPidDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CmrHisto.Enums;
using Microsoft.Win32;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for CopyPidDialog.xaml
	/// </summary>
	public partial class CopyPidDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handle the user clicking the cancel button.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handle the user clicking the copy button and get the appropriate data.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CopyButtonClick(object sender, RoutedEventArgs e)
		{
			DataType exportType = DataType.Last;
			switch (this.mCopyTypeDropdown.SelectedIndex)
			{
				case 0:
					exportType = DataType.Last;
					break;

				case 1:
					exportType = DataType.Average;
					break;

				case 2:
					exportType = DataType.Minimum;
					break;

				case 3:
					exportType = DataType.Maximum;
					break;

				default:
					exportType = DataType.Last;
					break;
			}

			if (!this.ForClipboard)
			{
				SaveFileDialog dialog = new SaveFileDialog
				{
					FileName = string.Empty,
					DefaultExt = ".csv",
					Filter = "csv files (*.csv)|*.csv",
					RestoreDirectory = true
				};

				bool? dialogResult = dialog.ShowDialog();
				if (dialogResult.HasValue && dialogResult.Value)
				{
					this.Close();
					((CmrHistoMain)this.Owner).SetCopyPid((string)this.mPidListbox.SelectedItem, exportType, this.ForClipboard, dialog.FileName);
					return;
				}
			}

			this.Close();
			((CmrHistoMain)this.Owner).SetCopyPid((string)this.mPidListbox.SelectedItem, exportType, this.ForClipboard, string.Empty);
		}

		/// <summary>
		/// Handle the user changing the selected PID and enable the copy button if they have selected a PID, disable it otherwise.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void PidListboxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.mPidListbox.SelectedItem != null)
			{
				this.mCopyButton.IsEnabled = true;
			}
			else
			{
				this.mCopyButton.IsEnabled = false;
			}
		}
		#endregion

		#region Private Properties
		/// <summary>
		/// Gets or sets a value indicating whether or not the data should be saved to the clipboard.
		/// </summary>
		/// <value><c>True</c> if it should, <c>False</c> otherwise.</value>
		private bool ForClipboard { get; set; }
		#endregion

		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="CopyPidDialog"/> class.
		/// </summary>
		/// <param name="pids">A <see cref="ReadOnlyCollection"/> of <see cref="PidInformation"/> objects.</param>
		/// <param name="forClipboard">A flag indicating whether or not this data is being copied for the clipboard.</param>
		public CopyPidDialog(ReadOnlyCollection<PidInformation> pids, bool forClipboard)
		{
			this.InitializeComponent();
			this.ForClipboard = forClipboard;
			if (!this.ForClipboard)
			{
				this.mCopyLabel.Content = "Select the PID you would like to export.";
				this.mDataLabel.Content = "What data do you want to export to csv?";
				this.mCopyButton.Content = "Export";
				this.Title = "Export PID Data to CSV";
				Uri bitmapUri = new Uri("pack://application:,,,/Icons/export.ico", UriKind.Absolute);
				this.Icon = BitmapFrame.Create(bitmapUri);
			}

			if (pids != null)
			{
				foreach (PidInformation information in pids)
				{
					if (information.IsSelected)
					{
						this.mPidListbox.Items.Add(information.Name);
					}
				}
			}
		}
		#endregion
	}
}
