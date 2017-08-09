// <copyright file="DataToColorDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CmrHisto.Enums;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for DataToColorDialog.xaml
	/// </summary>
	public sealed partial class DataToColorDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handles the user clicking the cancel button and closes the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handles the user clicking the highlight button.  Does some validation of the PID selection and saves the info if all is well.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void HighlightButtonClick(object sender, RoutedEventArgs e)
		{
			if (!this.ValueAboveIsValid())
			{
				if (this.mNumberTab.IsSelected)
				{
					MessageBox.Show("You must enter a numeric value in the \"Value to change color\" texbox!", CmrHisto.Properties.Resources.MessageBoxGeneralError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}
				else
				{
					MessageBox.Show("You must enter a numeric value in the \"Range Min \" and \"Range Max\" texboxes!", CmrHisto.Properties.Resources.MessageBoxGeneralError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}

				this.mHighlightButton.IsEnabled = false;
			}
			else
			{
				if (this.mNumberTab.IsSelected)
				{
					((CmrHistoMain)this.Owner).HighlightValueToChange = new double?(Utility.ParseDouble(this.mValueToChangeTextbox.Text));
					if (this.mShowAboveAsRed.IsChecked.HasValue && this.mShowAboveAsRed.IsChecked.Value)
					{
						((CmrHistoMain)this.Owner).ShowAboveAsRed = true;
					}

					((CmrHistoMain)this.Owner).HighlightRangeMin = null;
					((CmrHistoMain)this.Owner).HighlightRangeMax = null;
				}
				else
				{
					((CmrHistoMain)this.Owner).HighlightRangeMin = new double?(Utility.ParseDouble(this.mRangeLowerTextbox.Text));
					((CmrHistoMain)this.Owner).HighlightRangeMax = new double?(Utility.ParseDouble(this.mRangeUpperTextbox.Text));
					if (this.mRangeShowInsideAsRed.IsChecked.HasValue && this.mRangeShowInsideAsRed.IsChecked.Value)
					{
						((CmrHistoMain)this.Owner).ShowInsideAsRed = true;
					}

					((CmrHistoMain)this.Owner).HighlightValueToChange = null;
				}

				((CmrHistoMain)this.Owner).SetHighlightPid((string)this.mPidListbox.SelectedItem);
				((CmrHistoMain)this.Owner).HighlightParameter = (DataType)this.mHighlightParameterDropdown.SelectedIndex;

				this.Close();
				((CmrHistoMain)this.Owner).OpenAndProcessCsv(string.Empty);
			}
		}

		/// <summary>
		/// Handle the user changing the selected PID. Enable the highlight button if they have one selected, disable it otherwise.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void PidListboxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.ToggleHighlightButton();
		}

		/// <summary>
		/// Handle the key up when the user is typing in the value textbox. Validate what they've entered is a number.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ValueToChangeTextbox_KeyUp(object sender, KeyEventArgs e)
		{
			this.ToggleHighlightButton();
		}

		/// <summary>
		/// Handle the key up when the user is typing in the range lower value textbox. Validate what they've entered is a number.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void RangeLowerTextbox_KeyUp(object sender, KeyEventArgs e)
		{
			this.ToggleHighlightButton();
		}

		/// <summary>
		/// Handle the key up when the user is typing in the range lower value textbox. Validate what they've entered is a number.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void RangeUpperTextbox_KeyUp(object sender, KeyEventArgs e)
		{
			this.ToggleHighlightButton();
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Attempt to parse the text in the value textbox to a number.
		/// </summary>
		/// <returns>The user entered value if it is a number, 0 if it isn't.</returns>
		private bool ValueAboveIsValid()
		{
			if (this.mNumberTab.IsSelected)
			{
				double result = 0;
				if (!double.TryParse(this.mValueToChangeTextbox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out result))
				{
					return false;
				}
			}
			else
			{
				double lower = 0;
				if (!double.TryParse(this.mRangeLowerTextbox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out lower))
				{
					return false;
				}

				double upper = 0;
				if (!double.TryParse(this.mRangeUpperTextbox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out upper))
				{
					return false;
				}

				if (lower >= upper)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Enables or disables the highlight button based on a pid being selected and the values being correct for color changing
		/// </summary>
		private void ToggleHighlightButton()
		{
			if (this.mPidListbox.SelectedItem != null && this.ValueAboveIsValid())
			{
				this.mHighlightButton.IsEnabled = true;
			}
			else
			{
				this.mHighlightButton.IsEnabled = false;
			}
		}
		#endregion

		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="DataToColorDialog"/> class.
		/// </summary>
		/// <param name="pids">A <see cref="ReadOnlyCollection"/> of <see cref="PidInformation"/> objects.</param>
		/// <param name="pidToHighlight">The id of the column (PID) that is currently highlighted.</param>
		/// <param name="valueToChange">The value at which to change from red to yellow.</param>
		/// <param name="rangeMin">The min of the range at which to change from red to yellow/</param>
		/// <param name="rangeMax">The max of the range at which to change from red to yellow/</param>
		/// <param name="highlightParameter">The currently selected value to highlight on, Min, Max etc.</param>
		public DataToColorDialog(ReadOnlyCollection<PidInformation> pids, int? pidToHighlight, double? valueToChange, double? rangeMin, double? rangeMax, int highlightParameter)
		{
			this.InitializeComponent();
			int currentPid = -10;
			if (pidToHighlight.HasValue)
			{
				currentPid = pidToHighlight.Value;
			}

			this.mValueToChangeTextbox.Text = valueToChange.HasValue ? valueToChange.Value.ToString(CultureInfo.CurrentCulture) : CmrHisto.Properties.Resources.Zero;
			this.mRangeLowerTextbox.Text = rangeMin.HasValue ? rangeMin.Value.ToString(CultureInfo.CurrentCulture) : CmrHisto.Properties.Resources.Zero;
			this.mRangeUpperTextbox.Text = rangeMax.HasValue ? rangeMax.Value.ToString(CultureInfo.CurrentCulture) : CmrHisto.Properties.Resources.One;
			this.mHighlightParameterDropdown.SelectedIndex = highlightParameter;
			if (pids != null)
			{
				foreach (PidInformation information in pids)
				{
					if (information.IsSelected)
					{
						int selectedIndex = this.mPidListbox.Items.Add(information.Name);
						if (information.ColumnNumber == currentPid)
						{
							this.mPidListbox.SelectedItem = this.mPidListbox.Items[selectedIndex];
						}
					}
				}
			}

			if (valueToChange.HasValue && !rangeMin.HasValue && !rangeMax.HasValue)
			{
				this.mNumberTab.IsSelected = true;
				this.mRangeTab.IsSelected = false;
			}
			else if (!valueToChange.HasValue && rangeMin.HasValue && rangeMax.HasValue)
			{
				this.mNumberTab.IsSelected = false;
				this.mRangeTab.IsSelected = true;
			}

			this.ToggleHighlightButton();
		}
		#endregion
	}
}
