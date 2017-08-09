// <copyright file="SettingsDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CmrHisto.Properties;
using Microsoft.Win32;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for SettingsDialog.xaml
	/// </summary>
	public sealed partial class SettingsDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handle the user clicking the cancel button and close the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handles the user clicking the automatically load RPM scale checkbox and enables or disables controls as necessary.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void AutomaticallyLoadRpmScaleCheckbox_Click(object sender, RoutedEventArgs e)
		{
			if (this.mAutomaticallyLoadRpmScaleCheckbox.IsChecked.Value)
			{
				this.mRpmScaleFileTextbox.IsEnabled = true;
				this.mBrowseForRpmFileButton.IsEnabled = true;
			}
			else
			{
				this.mRpmScaleFileTextbox.IsEnabled = false;
				this.mRpmScaleFileTextbox.Text = string.Empty;
				this.mBrowseForRpmFileButton.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handles the user clicking the automatically load Y-Axis scale checkbox and enables or disables controls as necessary.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void AutomaticallyLoadYAxisScaleCheckbox_Click(object sender, RoutedEventArgs e)
		{
			if (this.mAutomaticallyLoadYAxisScaleCheckbox.IsChecked.Value)
			{
				this.mYAxisScaleFileTextbox.IsEnabled = true;
				this.mBrowseForYAxisFileButton.IsEnabled = true;
			}
			else
			{
				this.mYAxisScaleFileTextbox.IsEnabled = false;
				this.mYAxisScaleFileTextbox.Text = string.Empty;
				this.mBrowseForYAxisFileButton.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handle the user clicking the browse button to look for an RPM scale file and show the open file dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void BrowseForRpmFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openDialog = new OpenFileDialog
			{
				Filter = "RPM Scale (*.csv)|*.csv",
				RestoreDirectory = true
			};
			if (openDialog.ShowDialog().Value)
			{
				this.mRpmScaleFileTextbox.Text = openDialog.FileName;
			}
		}

		/// <summary>
		/// Handle the user clicking the browse button to look for an RPM scale file and show the open file dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void BrowseForYAxisFileButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				Filter = this.YAxisPid + " Scale (*.csv)|*.csv",
				RestoreDirectory = true
			};
			if (dialog.ShowDialog().Value)
			{
				this.mYAxisScaleFileTextbox.Text = dialog.FileName;
			}
		}

		/// <summary>
		/// Handle the user clicking the save button and attempt to save the settings.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.CheckForUpdateOnLoad = this.mAutomaticallyCheckForUpdatesCheckbox.IsChecked.Value;
			Settings.Default.AutomaticallyLoadRpmScale = this.mAutomaticallyLoadRpmScaleCheckbox.IsChecked.Value;
			Settings.Default.AutomaticallyLoadYAxisScale = this.mAutomaticallyLoadYAxisScaleCheckbox.IsChecked.Value;
			Settings.Default.RpmScaleFile = this.mRpmScaleFileTextbox.Text;
			Settings.Default.YAxisScaleFile = this.mYAxisScaleFileTextbox.Text;
			Settings.Default.Language = this.mLanguageDropDown.SelectedValue.ToString();
			Settings.Default.DontShowScaleWarning = this.mDontShowScaleWarningCheckbox.IsChecked.Value;
			Settings.Default.SelectScaleValuesOnFocus = this.mSelectScaleValueCheckbox.IsChecked.Value;
			Settings.Default.Save();
			if (this.mLanguageDropDown.SelectedValue.ToString() != Thread.CurrentThread.CurrentCulture.ToString())
			{
				CultureInfo info = new CultureInfo(this.mLanguageDropDown.SelectedValue.ToString());
				Thread.CurrentThread.CurrentCulture = info;
				Thread.CurrentThread.CurrentUICulture = info;
			}

			this.Close();
		}
		#endregion

		#region Private Properties
		/// <summary>
		/// Gets or sets the text of the Y-Axis PID.
		/// </summary>
		/// <value>The name of the PID used for the Y-Axis.</value>
		private string YAxisPid { get; set; }
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsDialog"/> class.
		/// </summary>
		/// <param name="yAxisPid">The name of the pid used for the y axis.</param>
		internal SettingsDialog(string yAxisPid)
		{
			this.InitializeComponent();
			this.YAxisPid = yAxisPid;
			if (Settings.Default.AutomaticallyLoadRpmScale)
			{
				this.mAutomaticallyLoadRpmScaleCheckbox.IsChecked = true;
				this.mRpmScaleFileTextbox.Text = Settings.Default.RpmScaleFile;
				this.mRpmScaleFileTextbox.IsEnabled = true;
				this.mBrowseForRpmFileButton.IsEnabled = true;
			}

			if (Settings.Default.AutomaticallyLoadYAxisScale)
			{
				this.mAutomaticallyLoadYAxisScaleCheckbox.IsChecked = true;
				this.mYAxisScaleFileTextbox.Text = Settings.Default.YAxisScaleFile;
				this.mYAxisScaleFileTextbox.IsEnabled = true;
				this.mBrowseForYAxisFileButton.IsEnabled = true;
			}

			this.mAutomaticallyCheckForUpdatesCheckbox.IsChecked = new bool?(Settings.Default.CheckForUpdateOnLoad);
			foreach (ComboBoxItem item in (IEnumerable)this.mLanguageDropDown.Items)
			{
				if (string.Compare(item.Tag.ToString(), Settings.Default.Language, StringComparison.OrdinalIgnoreCase) == 0)
				{
					this.mLanguageDropDown.SelectedItem = item;
					break;
				}
			}

			this.mSelectScaleValueCheckbox.IsChecked = Settings.Default.SelectScaleValuesOnFocus;
			this.mAutomaticallyCheckForUpdatesCheckbox.IsChecked = Settings.Default.CheckForUpdateOnLoad;
		}
		#endregion
	}
}
