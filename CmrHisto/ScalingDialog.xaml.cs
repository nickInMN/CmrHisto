// <copyright file="ScalingDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for ScalingDialog.xaml
	/// </summary>
	public sealed partial class ScalingDialog : Window
	{
		#region Private Event Handlers
		/// <summary>
		/// Handle the user clicking the apply button and update the scales.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ApplyButtonClick(object sender, RoutedEventArgs e)
		{
			if (this.RecalculateScale)
			{
				this.Scale = new List<ScalingInformation>();
				this.RebuildScale();
			}

			this.UpdateScale();
		}

		/// <summary>
		/// Handle the user clicking the cancel button and close the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handle the user clicking the defaults button and populate the dialog with a default scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void DefaultsButtonClick(object sender, RoutedEventArgs e)
		{
			this.FillDialog(Utility.SetDefaults(this.IsRpm));
		}

		/// <summary>
		/// Handle the user clicking the load saved scale menu item and attempt to load the selected scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void LoadSavedClick(object sender, RoutedEventArgs e)
		{
			List<ScalingInformation> newScale = Utility.LoadScaleFromFile(this.IsRpm, this.YAxisPid, string.Empty);
			if (newScale != null)
			{
				if (newScale.Count > 0)
				{
					this.FillDialog(newScale);
					this.RecalculateScale = true;
				}
			}
			else
			{
				MessageBox.Show("There was an error loading the scale file. Please make sure the file is a valid CmrHisto scale file. No changes have been made.", CmrHisto.Properties.Resources.MessageBoxScaleError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		/// <summary>
		/// Handle the user clicking the save scale menu item and attempt to save the current scale to a file.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void SaveScaleClick(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveDialog = new SaveFileDialog
			{
				FileName = string.Empty
			};

			if (this.IsRpm)
			{
				saveDialog.Filter = "RPM Scale (*.csv)|*.csv";
			}
			else
			{
				saveDialog.Filter = this.YAxisPid + " Scale (*.csv)|*.csv";
			}

			saveDialog.RestoreDirectory = true;
			string scaleType = string.Empty;
			if (saveDialog.ShowDialog().Value)
			{
				if (this.IsRpm)
				{
					scaleType = "RPM SCALE";
				}
				else
				{
					scaleType = this.YAxisPid + " SCALE";
				}

				try
				{
					using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
					{
						writer.WriteLine(string.Concat(new object[] { Assembly.GetExecutingAssembly().GetName().Name, " ", Assembly.GetExecutingAssembly().GetName().Version, ",", scaleType }));
						for (int i = 1; i < 18; i++)
						{
							double lowValue = Utility.ParseDouble(((TextBox)this.mTextGrid.FindName("mLowRow" + i.ToString(CultureInfo.CurrentCulture))).Text);
							double value = Utility.ParseDouble(((TextBox)this.mTextGrid.FindName("mValueRow" + i.ToString(CultureInfo.CurrentCulture))).Text);
							double highValue = Utility.ParseDouble(((TextBox)this.mTextGrid.FindName("mHighRow" + i.ToString(CultureInfo.CurrentCulture))).Text);
							writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", i - 1, lowValue, value, highValue));
						}
					}
				}
				catch (Exception)
				{
					MessageBox.Show("There was an error writing the file. Please try again.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}
			}
		}

		/// <summary>
		/// Make sure the user entered a valid number.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void TextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			double doubleValue;
			TextBox textBox = sender as TextBox;
			if (!double.TryParse(textBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out doubleValue))
			{
				MessageBox.Show("You must enter a numerical value.", CmrHisto.Properties.Resources.MessageBoxScaleError, MessageBoxButton.OK, MessageBoxImage.Hand);
				textBox.Text = CmrHisto.Properties.Resources.Zero;
			}
			
			// Update the value.
			if (textBox.Name.Contains("Low"))
			{
				int rowNumber = int.Parse(textBox.Name.Remove(0, 7), NumberStyles.Integer, CultureInfo.CurrentCulture);

				TextBox highValueTextBox = this.FindName("mHighRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
				if (highValueTextBox != null)
				{
					TextBox valueTextBox = this.FindName("mValueRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
					if (valueTextBox != null)
					{
						double newValue = ((Utility.ParseDouble(highValueTextBox.Text) - Utility.ParseDouble(textBox.Text)) / 2) + Utility.ParseDouble(textBox.Text);
						valueTextBox.Text = newValue.ToString(CultureInfo.CurrentCulture);
					}
				}
			}
			else if (textBox.Name.Contains("High"))
			{
				int rowNumber = int.Parse(textBox.Name.Remove(0, 8), NumberStyles.Integer, CultureInfo.CurrentCulture);

				TextBox lowValueTextBox = this.FindName("mLowRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
				if (lowValueTextBox != null)
				{
					TextBox valueTextBox = this.FindName("mValueRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
					if (valueTextBox != null)
					{
						double newValue = ((Utility.ParseDouble(textBox.Text) - Utility.ParseDouble(lowValueTextBox.Text)) / 2) + Utility.ParseDouble(lowValueTextBox.Text);
						valueTextBox.Text = newValue.ToString(CultureInfo.CurrentCulture);
					}
				}
			}
		}

		/// <summary>
		/// Handles the user clicking the "Manually Enter Scale" menu item. This will enable the high and low values textboxes and disable auto scale generation.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ManuallyEnterScaleMenuItemClick(object sender, RoutedEventArgs e)
		{
			this.UpdateTextBoxes(this.mMaunalScaleCheckbox.IsChecked);
			if (this.mMaunalScaleCheckbox.IsChecked)
			{
				this.mLowRow1.Focus();
			}
			else
			{
				this.mValueRow1.Focus();
			}
		}

		/// <summary>
		/// Handles the user leaving the value textbox of the first row and updates the scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void FirstValueRow_LostFocus(object sender, RoutedEventArgs e)
		{
			double row1Value = Utility.ParseDouble(this.mValueRow1.Text);
			double row2Value = Utility.ParseDouble(this.mValueRow2.Text);
			double difference = row2Value - row1Value;
			this.RecalculateScale = true;

			difference = Math.Round(difference / 2, this.DecimalPrecision);
			this.mHighRow1.Text = Math.Round(row1Value + difference, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
			this.mLowRow2.Text = Math.Round(row1Value + difference + this.ScaleIncrement, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Handles the user leaving the value textbox of one of the 15 middle rows and updates the scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void MiddleValueRow_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox textBox = sender as TextBox;

			// Get the row number of the texbot that changed
			int rowNumber = int.Parse(textBox.Name.Remove(0, 9), NumberStyles.Integer, CultureInfo.CurrentCulture);

			// Get the previous, current, and next values.
			double previousRowValue = Utility.ParseDouble(((TextBox)this.FindName("mValueRow" + (rowNumber - 1).ToString(CultureInfo.CurrentCulture))).Text);
			double currentRowValue = Utility.ParseDouble(textBox.Text);
			double nextRowValue = Utility.ParseDouble(((TextBox)this.FindName("mValueRow" + (rowNumber + 1).ToString(CultureInfo.CurrentCulture))).Text);

			// Get a reference to the previous row's high textbox and the next row's low textbox.
			TextBox previousRowHighTextbox = this.FindName("mHighRow" + (rowNumber - 1).ToString(CultureInfo.CurrentCulture)) as TextBox;
			TextBox currentRowLowTextbox = this.FindName("mLowRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
			TextBox currentRowHighTextbox = this.FindName("mHighRow" + rowNumber.ToString(CultureInfo.CurrentCulture)) as TextBox;
			TextBox nextRowLowTextbox = this.FindName("mLowRow" + (rowNumber + 1).ToString(CultureInfo.CurrentCulture)) as TextBox;

			double previousDifference = currentRowValue - previousRowValue;
			double nextDifference = nextRowValue - currentRowValue;
			this.RecalculateScale = true;

			previousDifference = Math.Round(previousDifference / 2, this.DecimalPrecision);
			previousRowHighTextbox.Text = Math.Round(previousRowValue + previousDifference, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
			currentRowLowTextbox.Text = Math.Round(previousRowValue + previousDifference + this.ScaleIncrement, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);

			nextDifference = Math.Round(nextDifference / 2, this.DecimalPrecision);
			currentRowHighTextbox.Text = Math.Round(currentRowValue + nextDifference, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
			nextRowLowTextbox.Text = Math.Round(currentRowValue + nextDifference + this.ScaleIncrement, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Handles the user leaving the last row's value textbox and updates the scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void LastValueRow_LostFocus(object sender, RoutedEventArgs e)
		{
			double row17Value = Utility.ParseDouble(this.mValueRow17.Text);
			double row16Value = Utility.ParseDouble(this.mValueRow16.Text);
			double difference = row17Value - row16Value;
			this.RecalculateScale = true;

			difference = Math.Round(difference / 2, this.DecimalPrecision);
			this.mHighRow16.Text = Math.Round(row16Value + difference, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
			this.mLowRow17.Text = Math.Round(row16Value + difference + this.ScaleIncrement, this.DecimalPrecision).ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Select the text of the textbox when it gets focus. This makes it easier to quickly change scale.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void Textbox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (CmrHisto.Properties.Settings.Default.SelectScaleValuesOnFocus)
			{
				TextBox tb = (TextBox)e.OriginalSource;
				tb.Dispatcher.BeginInvoke(
					new Action(delegate
					{
						tb.SelectAll();
					}),
					System.Windows.Threading.DispatcherPriority.Input);
			}
		}
		#endregion

		#region Private Properties
		/// <summary>
		/// Gets or sets the scale to use for this dialog.
		/// </summary>
		/// <value>The <see cref="List"/> of <see cref="ScalingInformation"/> objects representing the scale for this dialog.</value>
		private List<ScalingInformation> Scale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether we are working with the RPM scale or not.
		/// </summary>
		/// <value><c>True</c> if we are, <c>False</c> otherwise.</value>
		private bool IsRpm { get; set; }

		/// <summary>
		/// Gets or sets the text to use for the Y-Axis PID.
		/// </summary>
		/// <value>The name of the PID being used for the Y-Axis.</value>
		private string YAxisPid { get; set; }

		/// <summary>
		/// Gets or sets the number of decimal points to use.
		/// </summary>
		/// <value>If this is RPM the increment should be 0, if this is PRatio it should be 3.</value>
		private int DecimalPrecision { get; set; }

		/// <summary>
		/// Gets or sets the increment to use for the scale
		/// </summary>
		/// <value>If this is RPM the increment should be 1, if this is PRatio it should be .001.</value>
		private double ScaleIncrement { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the scale should be recalculated when the user clicks apply.
		/// </summary>
		/// <value><c>True</c> if the scale should be recalculated, <c>False</c> if it shouldn't.</value>
		private bool RecalculateScale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has opened a data file yet.
		/// </summary>
		/// <value><c>False</c> if they have not opened a file, <c>True</c> if they have.</value>
		private bool FileOpened { get; set; }
		#endregion

		#region Private Methods

		/// <summary>
		/// Populate the dialog from the supplied scale.
		/// </summary>
		/// <param name="scale">A <see cref="List"/> of <see cref="ScalingInformation"/> objects.</param>
		private void FillDialog(List<ScalingInformation> scale)
		{
			this.mLowRow1.Text = scale[0].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow1.Text = scale[0].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow1.Text = scale[0].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow2.Text = scale[1].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow2.Text = scale[1].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow2.Text = scale[1].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow3.Text = scale[2].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow3.Text = scale[2].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow3.Text = scale[2].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow4.Text = scale[3].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow4.Text = scale[3].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow4.Text = scale[3].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow5.Text = scale[4].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow5.Text = scale[4].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow5.Text = scale[4].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow6.Text = scale[5].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow6.Text = scale[5].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow6.Text = scale[5].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow7.Text = scale[6].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow7.Text = scale[6].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow7.Text = scale[6].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow8.Text = scale[7].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow8.Text = scale[7].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow8.Text = scale[7].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow9.Text = scale[8].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow9.Text = scale[8].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow9.Text = scale[8].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow10.Text = scale[9].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow10.Text = scale[9].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow10.Text = scale[9].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow11.Text = scale[10].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow11.Text = scale[10].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow11.Text = scale[10].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow12.Text = scale[11].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow12.Text = scale[11].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow12.Text = scale[11].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow13.Text = scale[12].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow13.Text = scale[12].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow13.Text = scale[12].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow14.Text = scale[13].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow14.Text = scale[13].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow14.Text = scale[13].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow15.Text = scale[14].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow15.Text = scale[14].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow15.Text = scale[14].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow16.Text = scale[15].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow16.Text = scale[15].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow16.Text = scale[15].MaxValue.ToString(CultureInfo.CurrentCulture);
			this.mLowRow17.Text = scale[16].MinValue.ToString(CultureInfo.CurrentCulture);
			this.mValueRow17.Text = scale[16].Value.ToString(CultureInfo.CurrentCulture);
			this.mHighRow17.Text = scale[16].MaxValue.ToString(CultureInfo.CurrentCulture);
		}

		/// <summary>
		/// Rebuild the dialog's scale based on the textbox values.
		/// </summary>
		private void RebuildScale()
		{
			ScalingInformation info = null;
			double previousValue = 0;
			double currentValue = 0;
			double nextValue = 0;

			for (int i = 1; i < 18; i++)
			{
				info = new ScalingInformation();
				if (i == 1)
				{
					currentValue = Utility.ParseDouble(this.mValueRow1.Text);
					nextValue = Utility.ParseDouble(this.mValueRow2.Text);

					info.MinValue = 0;
					info.MaxValue = Math.Round(((nextValue - currentValue) / 2) + currentValue, this.DecimalPrecision);
				}
				else if (i > 1 && i < 17)
				{
					previousValue = Utility.ParseDouble(((TextBox)this.FindName("mValueRow" + (i - 1).ToString(CultureInfo.CurrentCulture))).Text);
					currentValue = Utility.ParseDouble(((TextBox)this.FindName("mValueRow" + i.ToString(CultureInfo.CurrentCulture))).Text);
					nextValue = Utility.ParseDouble(((TextBox)this.FindName("mValueRow" + (i + 1).ToString(CultureInfo.CurrentCulture))).Text);

					info.MinValue = Math.Round(((currentValue - previousValue) / 2) + previousValue + this.ScaleIncrement, this.DecimalPrecision);
					info.MaxValue = Math.Round(((nextValue - currentValue) / 2) + currentValue, this.DecimalPrecision);
				}
				else
				{
					previousValue = Utility.ParseDouble(this.mValueRow16.Text);
					currentValue = Utility.ParseDouble(this.mValueRow17.Text);

					info.MinValue = Math.Round(((currentValue - previousValue) / 2) + previousValue + this.ScaleIncrement, this.DecimalPrecision);
					info.MaxValue = 99999;
				}

				info.Value = Math.Round(currentValue, this.DecimalPrecision);
				this.Scale.Add(info);
			}
		}

		/// <summary>
		/// Save the new scale.
		/// </summary>
		private void UpdateScale()
		{
			((CmrHistoMain)this.Owner).SetScale(this.Scale, this.IsRpm);
			
			if (this.FileOpened)
			{
				((CmrHistoMain)this.Owner).OpenAndProcessCsv(string.Empty);
			}
			
			this.Close();
		}

		/// <summary>
		/// Set the enabled state of the text boxes.
		/// </summary>
		/// <param name="isEnabled"><c>True</c> if the textbox should be enabled, <c>False</c> otherwise.</param>
		private void UpdateTextBoxes(bool isEnabled)
		{
			this.mLowRow1.IsEnabled = isEnabled;
			this.mValueRow1.IsEnabled = !isEnabled;
			this.mHighRow1.IsEnabled = isEnabled;
			this.mLowRow2.IsEnabled = isEnabled;
			this.mValueRow2.IsEnabled = !isEnabled;
			this.mHighRow2.IsEnabled = isEnabled;
			this.mLowRow3.IsEnabled = isEnabled;
			this.mValueRow3.IsEnabled = !isEnabled;
			this.mHighRow3.IsEnabled = isEnabled;
			this.mLowRow4.IsEnabled = isEnabled;
			this.mValueRow4.IsEnabled = !isEnabled;
			this.mHighRow4.IsEnabled = isEnabled;
			this.mLowRow5.IsEnabled = isEnabled;
			this.mValueRow5.IsEnabled = !isEnabled;
			this.mHighRow5.IsEnabled = isEnabled;
			this.mLowRow6.IsEnabled = isEnabled;
			this.mValueRow6.IsEnabled = !isEnabled;
			this.mHighRow6.IsEnabled = isEnabled;
			this.mLowRow7.IsEnabled = isEnabled;
			this.mValueRow7.IsEnabled = !isEnabled;
			this.mHighRow7.IsEnabled = isEnabled;
			this.mLowRow8.IsEnabled = isEnabled;
			this.mValueRow8.IsEnabled = !isEnabled;
			this.mHighRow8.IsEnabled = isEnabled;
			this.mLowRow9.IsEnabled = isEnabled;
			this.mValueRow9.IsEnabled = !isEnabled;
			this.mHighRow9.IsEnabled = isEnabled;
			this.mLowRow10.IsEnabled = isEnabled;
			this.mValueRow10.IsEnabled = !isEnabled;
			this.mHighRow10.IsEnabled = isEnabled;
			this.mLowRow11.IsEnabled = isEnabled;
			this.mValueRow11.IsEnabled = !isEnabled;
			this.mHighRow11.IsEnabled = isEnabled;
			this.mLowRow12.IsEnabled = isEnabled;
			this.mValueRow12.IsEnabled = !isEnabled;
			this.mHighRow12.IsEnabled = isEnabled;
			this.mLowRow13.IsEnabled = isEnabled;
			this.mValueRow13.IsEnabled = !isEnabled;
			this.mHighRow13.IsEnabled = isEnabled;
			this.mLowRow14.IsEnabled = isEnabled;
			this.mValueRow14.IsEnabled = !isEnabled;
			this.mHighRow14.IsEnabled = isEnabled;
			this.mLowRow15.IsEnabled = isEnabled;
			this.mValueRow15.IsEnabled = !isEnabled;
			this.mHighRow15.IsEnabled = isEnabled;
			this.mLowRow16.IsEnabled = isEnabled;
			this.mValueRow16.IsEnabled = !isEnabled;
			this.mHighRow16.IsEnabled = isEnabled;
			this.mLowRow17.IsEnabled = isEnabled;
			this.mValueRow17.IsEnabled = !isEnabled;
			this.mHighRow17.IsEnabled = isEnabled;
		}
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="ScalingDialog"/> class.
		/// </summary>
		/// <param name="isRpm">A value indicating whether or not the scale is the RPM scale.</param>
		/// <param name="currentScale">The current scale to be edited.</param>
		/// <param name="yAxisPid">The text for the Y Axis PID. This is used if isRpm is false.</param>
		/// <param name="fileOpened">A flag indicating whether or not the user has a data file opened.  If false, don't try to update data on scale change.</param>
		internal ScalingDialog(bool isRpm, List<ScalingInformation> currentScale, string yAxisPid, bool fileOpened)
		{
			this.InitializeComponent();
			this.IsRpm = isRpm;
			this.YAxisPid = yAxisPid;
			if (!this.IsRpm)
			{
				this.Title = yAxisPid + " Scaling";
				this.mGroupBox.Header = "Custom " + yAxisPid + " Scaling";
			}

			this.DecimalPrecision = 1;
			this.ScaleIncrement = 0.1;
			if (!this.IsRpm)
			{
				this.DecimalPrecision = 3;
				this.ScaleIncrement = 0.001;
			}

			this.Scale = currentScale;
			this.FillDialog(this.Scale);
			this.FileOpened = fileOpened;
		}
		#endregion
	}
}
