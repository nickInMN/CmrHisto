// <copyright file="PidSelectionDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CmrHisto.Properties;

namespace CmrHisto
{
    /// <summary>
    /// Interaction logic for PidSelectionDialog.xaml
    /// </summary>
    public sealed partial class PidSelectionDialog : Window
	{
		#region Private Data Members
		private List<PidInformation> mPids;
		#endregion

		#region Private Event Handlers
		/// <summary>
		/// Handle the user checking/unchecking the "Auto Scale Y-Axis" checkbox.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void AutoScaleYAxisCheckboxChecked(object sender, RoutedEventArgs e)
		{
			if (this.mAutoScaleYAxisCheckbox.IsChecked.Value)
			{
				this.mOnlyPositiveRatioCheckbox.IsEnabled = true;
			}
			else
			{
				this.mOnlyPositiveRatioCheckbox.IsChecked = false;
				this.mOnlyPositiveRatioCheckbox.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handle the ECT range textbox losing focus and validate the value.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void EctRangeTexeboxLostFocus(object sender, RoutedEventArgs e)
		{
			int ectValue;
			TextBox enteredValue = sender as TextBox;
			if (!int.TryParse(enteredValue.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out ectValue))
			{
				MessageBox.Show("You must enter a valid integer for ECT ranges.", CmrHisto.Properties.Resources.MessageBoxEctError, MessageBoxButton.OK, MessageBoxImage.Hand);
				enteredValue.Background = Brushes.Red;
				this.mGoButton.IsEnabled = false;
			}
			else
			{
				enteredValue.Background = Brushes.White;
				if (this.mPidListbox.SelectedItems.Count > 0)
				{
					this.mGoButton.IsEnabled = true;
				}
			}
		}

		/// <summary>
		/// Handle the user clicking the Okay button and process the dialog's values.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void OkayButtonClick(object sender, RoutedEventArgs e)
		{
			if (this.mPidListbox.SelectedItems.Count < 1)
			{
				MessageBox.Show("You must select at least one PID!", CmrHisto.Properties.Resources.MessageBoxGeneralError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else
			{
				for (int i = 0; i < this.mPidListbox.SelectedItems.Count; i++)
				{
					string selectedPid = this.mPidListbox.SelectedItems[i].ToString();
					this.mPids.Find(p => string.Compare(p.Name, selectedPid, StringComparison.OrdinalIgnoreCase) == 0).IsSelected = true;
				}

				if (this.mOnlyShowEctsBetweenRangeCheckbox.IsChecked.Value)
				{
					if (string.IsNullOrEmpty(this.mEctLowRangeTextbox.Text))
					{
						MessageBox.Show("You selected the option to limit by ECT but did not enter a minimum value.", CmrHisto.Properties.Resources.MessageBoxEctError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return;
					}

					if (string.IsNullOrEmpty(this.mEctHighRangeTextbox.Text))
					{
						MessageBox.Show("You selected the option to limit by ECT but did not enter a maximum value.", CmrHisto.Properties.Resources.MessageBoxEctError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return;
					}

					int ectLowValue = 0;
					int ectHighValue = 0;
					if (!int.TryParse(this.mEctLowRangeTextbox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out ectLowValue) || !int.TryParse(this.mEctHighRangeTextbox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out ectHighValue))
					{
						MessageBox.Show("You selected the option to limit by ECT but at least one of the values are not integers.", CmrHisto.Properties.Resources.MessageBoxEctError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return;
					}

					if (ectLowValue >= ectHighValue)
					{
						MessageBox.Show("You selected the option to limit by ECT but but the minimum is greater than or equal to the maximum.", CmrHisto.Properties.Resources.MessageBoxEctError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return;
					}
				}

				((CmrHistoMain)this.Owner).ShowPidLabels = this.mShowPidLabelsCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).ShowSampleSize = this.mShowSampleSizeCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).ShowLastValue = this.mShowLastValueCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).ShowMinAndMax = this.mShowMinAndMaxCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).ShowAverage = this.mShowAverageCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).ShowCustomPidLabels = this.mUseCustomPidNamesCheckbox.IsChecked.Value;
				((CmrHistoMain)this.Owner).UpdateShowMenuItems();
				
				if (this.mUseCustomYAxisCheckbox.IsChecked.Value)
				{
					((CmrHistoMain)this.Owner).SetupYAxisPid(true, this.mCustomYAxisPidDropdown.SelectedItem.ToString());
				}
				else
				{
					((CmrHistoMain)this.Owner).SetupYAxisPid(false, "PRatio");
				}

				this.DialogResult = true;
			}
		}

		/// <summary>
		/// Handle the user checking the show between ECT checkbox. If they checked it, validate the values, if they unchecked it clear the values and disable the textboxes.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void OnlyShowEctsBetweenRangeCheckboxChecked(object sender, RoutedEventArgs e)
		{
			if (this.mOnlyShowEctsBetweenRangeCheckbox.IsChecked.Value)
			{
				this.mEctLowRangeTextbox.IsEnabled = true;
				this.mEctHighRangeTextbox.IsEnabled = true;
			}
			else
			{
				this.mEctLowRangeTextbox.Text = string.Empty;
				this.mEctHighRangeTextbox.Text = string.Empty;
				this.mEctLowRangeTextbox.IsEnabled = false;
				this.mEctHighRangeTextbox.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handle the user selecting or deselecting a PID. If they selected a PID, enable the go button, if there aren't any PIDs selected disable the go button.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void PidListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.mPidListbox.SelectedItems.Count > 0 && ((this.mShowMinAndMaxCheckbox.IsChecked.Value || this.mShowLastValueCheckbox.IsChecked.Value) || this.mShowAverageCheckbox.IsChecked.Value))
			{
				this.mGoButton.IsEnabled = true;
			}
			else
			{
				this.mGoButton.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handles the user clicking the Show PID Labels checkbox. Update the data member.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowPidLabelsClick(object sender, RoutedEventArgs e)
		{
			this.mUseCustomPidNamesCheckbox.IsEnabled = this.mShowPidLabelsCheckbox.IsChecked.Value;
		}

		/// <summary>
		/// Handle the user clicking the Use Custom Y-Axis checkbox.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void UseCustomYAxis_Click(object sender, RoutedEventArgs e)
		{
			if (this.mUseCustomYAxisCheckbox.IsChecked.Value)
			{
				this.mCustomYAxisPidDropdown.IsEnabled = true;
				this.mOnlyPositiveRatioCheckbox.IsEnabled = false;
				this.mOnlyPositiveRatioCheckbox.IsChecked = false;
				this.mAutoScaleYAxisCheckbox.Content = "Auto Scale Y-Axis";
				if (!Settings.Default.DontShowScaleWarning)
				{
					new WarningDialog().ShowDialog();
				}
			}
			else
			{
				this.mCustomYAxisPidDropdown.IsEnabled = false;
				if (this.mAutoScaleYAxisCheckbox.IsChecked.Value)
				{
					this.mOnlyPositiveRatioCheckbox.IsEnabled = true;
				}

				this.mAutoScaleYAxisCheckbox.Content = "Auto Scale PRatio";
				this.mAutoScaleYAxisCheckbox.IsChecked = false;
				this.mAutoScaleYAxisCheckbox.IsEnabled = true;
			}
		}

		/// <summary>
		/// Handle the user clicking the min/max, average or last value checkbox. If there is a PID selected, and at least one of the checkboxes is checked enable the go button, otherwise disable it.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ValueSelectionChanged(object sender, RoutedEventArgs e)
		{
			if (this.mPidListbox.SelectedItems.Count > 0 && ((this.mShowMinAndMaxCheckbox.IsChecked.Value || this.mShowAverageCheckbox.IsChecked.Value) || this.mShowLastValueCheckbox.IsChecked.Value))
			{
				this.mGoButton.IsEnabled = true;
			}
			else
			{
				this.mGoButton.IsEnabled = false;
			}
		}
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="PidSelectionDialog"/> class.
		/// </summary>
		/// <param name="pids">A <see cref="List"/> of <see cref="PidInformation"/> objects to populate the select box with.</param>
		/// <param name="pressureRatioLogged">A flag indicating whether or not a PRatio pid was logged.</param>
		/// <param name="ectPidFound">A flag indicating whether or not the ECT pid was logged.</param>
		/// <param name="requireCustomYAxis">A flag indicating whether or not the user must select a custom Y-Axis.</param>
		internal PidSelectionDialog(List<PidInformation> pids, bool pressureRatioLogged, bool ectPidFound, bool requireCustomYAxis)
		{
			this.InitializeComponent();
			this.mPids = pids;
			foreach (PidInformation information in this.mPids)
			{
				int index = this.mPidListbox.Items.Add(information.Name);
				if (information.IsSelected)
				{
					this.mPidListbox.SelectedItems.Add(this.mPidListbox.Items[index]);
				}

				this.mCustomYAxisPidDropdown.Items.Add(information.Name);
			}

			if (!pressureRatioLogged && !requireCustomYAxis)
			{
				this.mPidsUsedTextBlock.Text = CmrHisto.Properties.Resources.LogMissingRatioPid;
			}
			else if (requireCustomYAxis)
			{
				this.mPidsUsedTextBlock.Text = CmrHisto.Properties.Resources.LogMissingRatioCalculationPid;
				this.mCustomYAxisPidDropdown.IsEnabled = true;
				this.mOnlyPositiveRatioCheckbox.IsEnabled = false;
				this.mOnlyPositiveRatioCheckbox.IsChecked = false;
				this.mAutoScaleYAxisCheckbox.Content = "Auto Scale Y-Axis";
				this.mAutoScaleYAxisCheckbox.IsChecked = true;
				this.mUseCustomYAxisCheckbox.IsChecked = true;
			}

			if (!ectPidFound)
			{
				this.mPidsUsedTextBlock.Text = this.mPidsUsedTextBlock.Text + CmrHisto.Properties.Resources.LogMissingEctPid;
				this.mOnlyShowEctsBetweenRangeCheckbox.IsEnabled = false;
				this.mEctHighRangeTextbox.IsEnabled = false;
				this.mEctLowRangeTextbox.IsEnabled = false;
			}

			if (Settings.Default.AutomaticallyLoadRpmScale || Settings.Default.AutomaticallyLoadYAxisScale)
			{
				this.mPidsUsedTextBlock.Text = this.mPidsUsedTextBlock.Text + CmrHisto.Properties.Resources.LoadScaleFromFileOverrideWarning;
			}
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets a value indicating whether or not the auto scale RPM checkbox is checked.
		/// </summary>
		/// <value><c>True</c> if the checkbox is checked, <c>False</c> otherwise.</value>
		internal bool AutoScaleRpm
		{
			get
			{
				return this.mAutoScaleRpmCheckbox.IsChecked.Value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the auto scale y axis checkbox is checked.
		/// </summary>
		/// <value><c>True</c> if the checkbox is checked, <c>False</c> otherwise.</value>
		internal bool AutoScaleYAxis
		{
			get
			{
				return this.mAutoScaleYAxisCheckbox.IsChecked.Value;
			}
		}

		/// <summary>
		/// Gets the value entered in the ECT high textbox.
		/// </summary>
		/// <value>The user entered value if it is a valid number, 0 otherwise.</value>
		internal int EctRangeHighValue
		{
			get
			{
				int num;
				if (int.TryParse(this.mEctHighRangeTextbox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out num))
				{
					return num;
				}

				return 0;
			}
		}

		/// <summary>
		/// Gets the value entered in the ECT low textbox.
		/// </summary>
		/// <value>The user entered value if it is a valid number, 0 otherwise.</value>
		internal int EctRangeLowValue
		{
			get
			{
				int num;
				if (int.TryParse(this.mEctLowRangeTextbox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out num))
				{
					return num;
				}

				return 0;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the ECT range checkbox is checked or not.
		/// </summary>
		/// <value><c>True</c> if the checkbox is checked, <c>False</c> otherwise.</value>
		internal bool OnlyShowEctsBetweenRange
		{
			get
			{
				return this.mOnlyShowEctsBetweenRangeCheckbox.IsChecked.Value;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the boost only checkbox is checked or not.
		/// </summary>
		/// <value><c>True</c> if the checkbox is checked, <c>False</c> otherwise.</value>
		internal bool UseOnlyBoostPRatios
		{
			get
			{
				return this.mOnlyPositiveRatioCheckbox.IsChecked.Value;
			}
		}
		#endregion
	}
}
