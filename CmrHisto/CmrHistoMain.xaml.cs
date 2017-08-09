// <copyright file="CmrHistoMain.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using CmrHisto.Enums;
using CmrHisto.Properties;
using CmrHisto.Views;
using log4net;
using Microsoft.Win32;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for CmrHistoMain.xaml
	/// </summary>
	public sealed partial class CmrHistoMain : Window, IDisposable
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(CmrHistoMain));

        #region Private Data Members
        private ProgressDialog mProgressDialog;
		private DataTable mOriginalDataSet;
		private List<PidInformation> mPids;
		private CellData[,] mProcessedData;
		private List<ScalingInformation> mRpmScale;
		private List<ScalingInformation> mYAxisScale;
		private BackgroundWorker mWorker;
		private int? mAvgMapToUseColumn;
		private int? mBaroColumn;
		private int? mEctColumn;
		private int? mInjectorPulseWidthColum;
		private int? mOneToOneLTFuelColumn;
		private int? mOneToOneSTFuelColumn;
		private int? mPRatioColumn;
		private int? mRpmColumn;
		private int? mTwoToOneLTFuelColumn;
		private int? mTwoToOneSTFuelColumn;
		private int? mUserDefinedYAxisColumn;
		#endregion

		#region Private Event Handlers
		/// <summary>
		/// Handle the user checking the ascending sort menu and resort the data.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void AscendingSortChecked(object sender, RoutedEventArgs e)
		{
			if (this.YAxisSort != Sort.Ascending)
			{
				this.YAxisSort = Sort.Ascending;
				this.mYAxisDescendingSortMenuItem.IsChecked = false;
				this.OpenAndProcessCsv(string.Empty);
			}
		}

		/// <summary>
		/// Handle the user checking the descending sort menu and resort the data.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void DescendingSortChecked(object sender, RoutedEventArgs e)
		{
			if (this.YAxisSort != Sort.Descending)
			{
				this.YAxisSort = Sort.Descending;
				this.mYAxisAscendingSortMenuItem.IsChecked = false;
				this.OpenAndProcessCsv(string.Empty);
			}
		}

		/// <summary>
		/// Handle the user clicking cancel to stop a process.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void CancelProcess(object sender, EventArgs e)
		{
			this.mWorker.CancelAsync();
		}

		/// <summary>
		/// Handle the selected cells of the grid being changed. Enable the scale to selected menu item if at least one cell is selected.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void DataGridSelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			if (this.mDataGrid.SelectedCells.Count > 0)
			{
				this.mScaleToSelectedCellsMenuItem.IsEnabled = true;
			}
			else
			{
				this.mScaleToSelectedCellsMenuItem.IsEnabled = false;
			}
		}

		/// <summary>
		/// Handle the user clicking the x or going to File -> Exit and close the program.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void Exit(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Create a scale based on the currently selected cells.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ScaleToSelected(object sender, RoutedEventArgs e)
		{
			int lowColumn = int.MaxValue;
			int highColumn = int.MinValue;
			int lowRow = int.MaxValue;
			int highRow = int.MinValue;
			foreach (DataGridCellInfo info in this.mDataGrid.SelectedCells)
			{
				if (!info.IsValid)
				{
					return;
				}

				if (info.Column.DisplayIndex < lowColumn)
				{
					lowColumn = info.Column.DisplayIndex;
				}

				if (info.Column.DisplayIndex > highColumn)
				{
					highColumn = info.Column.DisplayIndex;
				}

				FrameworkElement cellContent = info.Column.GetCellContent(info.Item);
				if (cellContent != null)
				{
					int rowIndex = Utility.GetRowIndex((DataGridCell)cellContent.Parent);
					if (rowIndex < lowRow)
					{
						lowRow = rowIndex;
					}

					if (rowIndex > highRow)
					{
						highRow = rowIndex;
					}
				}
			}

			if (lowRow == highRow)
			{
				highRow++;
			}

			if (lowColumn == highColumn)
			{
				highColumn++;
			}

			List<ScalingInformation> newScale = Utility.CreateScale(this.mRpmScale[lowColumn].MinValue, this.mRpmScale[highColumn].MaxValue, 1, this.YAxisPid);
			if (newScale != null)
			{
				this.mRpmScale = newScale;
			}

			newScale = Utility.CreateScale(this.mYAxisScale[lowRow].MinValue, this.mYAxisScale[highRow].MaxValue, 3, this.YAxisPid);
			if (newScale != null)
			{
				this.mYAxisScale = newScale;
			}

			this.ScaleFromDataGrid = true;
			this.OpenAndProcessCsv(string.Empty);
		}

		/// <summary>
		/// Handle the user clicking the "Select Pid to Color" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void SelectPidToColorClicked(object sender, RoutedEventArgs e)
		{
			DataToColorDialog dialog = null;
			dialog = new DataToColorDialog(this.mPids.AsReadOnly(), this.PidToHighlight, this.HighlightValueToChange, this.HighlightRangeMin, this.HighlightRangeMax, (int)this.HighlightParameter);

			dialog.Owner = this;
			dialog.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Show About Dialog" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowAboutDialog(object sender, RoutedEventArgs e)
		{
			new AboutDialog { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Show Copy PID Dialog" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowCopyPidToClipboardDialog(object sender, RoutedEventArgs e)
		{
			new CopyPidDialog(this.mPids.AsReadOnly(), true) { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Show Custom PID Dialog" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowCustomPidDialog(object sender, RoutedEventArgs e)
		{
			new CustomPidsDialog { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the loaded event and check for an update if the user has selected that option.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			#if (!DEBUG)
			if (Settings.Default.CheckForUpdateOnLoad)
			{
				this.mWorker = new BackgroundWorker();
				this.mWorker.WorkerSupportsCancellation = true;
				bool successful = false;
				XmlDocument version = new XmlDocument();
				this.mWorker.DoWork += delegate(object s, DoWorkEventArgs args)
				{
					try
					{
						version.Load("http://www.cmrhisto.candnbrett.com/update/version.xml");
						successful = true;
					}
					catch (Exception)
					{
						successful = false;
					}
				};
				this.mWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
				{
					if (successful)
					{
						string executingVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
						string currentVersion = version.GetElementsByTagName("current-version")[0].InnerText;

						int executingPointVersion = int.Parse(executingVersion.Split(new char[] { '.' })[3], CultureInfo.CurrentCulture);
						int currentPointVersion = int.Parse(currentVersion.Split(new char[] { '.' })[3], CultureInfo.CurrentCulture);

						if (executingPointVersion < currentPointVersion)
						{
							UpdateDialog dialog = new UpdateDialog
							{
								Owner = this
							};

							dialog.NotCurrent(currentVersion, executingVersion);
							dialog.ShowDialog();
						}
					}
				};
				this.mWorker.RunWorkerAsync();
			}
#endif
		}

		/// <summary>
		/// Handle the user clicking the show data button.  Save their selections and process the data.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowDataMenuItem_Click(object sender, RoutedEventArgs e)
		{
			this.ShowMinAndMax = this.mShowMinAndMaxMenuItem.IsChecked;
			this.ShowAverage = this.mShowAverageMenuItem.IsChecked;
			this.ShowLastValue = this.mShowLastMenuItem.IsChecked;
			this.ShowPidLabels = this.mShowPidLabelsMenuItem.IsChecked;
			this.ShowSampleSize = this.mShowSampleSizeMenuItem.IsChecked;
			this.ShowCustomPidLabels = this.mShowCustomPidLabelsMenuItem.IsChecked;
			this.mShowCustomPidLabelsMenuItem.IsEnabled = this.ShowPidLabels;
			this.OpenAndProcessCsv(string.Empty);
		}

		/// <summary>
		/// Handle the user clicking the "Export PID Data" menu item and show the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowExportPidDialog(object sender, RoutedEventArgs e)
		{
			new CopyPidDialog(this.mPids.AsReadOnly(), false) { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Open CSV" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowOpenFileDialog(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				FileName = string.Empty,
				DefaultExt = ".csv",
				Filter = "csv files (*.csv)|*.csv"
			};
			bool? dialogResult = openFileDialog.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value)
			{
				this.ScaleFromDataGrid = false;
				this.PidToHighlight = null;
				this.HighlightValueToChange = 0;
				this.HighlightRangeMin = 0;
				this.HighlightRangeMax = 1;
				this.OpenAndProcessCsv(openFileDialog.FileName);
			}
		}

		/// <summary>
		/// Handle the user clicking the "Scale RPM" menu item and display the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowRpmScalingDialog(object sender, RoutedEventArgs e)
		{
			new ScalingDialog(true, this.mRpmScale, string.Empty, this.mOriginalDataSet != null) { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Settings" menu item and display the settings dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowSettingsDialog(object sender, RoutedEventArgs e)
		{
			new SettingsDialog(this.YAxisPid) { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Check for Updates" menu item, display the dialog and start the update check.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowUpdatesDialog(object sender, RoutedEventArgs e)
		{
			UpdateDialog updateDialog = new UpdateDialog
			{
				Owner = this
			};
			this.mWorker = new BackgroundWorker();
			this.mWorker.WorkerSupportsCancellation = true;
			bool successful = false;
			XmlDocument version = new XmlDocument();
			this.mWorker.DoWork += delegate(object s, DoWorkEventArgs args)
			{
				try
				{
					version.Load("http://www.cmrhisto.candnbrett.com/update/version.xml");
					successful = true;
				}
				catch (Exception)
				{
					successful = false;
				}
			};
			this.mWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
			{
				if (successful)
				{
					string executingVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
					string currentVersion = version.GetElementsByTagName("current-version")[0].InnerText;

					int executingPointVersion = int.Parse(executingVersion.Split(new char[] { '.' })[3], CultureInfo.CurrentCulture);
					int currentPointVersion = int.Parse(currentVersion.Split(new char[] { '.' })[3], CultureInfo.CurrentCulture);

					if (executingPointVersion < currentPointVersion)
					{
						UpdateDialog dialog = new UpdateDialog
						{
							Owner = this
						};

						dialog.NotCurrent(currentVersion, executingVersion);
						dialog.ShowDialog();
					}
				}
				else
				{
					updateDialog.SetText(CmrHisto.Properties.Resources.VersionCheckError);
				}
			};

			this.mWorker.RunWorkerAsync();
			updateDialog.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Scale Y-Axis" menu item and show the dialog.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowYAxisScalingDialog(object sender, RoutedEventArgs e)
		{
			new ScalingDialog(false, this.mYAxisScale, this.YAxisPid, this.mOriginalDataSet != null) { Owner = this }.ShowDialog();
		}

		/// <summary>
		/// Handle the user clicking the "Save to HTML" menu item. Show the dialog, and save the HTML to the specified file if the use clicks save.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void ShowSaveToHtmlDialog(object sender, RoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog
			{
				FileName = string.Empty,
				DefaultExt = ".html",
				Filter = "html files (*.html)|*.html",
				RestoreDirectory = true
			};
			bool? dialogResult = saveFileDialog.ShowDialog();
			if (dialogResult.HasValue && dialogResult.Value)
			{
				StringBuilder htmlData = new StringBuilder();
				double columnWidth = 50;
				foreach (DataGridColumn column in this.mDataGrid.Columns)
				{
					columnWidth += column.ActualWidth + 45;
				}

				htmlData.AppendFormat("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">\n<html dir=\"ltr\" xml:lang=\"en\" xmlns=\"http://www.w3.org/1999/xhtml\">\n\t<head>\n\t\t<title>CmrHisto Save file - {0}</title>", Path.GetFileName(saveFileDialog.FileName));
				htmlData.Append("\n\t\t<style type=\"text/css\">\n\t\t\tbody{font-family: Arial, Helvetica, sans-serif;}\n\t\t\ttd.no-cellDisplayData{font-size:0.6em; background:#FFFFFF; text-align:center;}\n\t\t\ttd.light-gray{font-size:0.6em; background:#D3D3D3;}\n\t\t\ttd.light-blue{font-size:0.6em; background:#ADD8E6;}\n\t\t\ttd.light-pink{font-size:0.6em; background:#FFB6C1;}\n\t\t\ttd.light-yellow{font-size:0.6em; background:#FFFFE0;}\n\t\t\ttd.row-header{text-align:center;}\n\t\t</style>");
				htmlData.Append("\n\t</head>\n\t<body>");
				htmlData.AppendFormat("\n\t\t<table border=\"1\" width=\"{0}\" cellpadding=\"2\" cellspacing=\"0\">\n\t\t\t<tr>\n\t\t\t\t<th width=\"50px\">\n\t\t\t\t\t&nbsp;\n\t\t\t\t</th>", Math.Ceiling(columnWidth));
				int columnNumber = 0;
				foreach (ScalingInformation information in this.mRpmScale)
				{
					htmlData.AppendFormat("\n\t\t\t\t<th width=\"{2}px\">\n\t\t\t\t\t{0} - {1}\n\t\t\t\t</th>", information.MinValue, information.MaxValue, Math.Ceiling((double)(this.mDataGrid.Columns[columnNumber].ActualWidth + 45)));
					columnNumber++;
				}

				htmlData.Append("\n\t\t\t</tr>");

				foreach (List<CellDisplayData> list in this.mDataGrid.ItemsSource)
				{
					htmlData.AppendFormat("\n\t\t\t<tr>\n\t\t\t\t<td class=\"row-header\">\n\t\t\t\t\t{0}\n\t\t\t\t</td>", list[0].YAxisToolTip.Replace(" ", "<br />"));
					foreach (CellDisplayData data in list)
					{
						string backgroundColor = "no-cellDisplayData";
						string color = data.Color;
						if (color != null && (string.Compare(color, "-1", StringComparison.OrdinalIgnoreCase) != 0))
						{
							if (string.Compare(color, "0", StringComparison.OrdinalIgnoreCase) == 0)
							{
								backgroundColor = "light-gray";
							}
							else if (string.Compare(color, "1", StringComparison.OrdinalIgnoreCase) == 0)
							{
								backgroundColor = "light-yellow";
							}
							else if (string.Compare(color, "2", StringComparison.OrdinalIgnoreCase) == 0)
							{
								backgroundColor = "light-pink";
							}
							else if (string.Compare(color, "3", StringComparison.OrdinalIgnoreCase) == 0)
							{
								backgroundColor = "light-blue";
							}
							else
							{
								backgroundColor = "no-cellDisplayData";
							}
						}

						htmlData.AppendFormat("\n\t\t\t\t<td class=\"{0}\">\n\t\t\t\t\t{1}\n\t\t\t\t</td>", backgroundColor, data.DisplayText.Replace("\n", "<br />"));
					}

					htmlData.Append("\n\t\t\t</tr>");
				}

				htmlData.Append("\n\t\t</table>\n\t</body>\n</html>");
				using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
				{
					writer.Write(htmlData.ToString());
				}
			}
		}

        /// <summary>
        /// Handle the user clicking the surface map menu item and display the dialog.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">Event parameters.</param>
        private void ShowSurfaceMapClick(object sender, RoutedEventArgs e)
        {
            SurfaceMapDialog w = new SurfaceMapDialog(this.mPids.Where(p => p.IsSelected).Select(p => !string.IsNullOrWhiteSpace(p.CustomName) && this.ShowCustomPidLabels ? p.CustomName : p.Name));
            w.Owner = this;
            w.ShowDialog();
        }

        /// <summary>
        /// Handles the opening event of the cell context menu and populates the menu and sets up the click events.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">Additional event parameters.</param>
        private void CellContextMenu_Opening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
		{
			DependencyObject dep = (DependencyObject)sender;
			int columnIndex = -1;
			int rowIndex = -1;
			while (dep != null && !(dep is DataGridCell))
			{
				dep = VisualTreeHelper.GetParent(dep);
			}

			if (dep is DataGridCell)
			{
				DataGridCell cell = dep as DataGridCell;
				columnIndex = cell.Column.DisplayIndex;
				rowIndex = Utility.GetRowIndex(cell);
			}

			if (this.YAxisSort == Sort.Descending)
			{
				rowIndex = 16 - rowIndex;
			}

			System.Windows.Controls.ContextMenu menu = new System.Windows.Controls.ContextMenu();

			List<string> pids = null;
			if (this.ShowCustomPidLabels)
			{
				pids = this.mPids.Where(p => p.IsSelected == true).Select(p => string.IsNullOrEmpty(p.CustomName) ? p.Name : p.CustomName).ToList();
			}
			else
			{
				pids = this.mPids.Where(p => p.IsSelected == true).Select(p => p.Name).ToList();
			}

			foreach (string p in pids)
			{
				System.Windows.Controls.MenuItem item = new System.Windows.Controls.MenuItem();
				item.Header = p;
				item.Name = "Column_" + columnIndex + "_Row_" + rowIndex;
				item.Click += this.GridCellContextItem_Click;
				item.Icon = new System.Windows.Controls.Image
				{
					Source = new BitmapImage(new Uri("pack://application:,,,/Icons/bar-graph.ico", UriKind.Absolute)),
					Height = 16,
					Width = 16
				};
				menu.Items.Add(item);
			}

			FrameworkElement fe = e.Source as FrameworkElement;
			fe.ContextMenu = menu;
		}

		/// <summary>
		/// Handles the user clicking an item in the cell context menu.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void GridCellContextItem_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
			string name = item.Header.ToString();
			string[] cellInfo = item.Name.Split(new char[] { '_' });
			int columnIndex = int.Parse(cellInfo[1], CultureInfo.CurrentCulture);
			int rowIndex = int.Parse(cellInfo[3], CultureInfo.CurrentCulture);
			List<double> values = this.mProcessedData[columnIndex, rowIndex].GetPidValues(this.GetPidColumFromName(name).Value);
			PidChart chart = new PidChart(name, values);
			chart.ShowDialog();
		}
		#endregion

		#region Private Properties

		/// <summary>
		/// Gets or sets a value indicating whether or not the RPM axis should be auto-scaled or not.
		/// </summary>
		/// <value><c>True</c> if it should auto-newScale, <c>False</c> otherwise.</value>
		private bool AutoScaleRpm { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the Y-Axis should be auto-scaled or not.
		/// </summary>
		/// <value><c>True</c> if it should auto-newScale, <c>False</c> otherwise.</value>
		private bool AutoScaleYAxis { get; set; }

		/// <summary>
		/// Gets or sets the upper bound for ECT limiting.
		/// </summary>
		/// <value>The user entered ECT high value.</value>
		private int EctRangeHighValue { get; set; }

		/// <summary>
		/// Gets or sets the lower bound for ECT limiting.
		/// </summary>
		/// <value>The user entered ECT low value.</value>
		private int EctRangeLowValue { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the average long term fuel trims have been calculated.
		/// </summary>
		/// <value><c>True</c> if the average has been calculated, <c>False</c> otherwise.</value>
		private bool HaveCalculatedAverageLTFuel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the average short term fuel trims have been calculated.
		/// </summary>
		/// <value><c>True</c> if the average has been calculated, <c>False</c> otherwise.</value>
		private bool HaveCalculatedAverageSTFuel { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not boost has been calculated.
		/// </summary>
		/// <value><c>True</c> if boost has been calculated, <c>False</c> otherwise.</value>
		private bool HaveCalculatedBoost { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the data should be limited to the ECT range.
		/// </summary>
		/// <value><c>True</c> if the cellDisplayData should be ECT limited, <c>False</c> otherwise.</value>
		private bool OnlyShowEctsBetweenRange { get; set; }

		/// <summary>
		/// Gets or sets the column number of the PID to highlight.
		/// </summary>
		/// <value>The column number if one has been set, <c>Null</c> otherwise.</value>
		private int? PidToHighlight { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user did a "newScale to selected" operation or not.
		/// </summary>
		/// <value><c>True</c> if they have, <c>False</c> otherwise.</value>
		private bool ScaleFromDataGrid { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has defined a custom Y-Axis.
		/// </summary>
		/// <value><c>True</c> if a PID other than PRatio should be used for the Y-Axis, <c>False</c> otherwise.</value>
		private bool UseCustomYAxis { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not only data in boost should be shown.
		/// </summary>
		/// <value><c>True</c> if only boost data should show, <c>False</c> otherwise.</value>
		private bool UseOnlyBoostPRatios { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has created a file for the RPM scale.
		/// </summary>
		/// <value><c>True</c> if the user has defined an RPM scale, <c>False</c> otherwise.</value>
		private bool UserDefinedRpmScale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the user has created a scale file for the Y-Axis scale.
		/// </summary>
		/// <value><c>True</c> if the user has defined a scale for the Y-Axis, <c>False</c> otherwise.</value>
		private bool UserDefinedYAxisScale { get; set; }

		/// <summary>
		/// Gets or sets the text of the PID the user defined for the Y-Axis.
		/// </summary>
		/// <value>The PID's name used for the Y-Axis.</value>
		private string YAxisPid { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Sort"/> order of the Y-Axis.
		/// </summary>
		/// <value>The sort order of the Y-Axis, defaults to Ascending.</value>
		private Sort YAxisSort { get; set; }
		#endregion

		#region Private Methods
		/// <summary>
		/// Create CSV data from the current data set.
		/// </summary>
		/// <param name="copyValueType">The <see cref="DataType"/> used to define the type of copy that is needed.</param>
		/// <param name="columnNumber">The number of the column (PID) to export.</param>
		/// <param name="forClipboard">A flag indicating whether or not the data will be copied to the clipboard.</param>
		/// <returns>The string containing the copied data.</returns>
		private string BuildCsvData(DataType copyValueType, int columnNumber, bool forClipboard)
		{
			StringBuilder builder = new StringBuilder();
			List<string> lines = new List<string>();
			string str = "\t";
			if (!forClipboard)
			{
				str = ",";
			}

			for (int i = 0; i < 17; i++)
			{
				builder.Clear();
				for (int j = 0; j < 17; j++)
				{
					if (this.mProcessedData[j, i].SampleSize > 0)
					{
						switch (copyValueType)
						{
							case DataType.Last:
								builder.AppendFormat("{0}{1}", this.mProcessedData[j, i].GetLastValue(columnNumber), str);
								break;

							case DataType.Minimum:
								builder.AppendFormat("{0}{1}", this.mProcessedData[j, i].GetMinimumValue(columnNumber), str);
								break;

							case DataType.Maximum:
								builder.AppendFormat("{0}{1}", this.mProcessedData[j, i].GetMaximumValue(columnNumber), str);
								break;

							case DataType.Average:
								builder.AppendFormat("{0}{1}", this.mProcessedData[j, i].GetAverageValue(columnNumber), str);
								break;
						}
					}
					else
					{
						builder.AppendFormat("0{0}", str);
					}
				}

				builder.Append("\n");
				lines.Add(builder.ToString());
			}

			if (this.YAxisSort == Sort.Descending)
			{
				lines.Reverse();
			}

			builder.Clear();

			foreach (string s in lines)
			{
				builder.Append(s);
			}

			return builder.ToString();
		}

		/// <summary>
		/// Find all of the columns (PIDs) in the CSV file and validate the necessary ones are present.
		/// </summary>
		/// <param name="fileName">The name of the CSV to open.</param>
		/// <returns>A <see cref="Status"/>.</returns>
		private Status FindColumns(string fileName)
		{
			this.mRpmColumn = null;
			this.mPRatioColumn = null;
			this.mBaroColumn = null;
			this.mAvgMapToUseColumn = null;
			this.mOneToOneLTFuelColumn = null;
			this.mTwoToOneLTFuelColumn = null;
			this.mOneToOneSTFuelColumn = null;
			this.mTwoToOneSTFuelColumn = null;
			this.mUserDefinedYAxisColumn = null;
			this.HaveCalculatedBoost = false;
			this.HaveCalculatedAverageLTFuel = false;
			this.HaveCalculatedAverageSTFuel = false;
			this.mPids = new List<PidInformation>();
			string[] pidUnits = Utility.LoadUnits(fileName);
			this.IdentifyColumnsAndAddPids(pidUnits);
			if (this.mAvgMapToUseColumn.HasValue && this.mBaroColumn.HasValue)
			{
				PidInformation calculatedBoost = new PidInformation
				{
					Name = "Boost (calculated)",
					ColumnNumber = -2,
					IsSelected = false,
					Unit = (pidUnits != null) ? pidUnits[this.mBaroColumn.Value] : string.Empty
				};
				this.mPids.Add(calculatedBoost);
				this.HaveCalculatedBoost = true;
			}

			if (this.mOneToOneLTFuelColumn.HasValue && this.mTwoToOneLTFuelColumn.HasValue)
			{
				PidInformation ltFuelAveragePid = new PidInformation
				{
					Name = "LT Fuel ADAP Avg (calculated)",
					ColumnNumber = -3,
					IsSelected = false,
					Unit = (pidUnits != null) ? pidUnits[this.mOneToOneLTFuelColumn.Value] : string.Empty
				};
				this.mPids.Add(ltFuelAveragePid);
				this.HaveCalculatedAverageLTFuel = true;
			}

			if (this.mOneToOneSTFuelColumn.HasValue && this.mTwoToOneSTFuelColumn.HasValue)
			{
				PidInformation ltFuelAveragePid = new PidInformation
				{
					Name = "ST Fuel ADAP Avg (calculated)",
					ColumnNumber = -4,
					IsSelected = false,
					Unit = (pidUnits != null) ? pidUnits[this.mOneToOneSTFuelColumn.Value] : string.Empty
				};
				this.mPids.Add(ltFuelAveragePid);
				this.HaveCalculatedAverageSTFuel = true;
			}

			if (!this.mRpmColumn.HasValue)
			{
				return Status.RpmNotFound;
			}

            if (File.Exists("CustomPidNames.xml"))
            {
                XmlDocument customPidsDocument = new XmlDocument();
                customPidsDocument.Load("CustomPidNames.xml");
                XmlNodeList pidList = customPidsDocument.SelectSingleNode("/pids").ChildNodes;
                SortedDictionary<string, string> dictionary = new SortedDictionary<string, string>();
                foreach (XmlNode pidNode in pidList)
                {
                    if (pidNode.NodeType == XmlNodeType.Element && pidNode.Attributes != null)
                    {
                        dictionary.Add(pidNode.Attributes[0].Value, pidNode.Attributes[1].Value);
                    }
                }

                foreach (PidInformation pidInfo in this.mPids)
                {
                    if (dictionary.ContainsKey(pidInfo.Name))
                    {
                        pidInfo.CustomName = dictionary[pidInfo.Name];
                    }
                }
            }

			this.mPids.Sort((one, two) => string.Compare(one.Name, two.Name, StringComparison.OrdinalIgnoreCase));
			return Status.Success;
		}

		/// <summary>
		/// Calculate the average Long Term Fuel PID.
		/// </summary>
		/// <param name="row">The <see cref="DataRow"/> to do the calculation on.</param>
		/// <param name="cellData">The <see cref="CellData"/> that has the data.</param>
		private void HandleCalculatedAverageLTFuelPid(DataRow row, CellData cellData)
		{
			if (this.HaveCalculatedAverageLTFuel)
			{
				double? oneToOneLtFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mOneToOneLTFuelColumn.Value].DataType.Name, row, this.mOneToOneLTFuelColumn.Value);
				double? twoToOneLtFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mTwoToOneLTFuelColumn.Value].DataType.Name, row, this.mTwoToOneLTFuelColumn.Value);
				if (oneToOneLtFuel.HasValue && twoToOneLtFuel.HasValue)
				{
					cellData.AddDataSample(-3, Math.Round((double)((oneToOneLtFuel.Value + twoToOneLtFuel.Value) / 2), 2));
				}
			}
		}

		/// <summary>
		/// Calculate the average Short Term Fuel PID.
		/// </summary>
		/// <param name="row">The <see cref="DataRow"/> to do the calculation on.</param>
		/// <param name="cellData">The <see cref="CellData"/> that has the data.</param>
		private void HandleCalculatedAverageSTFuelPid(DataRow row, CellData cellData)
		{
			if (this.HaveCalculatedAverageSTFuel)
			{
				double? oneToOneStFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mOneToOneSTFuelColumn.Value].DataType.Name, row, this.mOneToOneSTFuelColumn.Value);
				double? twoToOneStFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mTwoToOneSTFuelColumn.Value].DataType.Name, row, this.mTwoToOneSTFuelColumn.Value);
				if (oneToOneStFuel.HasValue && twoToOneStFuel.HasValue)
				{
					cellData.AddDataSample(-4, Math.Round((double)((oneToOneStFuel.Value + twoToOneStFuel.Value) / 2), 2));
				}
			}
		}

		/// <summary>
		/// Calculate the Calculated Boost PID.
		/// </summary>
		/// <param name="row">The <see cref="DataRow"/> to do the calculation on.</param>
		/// <param name="cellData">The <see cref="CellData"/> that has the data.</param>
		private void HandleCalculatedBoostPid(DataRow row, CellData cellData)
		{
			if (this.HaveCalculatedBoost)
			{
				double? baroValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mBaroColumn.Value].DataType.Name, row, this.mBaroColumn.Value);
				double? mapValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mAvgMapToUseColumn.Value].DataType.Name, row, this.mAvgMapToUseColumn.Value);
				if (baroValue.HasValue && mapValue.HasValue)
				{
					cellData.AddDataSample(-2, Math.Round((double)(mapValue.Value - baroValue.Value), 2));
				}
			}
		}

		/// <summary>
		/// Identify all the necessary and special columns.
		/// </summary>
		/// <param name="pidUnits">An array of units for each PID.</param>
		private void IdentifyColumnsAndAddPids(string[] pidUnits)
		{
			foreach (DataColumn column in this.mOriginalDataSet.Columns)
			{
				switch (column.ColumnName.ToUpper(CultureInfo.CurrentCulture))
				{
					case "RPM":
					case "ENGINE SPEED":
						this.mRpmColumn = new int?(column.Ordinal);
						break;

					case "PRATIO":
					case "P-RATIO MAP/BARO":
						this.mPRatioColumn = new int?(column.Ordinal);
						break;

					case "AVG MAP TO USE":
						this.mAvgMapToUseColumn = new int?(column.Ordinal);
						break;

					case "BARO":
						this.mBaroColumn = new int?(column.Ordinal);
						break;

					case "ECT":
					case "ENGINE COOLANT TEMP":
                    case "ENGINE COOLANT TEMPERATURE":
                        this.mEctColumn = new int?(column.Ordinal);
						break;

					case "AVG TOTAL WORKING PW":
						this.mInjectorPulseWidthColum = new int?(column.Ordinal);
						PidInformation injectorDutyCyclePid = new PidInformation
						{
							Name = "Injector Duty Cycle (calculated)",
							ColumnNumber = -1,
							IsSelected = false,
							Unit = "%"
						};
						this.mPids.Add(injectorDutyCyclePid);
						break;

					case "1/1 LONG TERM ADAP":
						this.mOneToOneLTFuelColumn = new int?(column.Ordinal);
						break;

					case "2/1 LONG TERM ADAP":
						this.mTwoToOneLTFuelColumn = new int?(column.Ordinal);
						break;

					case "1/1 SHORT TERM ADAP":
						this.mOneToOneSTFuelColumn = new int?(column.Ordinal);
						break;

					case "2/1 SHORT TERM ADAP":
						this.mTwoToOneSTFuelColumn = new int?(column.Ordinal);
						break;
				}

				if ((!string.IsNullOrEmpty(column.ColumnName) && !column.ColumnName.Contains("NoName") && !column.ColumnName.ToLower(CultureInfo.CurrentCulture).StartsWith("column", StringComparison.OrdinalIgnoreCase)) && string.Compare(column.ColumnName, "Time", StringComparison.OrdinalIgnoreCase) != 0)
				{
					PidInformation pidInfo = new PidInformation
					{
						Name = column.ColumnName,
						ColumnNumber = column.Ordinal,
						IsSelected = false,
						Unit = (pidUnits != null) ? pidUnits[column.Ordinal] : "string"
					};

					this.mPids.Add(pidInfo);
				}
			}
		}

		/// <summary>
		/// Attempt to import the specified CSV file.
		/// </summary>
		/// <param name="fileName">The name of the file to load.</param>
		/// <param name="userAcceptedLargeFileWarning">A flag indicating whether or not the user has accepted the large file warning.</param>
		/// <returns>A <see cref="Status"/>.</returns>
		private Status ImportCsv(string fileName, bool userAcceptedLargeFileWarning)
		{
			this.mOriginalDataSet = null;
			this.mOriginalDataSet = new DataTable();
			this.mOriginalDataSet.Locale = CultureInfo.CurrentCulture;
			return Utility.ReadCsv(this.mOriginalDataSet, fileName, userAcceptedLargeFileWarning);
		}

		/// <summary>
		/// Load a scale from a file.
		/// </summary>
		/// <returns>A <see cref="Status"/>.</returns>
		private Status LoadScaling()
		{
			if (!this.ScaleFromDataGrid)
			{
				if (!this.UserDefinedRpmScale)
				{
					this.mRpmScale = new List<ScalingInformation>(17);
					if (this.AutoScaleRpm)
					{
						double maxValue = double.MaxValue;
						double minValue = double.MinValue;
						foreach (DataRow row in this.mOriginalDataSet.Rows)
						{
							double? rowValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mRpmColumn.Value].DataType.Name, row, this.mRpmColumn.Value);
							if (rowValue.HasValue && (rowValue.Value > 0))
							{
								maxValue = Math.Min(maxValue, rowValue.Value);
								minValue = Math.Max(minValue, rowValue.Value);
							}
						}

						this.mRpmScale = Utility.CreateScaleFromUpperAndLowerBounds(true, maxValue, minValue, 1);
					}
					else if (Settings.Default.AutomaticallyLoadRpmScale)
					{
						this.mRpmScale = Utility.LoadScaleFromFile(true, string.Empty, Settings.Default.RpmScaleFile);
						if (this.mRpmScale == null)
						{
							this.mRpmScale = Utility.SetDefaults(true);
							return Status.DataError;
						}
					}
					else
					{
						this.mRpmScale = Utility.SetDefaults(true);
					}
				}

				if (!this.UserDefinedYAxisScale)
				{
					this.mYAxisScale = new List<ScalingInformation>(17);
					if (this.AutoScaleYAxis)
					{
						double minValue = double.MaxValue;
						double maxValue = double.MinValue;
						if (this.mPRatioColumn.HasValue && !this.UseCustomYAxis)
						{
							foreach (DataRow row in this.mOriginalDataSet.Rows)
							{
								double? rowValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mPRatioColumn.Value].DataType.Name, row, this.mPRatioColumn.Value);
								if (rowValue.HasValue && (!this.UseOnlyBoostPRatios || (this.UseOnlyBoostPRatios && (rowValue.Value > 1))))
								{
									minValue = Math.Min(minValue, rowValue.Value);
									maxValue = Math.Max(maxValue, rowValue.Value);
								}
							}
						}
						else if ((this.mBaroColumn.HasValue && this.mAvgMapToUseColumn.HasValue) && !this.UseCustomYAxis)
						{
							foreach (DataRow row3 in this.mOriginalDataSet.Rows)
							{
								double? baroValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mBaroColumn.Value].DataType.Name, row3, this.mBaroColumn.Value);
								double? mapValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mAvgMapToUseColumn.Value].DataType.Name, row3, this.mAvgMapToUseColumn.Value);
								if (baroValue.HasValue && mapValue.HasValue)
								{
									double pressureRatioValue = mapValue.Value / baroValue.Value;
									if (!this.UseOnlyBoostPRatios || (this.UseOnlyBoostPRatios && (pressureRatioValue > 1)))
									{
										minValue = Math.Min(minValue, pressureRatioValue);
										maxValue = Math.Max(maxValue, pressureRatioValue);
									}
								}
							}
						}
						else
						{
							foreach (DataRow row in this.mOriginalDataSet.Rows)
							{
								double? pidValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mUserDefinedYAxisColumn.Value].DataType.Name, row, this.mUserDefinedYAxisColumn.Value);
								if (pidValue.HasValue)
								{
									minValue = Math.Min(minValue, pidValue.Value);
									maxValue = Math.Max(maxValue, pidValue.Value);
								}
							}
						}

						this.mYAxisScale = Utility.CreateScaleFromUpperAndLowerBounds(false, minValue, maxValue, 3);
					}
					else if (Settings.Default.AutomaticallyLoadYAxisScale)
					{
						this.mYAxisScale = Utility.LoadScaleFromFile(false, this.YAxisPid, Settings.Default.YAxisScaleFile);
						if (this.mYAxisScale == null)
						{
							this.mYAxisScale = Utility.SetDefaults(false);
							return Status.DataError;
						}
					}
					else
					{
						this.mYAxisScale = Utility.SetDefaults(false);
					}
				}
			}

			return Status.Success;
		}

		/// <summary>
		/// Update the text of the progress dialog.
		/// </summary>
		/// <param name="progressText">The text to display in the dialog.</param>
		private void UpdateProgressText(string progressText)
		{
			this.mProgressDialog.SetProgressText(progressText);
		}

		/// <summary>
		/// Process the data in the CSV file and put it into the data matrix.
		/// </summary>
		/// <returns>A <see cref="Status"/> telling the result of the process.</returns>
		private Status ProcessData()
		{
			this.mProcessedData = Utility.CreateEmtpyMatrix(this.mRpmScale.Count, this.mYAxisScale.Count);
			foreach (DataRow row in this.mOriginalDataSet.Rows)
			{
				double? rpmValue = null;
				try
				{
					rpmValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mRpmColumn.Value].DataType.Name, row, this.mRpmColumn.Value);
				}
				catch (IndexOutOfRangeException)
				{
					MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return Status.DataError;
				}
				catch (NullReferenceException)
				{
				}

				double? yAxisValue = null;
				if (this.mPRatioColumn.HasValue && !this.UseCustomYAxis)
				{
					try
					{
						yAxisValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mPRatioColumn.Value].DataType.Name, row, this.mPRatioColumn.Value);
					}
					catch (IndexOutOfRangeException)
					{
						MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return Status.DataError;
					}
					catch (NullReferenceException)
					{
					}
				}
				else if ((this.mBaroColumn.HasValue && this.mAvgMapToUseColumn.HasValue) && !this.UseCustomYAxis)
				{
					double? baroValue = null;
					double? averageMapToUseValue = null;
					try
					{
						baroValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mBaroColumn.Value].DataType.Name, row, this.mBaroColumn.Value);
						averageMapToUseValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mAvgMapToUseColumn.Value].DataType.Name, row, this.mAvgMapToUseColumn.Value);
					}
					catch (IndexOutOfRangeException)
					{
						MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return Status.DataError;
					}
					catch (NullReferenceException)
					{
					}

					if (baroValue.HasValue && averageMapToUseValue.HasValue)
					{
						yAxisValue = new double?(averageMapToUseValue.Value / baroValue.Value);
					}
				}
				else
				{
					try
					{
						yAxisValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mUserDefinedYAxisColumn.Value].DataType.Name, row, this.mUserDefinedYAxisColumn.Value);
					}
					catch (IndexOutOfRangeException)
					{
						MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return Status.DataError;
					}
					catch (NullReferenceException)
					{
					}
				}

				if (rpmValue.HasValue && yAxisValue.HasValue)
				{
					if (this.OnlyShowEctsBetweenRange)
					{
						double? ectValue = null;
						try
						{
							ectValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mEctColumn.Value].DataType.Name, row, this.mEctColumn.Value);
						}
						catch (IndexOutOfRangeException)
						{
							MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
							return Status.DataError;
						}
						catch (NullReferenceException)
						{
						}

						if ((!ectValue.HasValue || (ectValue.Value < this.EctRangeLowValue)) || ectValue.Value > this.EctRangeHighValue)
						{
							continue;
						}
					}

					if (yAxisValue.Value >= this.mYAxisScale[0].MinValue && yAxisValue.Value <= this.mYAxisScale[16].MaxValue && rpmValue.Value >= this.mRpmScale[0].MinValue && rpmValue.Value <= this.mRpmScale[16].MaxValue)
					{
						int yAxisBucket = 0;
						for (int i = 0; i < this.mYAxisScale.Count; i++)
						{
							if (yAxisValue.Value >= this.mYAxisScale[i].MinValue && yAxisValue.Value <= this.mYAxisScale[i].MaxValue)
							{
								yAxisBucket = i;
								break;
							}
						}

						int rpmBucket = 0;
						for (int j = 0; j < this.mRpmScale.Count; j++)
						{
							if (rpmValue.Value >= this.mRpmScale[j].MinValue && rpmValue.Value <= this.mRpmScale[j].MaxValue)
							{
								rpmBucket = j;
								break;
							}
						}

						CellData cellData = this.mProcessedData[rpmBucket, yAxisBucket];
						cellData.IncrementSampleSize();
						for (int k = 1; k < row.ItemArray.Length; k++)
						{
							double? pidValue = null;
							try
							{
								pidValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[k].DataType.Name, row, k);
							}
							catch (IndexOutOfRangeException)
							{
								MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
								return Status.DataError;
							}
							catch (NullReferenceException)
							{
							}

							if (pidValue.HasValue)
							{
								cellData.AddDataSample(k, pidValue.Value);
							}
						}

						this.HandleCalculatedBoostPid(row, cellData);
						this.HandleCalculatedAverageLTFuelPid(row, cellData);
						this.HandleCalculatedAverageSTFuelPid(row, cellData);
					}
				}
			}

			return Status.Success;
		}

		/// <summary>
		/// Delegate to call the PopulateGrid method.
		/// </summary>
		private void PopulateGridHelper()
		{
			this.PopuplateGrid();
		}

		/// <summary>
		/// Populate the <see cref="DataGrid"/> from the newly processed data.
		/// </summary>
		private void PopuplateGrid()
		{
			List<string> selectedPids = this.mPids.Where(p => p.IsSelected == true).OrderBy(p => p.Name).Select(p => p.Name).ToList();
			CmrHistoDataCollection dataCollection = new CmrHistoDataCollection();
			for (int i = 0; i < this.mYAxisScale.Count; i++)
			{
				List<CellDisplayData> displayDatas = new List<CellDisplayData>(17);
				for (int k = 0; k < this.mRpmScale.Count; k++)
				{
					if (this.mProcessedData[k, i].SampleSize == 0)
					{
						CellDisplayData cellDisplayData = new CellDisplayData
						{
							DisplayText = string.Empty,
							YAxisValue = this.mYAxisScale[i].Value.ToString(CultureInfo.CurrentCulture),
							YAxisToolTip = string.Concat(new object[] { this.mYAxisScale[i].MinValue.ToString(CultureInfo.CurrentCulture), " - ", this.mYAxisScale[i].MaxValue.ToString(CultureInfo.CurrentCulture), ' ', this.YAxisPid }),
							Color = "-1"
						};
						displayDatas.Add(cellDisplayData);
					}
					else
					{
						StringBuilder dataString = new StringBuilder();
						string stringValue = "0";
						for (int m = 0; m < this.mPids.Count; m++)
						{
							if (!this.mPids[m].IsSelected)
							{
								continue;
							}

							List<double> pidValues = this.mProcessedData[k, i].GetPidValues(this.mPids[m].ColumnNumber);
							if (pidValues != null && pidValues.Count >= 1)
							{
								double maxValue = double.MaxValue;
								double minValue = double.MinValue;
								double pidValue = 0;
								int dataSize = 0;
								if (this.mPids[m].ColumnNumber != -1)
								{
									for (int n = 0; n < pidValues.Count; n++)
									{
										maxValue = Math.Min(pidValues[n], maxValue);
										minValue = Math.Max(pidValues[n], minValue);
										pidValue += pidValues[n];
										dataSize++;
									}
								}
								else
								{
									pidValues = this.mProcessedData[k, i].GetPidValues(this.mInjectorPulseWidthColum.Value);
									List<double> pidData = this.mProcessedData[k, i].GetPidValues(this.mRpmColumn.Value);
									for (int dataIndexer = 0; dataIndexer < pidValues.Count; dataIndexer++)
									{
										double injectorDutyCycle = Math.Round((double)((pidValues[dataIndexer] * pidData[dataIndexer]) / 1200000), 1);
										maxValue = Math.Min(injectorDutyCycle, maxValue);
										minValue = Math.Max(injectorDutyCycle, minValue);
										pidValue += injectorDutyCycle;
										dataSize++;
									}
								}

								double averageValue = Math.Round((double)(pidValue / ((double)dataSize)), 2);
								string unit = this.mPids[m].Unit;
								string minValueUnit = this.mPids[m].Unit;
								string maxValueUnit = this.mPids[m].Unit;
								double? boostAverageValue = null;
								if (this.mPids[m].ColumnNumber == -2)
								{
									if (maxValue < 0)
									{
										maxValue = Math.Round((double)((maxValue * 2.036) * -1.0), 2);
										unit = "inHg";
									}

									if (minValue < 0)
									{
										minValue = Math.Round((double)((minValue * 2.036) * -1.0), 2);
										minValueUnit = "inHg";
									}

									if (averageValue < 0)
									{
										boostAverageValue = new double?(averageValue);
										averageValue = Math.Round((double)((averageValue * 2.036) * -1.0), 2);
										maxValueUnit = "inHg";
									}
								}

								if (dataString.Length > 0)
								{
									dataString.Append("\n");
								}

								if (this.ShowPidLabels)
								{
									if (this.ShowCustomPidLabels && !string.IsNullOrEmpty(this.mPids[m].CustomName))
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, "{0}\n", new object[] { this.mPids[m].CustomName });
									}
									else
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, "{0}\n", new object[] { this.mPids[m].Name });
									}
								}

								if (this.ShowMinAndMax)
								{
									dataString.AppendFormat(CultureInfo.CurrentCulture, "{0}{1} | {2}{3}", new object[] { maxValue, unit, minValue, minValueUnit });
								}

								if (this.ShowAverage)
								{
									if (this.ShowMinAndMax)
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, " | {0}{1}", new object[] { averageValue, maxValueUnit });
									}
									else
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", new object[] { averageValue, maxValueUnit });
									}
								}

								if (this.ShowLastValue)
								{
									if (this.ShowMinAndMax || this.ShowAverage)
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, " | {0}{1}", new object[] { this.mProcessedData[k, i].GetLastValue(this.mPids[m].ColumnNumber), maxValueUnit });
									}
									else
									{
										dataString.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", new object[] { this.mProcessedData[k, i].GetLastValue(this.mPids[m].ColumnNumber), maxValueUnit });
									}
								}

								if (boostAverageValue.HasValue)
								{
									averageValue = boostAverageValue.Value;
								}

								if (this.PidToHighlight.HasValue && (this.PidToHighlight.Value == this.mPids[m].ColumnNumber))
								{
									double lastValue = 0;
									switch (this.HighlightParameter)
									{
										case DataType.Minimum:
											lastValue = maxValue;
											break;

										case DataType.Maximum:
											lastValue = minValue;
											break;

										case DataType.Average:
											lastValue = averageValue;
											break;

										case DataType.Last:
											lastValue = this.mProcessedData[k, i].GetLastValue(this.mPids[m].ColumnNumber);
											break;
									}

									if (this.HighlightValueToChange.HasValue)
									{
										if (lastValue > this.HighlightValueToChange)
										{
											stringValue = this.ShowAboveAsRed ? "2" : "1";
										}
										else if (lastValue == this.HighlightValueToChange)
										{
											stringValue = "3";
										}
										else
										{
											stringValue = this.ShowAboveAsRed ? "1" : "2";
										}
									}
									else
									{
										if (lastValue >= this.HighlightRangeMin && lastValue <= this.HighlightRangeMax)
										{
											stringValue = this.ShowInsideAsRed ? "2" : "1";
										}
										else
										{
											stringValue = this.ShowInsideAsRed ? "1" : "2";
										}
									}
								}

								this.mProcessedData[k, i].SetAverageValue(this.mPids[m].ColumnNumber, averageValue);
								this.mProcessedData[k, i].SetMinimumValue(this.mPids[m].ColumnNumber, maxValue);
								this.mProcessedData[k, i].SetMaximumValue(this.mPids[m].ColumnNumber, minValue);
							}
						}

						if (this.ShowSampleSize)
						{
							dataString.AppendFormat("\nSampleSize: {0}", this.mProcessedData[k, i].SampleSize);
						}

						CellDisplayData newDisplayData = new CellDisplayData
						{
							DisplayText = dataString.ToString(),
							Color = stringValue,
							Pids = selectedPids,
							YAxisValue = this.mYAxisScale[i].Value.ToString(CultureInfo.CurrentCulture),
							YAxisToolTip = string.Concat(new object[] { this.mYAxisScale[i].MinValue.ToString(CultureInfo.CurrentCulture), " - ", this.mYAxisScale[i].MaxValue.ToString(CultureInfo.CurrentCulture), ' ', this.YAxisPid }),
						};
						displayDatas.Add(newDisplayData);
					}
				}

				dataCollection.AddDataRow(displayDatas);
			}

			if (this.YAxisSort == Sort.Descending)
			{
				dataCollection = Utility.ReverseData(dataCollection);
			}

			this.mDataGrid.ItemsSource = dataCollection;
			this.mDataGrid.HeadersVisibility = DataGridHeadersVisibility.All;
			this.mDataGrid.RowHeaderWidth = 50;
			for (int j = 0; j < this.mRpmScale.Count; j++)
			{
				HeaderInformation headerInfo = new HeaderInformation
				{
					Text = this.mRpmScale[j].Value.ToString(CultureInfo.CurrentCulture),
					ToolTip = this.mRpmScale[j].MinValue.ToString(CultureInfo.CurrentCulture) + " rpm - " + this.mRpmScale[j].MaxValue.ToString(CultureInfo.CurrentCulture) + " rpm"
				};
				this.mDataGrid.Columns[j].Header = headerInfo;
				this.mDataGrid.Columns[j].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
			}

			this.mSaveToHtmlMenuItem.IsEnabled = true;
		}

		/// <summary>
		/// Gets the name of a PID and the PID's units.
		/// </summary>
		/// <param name="pidColumn">The PID's column number.</param>
		/// <returns>A string of this format: Pid Name (Units).</returns>
		private string GetPidNameAndUnit(int pidColumn)
		{
			PidInformation info = this.mPids.Where(x => x.ColumnNumber == pidColumn).FirstOrDefault();

			return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", !string.IsNullOrEmpty(info.CustomName) ? info.CustomName : info.Name, info.Unit);
		}

		/// <summary>
		/// Gets the unit for the supplied PID.
		/// </summary>
		/// <param name="pidColumn">The PID's column number.</param>
		/// <returns>The unit for the supplied PID.</returns>
		private string GetPidUnit(int pidColumn)
		{
			return this.mPids.Where(x => x.ColumnNumber == pidColumn).FirstOrDefault().Unit;
		}
		#endregion

		#region Internal Properties
		/// <summary>
		/// Gets or sets the <see cref="DataType"/> for highlighting.
		/// </summary>
		/// <value>The value to highlight on.</value>
		internal DataType HighlightParameter { get; set; }

		/// <summary>
		/// Gets or sets the value at which highlighting should change from red to yellow.
		/// </summary>
		/// <value>The highlighting thresh hold.</value>
		internal double? HighlightValueToChange { get; set; }

		/// <summary>
		/// Gets or sets the lower value at which highlighting should change from red to yellow.
		/// </summary>
		/// <value>The lower bound for highlighting thresh hold</value>
		internal double? HighlightRangeMin { get; set; }

		/// <summary>
		/// Gets or sets the upper value at which highlighting should change from red to yellow.
		/// </summary>
		/// <value>The upper bound for highlighting thresh hold</value>
		internal double? HighlightRangeMax { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not values above the thresh hold should be shown as red.
		/// </summary>
		/// <value><c>True</c> if they should be shown as red, <c>False</c> otherwise.</value>
		internal bool ShowAboveAsRed { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not values outside the thresh hold should be shown as red.
		/// </summary>
		/// <value><c>True</c> if they should be shown as red, <c>False</c> otherwise.</value>
		internal bool ShowInsideAsRed { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the average value should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if it should be shown, <c>False</c> otherwise.</value>
		internal bool ShowAverage { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not custom PID labels should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if they should be shown, <c>False</c> otherwise.</value>
		internal bool ShowCustomPidLabels { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the last value should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if it should be shown, <c>False</c> otherwise.</value>
		internal bool ShowLastValue { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the min and max values should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if they should be shown, <c>False</c> otherwise.</value>
		internal bool ShowMinAndMax { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the PID labels should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if they should be shown, <c>False</c> otherwise.</value>
		internal bool ShowPidLabels { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the data sample size should be shown in the cell.
		/// </summary>
		/// <value><c>True</c> if it should be shown, <c>False</c> otherwise.</value>
		internal bool ShowSampleSize { get; set; }
		#endregion

		#region Internal Methods
		/// <summary>
		/// Attempt to open and process the specified CSV file.
		/// </summary>
		/// <param name="fileName">The file to open.</param>
		internal void OpenAndProcessCsv(string fileName)
		{
			DoWorkEventHandler workerHandler = null;
			RunWorkerCompletedEventHandler completedHandler = null;
			this.mProgressDialog = new ProgressDialog();
			this.mProgressDialog.Owner = this;
			Dispatcher progressDialogDispatcher = this.mProgressDialog.Dispatcher;
			this.mWorker = new BackgroundWorker();
			this.mWorker.WorkerSupportsCancellation = true;
			Status status = Status.None;

			if (!string.IsNullOrEmpty(fileName))
			{
				if (workerHandler == null)
				{
					workerHandler = delegate(object s, DoWorkEventArgs args)
					{
						UpdateProgressWorker updateProgressWorker = new UpdateProgressWorker(this.UpdateProgressText);
						status = this.ImportCsv(fileName, false);
						if (status != Status.Success && status != Status.LargeFile)
						{
							this.CancelProcess(s, args);
						}
						else
						{
							if (status == Status.LargeFile)
							{
								progressDialogDispatcher.BeginInvoke(updateProgressWorker, new object[] { "Large file detected." });
								if (MessageBox.Show("The file you selected is pretty big. It may take a while to process. Would you like to continue?", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.No)
								{
									this.CancelProcess(s, args);
									return;
								}

								progressDialogDispatcher.BeginInvoke(updateProgressWorker, new object[] { "Please wait while the selected file is opened..." });
								status = this.ImportCsv(fileName, true);
								if (status != Status.Success && status != Status.LargeFile)
								{
									this.CancelProcess(s, args);
									return;
								}
							}

							progressDialogDispatcher.BeginInvoke(updateProgressWorker, new object[] { "File opened, validating data..." });

							status = this.FindColumns(fileName);
							if (status != Status.Success && status == Status.RpmNotFound)
							{
								this.CancelProcess(s, args);
							}
						}
					};
				}

				this.mWorker.DoWork += workerHandler;
				if (completedHandler == null)
				{
					completedHandler = delegate(object s, RunWorkerCompletedEventArgs args)
					{
						if (status == Status.Success || status == Status.RatioNotFound)
						{
                            bool requireCustomAxis = !this.mPRatioColumn.HasValue && (!this.mBaroColumn.HasValue || !this.mAvgMapToUseColumn.HasValue);
							PidSelectionDialog dialog = new PidSelectionDialog(this.mPids, this.mPRatioColumn.HasValue, this.mEctColumn.HasValue, requireCustomAxis)
							{
								Owner = this
							};
							bool? nullable = dialog.ShowDialog();
							if (nullable.HasValue && !nullable.Value)
							{
								this.mProgressDialog.Close();
								status = Status.Canceled;
							}
							else
							{
								this.AutoScaleYAxis = dialog.AutoScaleYAxis;
								this.UseOnlyBoostPRatios = dialog.UseOnlyBoostPRatios;
								this.AutoScaleRpm = dialog.AutoScaleRpm;
								this.OnlyShowEctsBetweenRange = dialog.OnlyShowEctsBetweenRange;
								if (this.OnlyShowEctsBetweenRange)
								{
									this.EctRangeLowValue = dialog.EctRangeLowValue;
									this.EctRangeHighValue = dialog.EctRangeHighValue;
								}

								this.mDataMenuItem.IsEnabled = true;
								this.mYAxisScalingMenuItem.Header = this.YAxisPid + " Scaling";
								this.mYAxisScaleToolTip.Content = "Define " + this.YAxisPid + " newScale points.";
								this.mYAxisAscendingSortMenuItem.Header = this.YAxisPid + " _Ascending";
								this.mYAxisDescendingSortMenuItem.Header = this.YAxisPid + " _Descending";
								this.mSortMenuItem.IsEnabled = true;
								this.mProgressDialog.Close();
							}
						}
						else
						{
							this.mProgressDialog.Close();
						}
					};
				}

				this.mWorker.RunWorkerCompleted += completedHandler;
				this.mWorker.RunWorkerAsync();
				this.mProgressDialog.ShowDialog();
				if (status != Status.Success)
				{
					return;
				}

				this.Title = "CmrHisto - " + fileName;
			}

			this.mProgressDialog = new ProgressDialog();
			this.mProgressDialog.Owner = this;
			progressDialogDispatcher = this.mProgressDialog.Dispatcher;
			this.mWorker = new BackgroundWorker();
			this.mWorker.WorkerSupportsCancellation = true;
			this.mWorker.DoWork += delegate(object s, DoWorkEventArgs args)
			{
				UpdateProgressWorker updateProgressWorker = new UpdateProgressWorker(this.UpdateProgressText);
				progressDialogDispatcher.BeginInvoke(updateProgressWorker, new object[] { "Please wait while the data is processed..." });
				status = this.LoadScaling();
				if (status != Status.Success)
				{
					MessageBox.Show("There was an error loading one of the scales. Please make the scale file(s) exist, are valid and are the for the same PID as what is being used for the Y axis.\n\nThe default scale for that axis has been loaded.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}

				status = this.ProcessData();
				if (status != Status.Success)
				{
					MessageBox.Show("There was an error processing the data. Make sure the file is a valid csv file exported from DataViewer.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}
				else
				{
					PopulateGridWorker worker2 = new PopulateGridWorker(this.PopulateGridHelper);
					progressDialogDispatcher.BeginInvoke(worker2, new object[0]);
				}
			};
			this.mWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
			{
				this.mProgressDialog.Close();
			};
			this.mWorker.RunWorkerAsync();
			this.mProgressDialog.ShowDialog();
		}

		/// <summary>
		/// Sets the PID to copy and save the data to the clipboard or specified file.
		/// </summary>
		/// <param name="pidName">The name of the PID to save data for.</param>
		/// <param name="copyValueType">The <see cref="DataType"/> of data.</param>
		/// <param name="forClipboard">A flag indicating whether or not the data will be saved to the clipboard, if false it will be saved to a file.</param>
		/// <param name="fileName">The name of the file to create if saving to file.</param>
		internal void SetCopyPid(string pidName, DataType copyValueType, bool forClipboard, string fileName = "")
		{
			int? pidColumFromName = this.GetPidColumFromName(pidName);
			if (!pidColumFromName.HasValue)
			{
				MessageBox.Show("The selected PID was not found!", CmrHisto.Properties.Resources.MessageBoxPidNotFoundError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else if (forClipboard)
			{
				Clipboard.SetText(this.BuildCsvData(copyValueType, pidColumFromName.Value, true));
			}
			else
			{
				if (!string.IsNullOrEmpty(fileName))
				{
					using (StreamWriter writer = new StreamWriter(fileName))
					{
						try
						{
							writer.Write(this.BuildCsvData(copyValueType, pidColumFromName.Value, false));
						}
						catch (Exception)
						{
							MessageBox.Show("There was an error writing the file. Please try again.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the PID to highlight.
		/// </summary>
		/// <param name="pidName">The name of the PID to highlight.</param>
		internal void SetHighlightPid(string pidName)
		{
			int? pidColumFromName = this.GetPidColumFromName(pidName);
			this.PidToHighlight = pidColumFromName;
		}

		/// <summary>
		/// Update the scale.
		/// </summary>
		/// <param name="scale">The new scale.</param>
		/// <param name="isRpm">A flag indicating whether the scale is for the RPM scale or Y-Axis.</param>
		internal void SetScale(List<ScalingInformation> scale, bool isRpm)
		{
			if (isRpm)
			{
				this.mRpmScale = scale;
				this.UserDefinedRpmScale = true;
			}
			else
			{
				this.mYAxisScale = scale;
				this.UserDefinedYAxisScale = true;
			}
		}

		/// <summary>
		/// Define pidInfo for the Y-Axis PID.
		/// </summary>
		/// <param name="isUserDefined">A flag indicating whether or not the Y-Axis is user defined or not.</param>
		/// <param name="pidName">The name of the PID to use for the Y-Axis.</param>
		internal void SetupYAxisPid(bool isUserDefined, string pidName)
		{
			this.UseCustomYAxis = isUserDefined;
			this.YAxisPid = pidName;
			this.mUserDefinedYAxisColumn = this.GetPidColumFromName(this.YAxisPid);
		}

		/// <summary>
		/// Save the custom PIDs and re-process the current data.
		/// </summary>
		internal void UpdateCustomPids()
		{
			if (this.mPids.Count > 0)
			{
				XmlDocument pidDocument = new XmlDocument();
				pidDocument.Load("CustomPidNames.xml");
				XmlNodeList pidList = pidDocument.SelectSingleNode("/pids").ChildNodes;
				foreach (XmlNode pid in pidList)
				{
					if (pid.NodeType == XmlNodeType.Element && pid.Attributes != null)
					{
						string currentPidName = pid.Attributes[0].Value;
						PidInformation pidInfo = this.mPids.Find(p => string.Compare(p.Name, currentPidName, StringComparison.OrdinalIgnoreCase) == 0);
						if (pidInfo != null)
						{
							pidInfo.CustomName = pid.Attributes[1].Value;
						}
					}
				}

				this.OpenAndProcessCsv(string.Empty);
			}
		}

		/// <summary>
		/// Update the menu items based on current selections.
		/// </summary>
		internal void UpdateShowMenuItems()
		{
			this.mShowPidLabelsMenuItem.IsChecked = this.ShowPidLabels;
			this.mShowMinAndMaxMenuItem.IsChecked = this.ShowMinAndMax;
			this.mShowAverageMenuItem.IsChecked = this.ShowAverage;
			this.mShowLastMenuItem.IsChecked = this.ShowLastValue;
			this.mShowSampleSizeMenuItem.IsChecked = this.ShowSampleSize;
			this.mShowCustomPidLabelsMenuItem.IsEnabled = this.ShowPidLabels;
			this.mShowCustomPidLabelsMenuItem.IsChecked = this.ShowCustomPidLabels;
		}

		/// <summary>
		/// Find the column number for the supplied PID.
		/// </summary>
		/// <param name="pidName">The name of the PID to look for.</param>
		/// <returns>The column corresponding to the PID, or null if the name is not found.</returns>
		internal int? GetPidColumFromName(string pidName)
		{
			foreach (PidInformation information in this.mPids)
			{
				if (string.Compare(pidName, information.Name, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return new int?(information.ColumnNumber);
				}
			}

			return null;
		}

		/// <summary>
		/// Gets all of the data points for the given pid and returns the data in a <see cref="MapData"/> object.
		/// </summary>
		/// <param name="selectedPidName">The name of the selected PID.</param>
		/// <param name="type">The <see cref="DataType"/> to use for data.</param>
		/// <returns>A <see cref="MapData"/> object with all of the data necessary to build the surface plot.</returns>
		internal Task<MapData> GetSelectedPidDataAsync(string selectedPidName, DataType type)
		{
            
            return Task.Run(() =>
            {
                int selectedPidId = this.GetPidColumFromName(selectedPidName).Value;
                List<Triangulator.Point> data = new List<Triangulator.Point>();
                Dictionary<string, double> valueData = new Dictionary<string, double>();
                Dictionary<string, List<double>> averageData = new Dictionary<string, List<double>>();
                SortedSet<double> xAxisValues = new SortedSet<double>();
                SortedSet<double> yAxisValues = new SortedSet<double>();
                double lowestXValue = double.MaxValue;
                double highestXValue = double.MinValue;
                double lowestZValue = double.MaxValue;
                double highestZValue = double.MinValue;
                double lowestValue = double.MaxValue;
                double highestValue = double.MinValue;

                double? rpmValue = null;
                double? yAxisValue = null;

                string yAxisPidName = string.Empty;
                string baroColumnName = string.Empty;
                string averageMapToUseColumnName = string.Empty;
                if (this.mPRatioColumn.HasValue && !this.UseCustomYAxis)
                {
                    yAxisPidName = this.mOriginalDataSet.Columns[this.mPRatioColumn.Value].DataType.Name;
                }
                else if ((this.mBaroColumn.HasValue && this.mAvgMapToUseColumn.HasValue) && !this.UseCustomYAxis)
                {
                    baroColumnName = this.mOriginalDataSet.Columns[this.mBaroColumn.Value].DataType.Name;
                    averageMapToUseColumnName = this.mOriginalDataSet.Columns[this.mAvgMapToUseColumn.Value].DataType.Name;
                }
                else
                {
                    yAxisPidName = this.mOriginalDataSet.Columns[this.mUserDefinedYAxisColumn.Value].DataType.Name;
                }

                Stopwatch stop = new Stopwatch();

                // First thing to do is figure out all of the x and y axis values.
                stop.Start();
                foreach (DataRow row in this.mOriginalDataSet.Rows)
                {
                    rpmValue = null;
                    yAxisValue = null;
                    try
                    {
                        rpmValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mRpmColumn.Value].DataType.Name, row, this.mRpmColumn.Value);
                    }
                    catch (IndexOutOfRangeException outOfRange)
                    {
                        CmrHistoMain.Log.Error("Error getting RPM value", outOfRange);
                        MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                    }
                    catch (NullReferenceException nullReference)
                    {
                        CmrHistoMain.Log.Error("Null reference.", nullReference);
                    }

                    if (this.mPRatioColumn.HasValue && !this.UseCustomYAxis)
                    {
                        try
                        {
                            yAxisValue = Utility.GetValueFromDataRow(yAxisPidName, row, this.mPRatioColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    else if ((this.mBaroColumn.HasValue && this.mAvgMapToUseColumn.HasValue) && !this.UseCustomYAxis)
                    {
                        double? baroValue = null;
                        double? averageMapToUseValue = null;
                        try
                        {
                            baroValue = Utility.GetValueFromDataRow(baroColumnName, row, this.mBaroColumn.Value);
                            averageMapToUseValue = Utility.GetValueFromDataRow(averageMapToUseColumnName, row, this.mAvgMapToUseColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }

                        if (baroValue.HasValue && averageMapToUseValue.HasValue)
                        {
                            yAxisValue = new double?(averageMapToUseValue.Value / baroValue.Value);
                        }
                    }
                    else
                    {
                        try
                        {
                            yAxisValue = Utility.GetValueFromDataRow(yAxisPidName, row, this.mUserDefinedYAxisColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }

                    if (rpmValue.HasValue && yAxisValue.HasValue)
                    {
                        if (this.OnlyShowEctsBetweenRange)
                        {
                            double? ectValue = null;
                            try
                            {
                                ectValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mEctColumn.Value].DataType.Name, row, this.mEctColumn.Value);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                            }
                            catch (NullReferenceException)
                            {
                            }

                            if ((!ectValue.HasValue || (ectValue.Value < this.EctRangeLowValue)) || (ectValue.Value > this.EctRangeHighValue))
                            {
                                continue;
                            }
                        }

                        xAxisValues.Add(rpmValue.Value);
                        yAxisValues.Add(yAxisValue.Value);
                    }
                }
                stop.Stop();
                CmrHistoMain.Log.Info($"Elapsed time for finding min and max: {stop.ElapsedMilliseconds}");
                stop.Reset();

                lowestXValue = xAxisValues.Min;
                highestXValue = xAxisValues.Max;
                lowestZValue = yAxisValues.Min;
                highestZValue = yAxisValues.Max;


                // Loop over the data again, this time getting the value for the selected PID and inserting it into a list at the given coordinate.
                stop.Start();
                foreach (DataRow row in this.mOriginalDataSet.Rows)
                {
                    rpmValue = null;
                    yAxisValue = null;

                    try
                    {
                        rpmValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mRpmColumn.Value].DataType.Name, row, this.mRpmColumn.Value);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                    }
                    catch (NullReferenceException)
                    {
                    }

                    if (this.mPRatioColumn.HasValue && !this.UseCustomYAxis)
                    {
                        try
                        {
                            yAxisValue = Utility.GetValueFromDataRow(yAxisPidName, row, this.mPRatioColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
                    else if ((this.mBaroColumn.HasValue && this.mAvgMapToUseColumn.HasValue) && !this.UseCustomYAxis)
                    {
                        double? baroValue = null;
                        double? averageMapToUseValue = null;
                        try
                        {
                            baroValue = Utility.GetValueFromDataRow(baroColumnName, row, this.mBaroColumn.Value);
                            averageMapToUseValue = Utility.GetValueFromDataRow(averageMapToUseColumnName, row, this.mAvgMapToUseColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }

                        if (baroValue.HasValue && averageMapToUseValue.HasValue)
                        {
                            yAxisValue = new double?(averageMapToUseValue.Value / baroValue.Value);
                        }
                    }
                    else
                    {
                        try
                        {
                            yAxisValue = Utility.GetValueFromDataRow(yAxisPidName, row, this.mUserDefinedYAxisColumn.Value);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }

                    if (rpmValue.HasValue && yAxisValue.HasValue)
                    {
                        if (this.OnlyShowEctsBetweenRange)
                        {
                            double? ectValue = null;
                            try
                            {
                                ectValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mEctColumn.Value].DataType.Name, row, this.mEctColumn.Value);
                            }
                            catch (IndexOutOfRangeException)
                            {
                                MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                            }
                            catch (NullReferenceException)
                            {
                            }

                            if ((!ectValue.HasValue || (ectValue.Value < this.EctRangeLowValue)) || (ectValue.Value > this.EctRangeHighValue))
                            {
                                continue;
                            }
                        }

                        // Get the actual value of the PID.
                        double? pidValue = null;
                        try
                        {
                            if (selectedPidId >= 0)
                            {
                                pidValue = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[selectedPidId].DataType.Name, row, selectedPidId);
                            }
                            else if (selectedPidId == -1)
                            {
                                // Injector Duty Cycle
                                double? pulseWidth = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mInjectorPulseWidthColum.Value].DataType.Name, row, this.mInjectorPulseWidthColum.Value);
                                if (pulseWidth.HasValue)
                                {
                                    pidValue = Math.Round((double)((rpmValue.Value * pulseWidth.Value) / 1200000), 1);
                                }
                            }
                            else if (selectedPidId == -2)
                            {
                                // Boost
                                double? baroValue = Utility.GetValueFromDataRow(baroColumnName, row, this.mBaroColumn.Value);
                                double? mapValue = Utility.GetValueFromDataRow(averageMapToUseColumnName, row, this.mAvgMapToUseColumn.Value);
                                if (baroValue.HasValue && mapValue.HasValue)
                                {
                                    pidValue = Math.Round((double)(mapValue.Value - baroValue.Value), 2);
                                }
                            }
                            else if (selectedPidId == -3)
                            {
                                // LT Fuel ADAP Avg
                                double? oneToOneLtFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mOneToOneLTFuelColumn.Value].DataType.Name, row, this.mOneToOneLTFuelColumn.Value);
                                double? twoToOneLtFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mTwoToOneLTFuelColumn.Value].DataType.Name, row, this.mTwoToOneLTFuelColumn.Value);
                                if (oneToOneLtFuel.HasValue && twoToOneLtFuel.HasValue)
                                {
                                    pidValue = Math.Round((double)((oneToOneLtFuel.Value + twoToOneLtFuel.Value) / 2), 2);
                                }
                            }
                            else if (selectedPidId == -4)
                            {
                                // ST Fuel ADAP Avg
                                double? oneToOneStFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mOneToOneSTFuelColumn.Value].DataType.Name, row, this.mOneToOneSTFuelColumn.Value);
                                double? twoToOneStFuel = Utility.GetValueFromDataRow(this.mOriginalDataSet.Columns[this.mTwoToOneSTFuelColumn.Value].DataType.Name, row, this.mTwoToOneSTFuelColumn.Value);
                                if (oneToOneStFuel.HasValue && twoToOneStFuel.HasValue)
                                {
                                    pidValue = Math.Round((double)((oneToOneStFuel.Value + twoToOneStFuel.Value) / 2), 2);
                                }
                            }
                        }
                        catch (IndexOutOfRangeException)
                        {
                            MessageBox.Show("There was an error processing the data, please make sure the file is valid and try again.", CmrHisto.Properties.Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        catch (NullReferenceException)
                        {
                        }

                        // We have a value, insert it if it matches the criteria.
                        if (pidValue.HasValue)
                        {
                            Triangulator.Point point = new Triangulator.Point(rpmValue.Value, yAxisValue.Value, pidValue.Value);

                            if (type == DataType.Average)
                            {
                                List<double> kvp;
                                if (averageData.TryGetValue(point.ToString(), out kvp))
                                {
                                    kvp.Add(pidValue.Value);
                                }
                                else
                                {
                                    averageData.Add(point.ToString(), new List<double>() { pidValue.Value });
                                }
                            }
                            else if (type == DataType.Last)
                            {
                                double value;
                                if (valueData.TryGetValue(point.ToString(), out value))
                                {
                                    value = pidValue.Value;
                                }
                                else
                                {
                                    valueData.Add(point.ToString(), pidValue.Value);
                                }
                            }
                            else if (type == DataType.Maximum)
                            {
                                double value;
                                if (valueData.TryGetValue(point.ToString(), out value))
                                {
                                    value = Math.Max(pidValue.Value, value);
                                }
                                else
                                {
                                    valueData.Add(point.ToString(), pidValue.Value);
                                }
                            }
                            else if (type == DataType.Minimum)
                            {
                                double value;
                                if (valueData.TryGetValue(point.ToString(), out value))
                                {
                                    value = Math.Min(pidValue.Value, value);
                                }
                                else
                                {
                                    valueData.Add(point.ToString(), pidValue.Value);
                                }
                            }
                        }
                    }
                }
                stop.Stop();
                CmrHistoMain.Log.Info($"Elapsed time getting values: {stop.ElapsedMilliseconds}");
                stop.Reset();

                // For average we need to populate the data now.
                if (type == DataType.Average)
                {
                    stop.Start();
                    double averageValue = 0;
                    char[] split = new char[] { '-' };
                    foreach (KeyValuePair<string, List<double>> kvp in averageData)
                    {
                        averageValue = kvp.Value.Sum();
                        averageValue = Math.Round(averageValue / kvp.Value.Count, 3);

                        string[] values = kvp.Key.Split(split);
                        data.Add(new Triangulator.Point(Convert.ToDouble(values[0]), Convert.ToDouble(values[1]), averageValue));
                        highestValue = Math.Max(averageValue, highestValue);
                        lowestValue = Math.Min(averageValue, lowestValue);
                    }

                    stop.Stop();
                    CmrHistoMain.Log.Info($"Elapsed time for calculating average: {stop.ElapsedMilliseconds}");
                    stop.Reset();
                }
                else
                {
                    char[] split = new char[] { '-' };
                    foreach (KeyValuePair<string, double> kvp in valueData)
                    {
                        string[] values = kvp.Key.Split(split);
                        data.Add(new Triangulator.Point(Convert.ToDouble(values[0]), Convert.ToDouble(values[1]), kvp.Value));
                        highestValue = Math.Max(kvp.Value, highestValue);
                        lowestValue = Math.Min(kvp.Value, lowestValue);
                    }
                }

                // Now that we have all the data we need to scale it.
                stop.Start();
                foreach (Triangulator.Point point in data)
                {
                    point.X = Utility.ConvertRange(lowestXValue, highestXValue, -50, 50, point.X);
                    point.Y = Utility.ConvertRange(lowestZValue, highestZValue, -50, 50, point.Y);
                    point.Value = Utility.ConvertRange(lowestValue, highestValue, -50, 50, point.Value);
                }

                stop.Stop();
                CmrHistoMain.Log.Info($"Elapsed time for scaling: {stop.ElapsedMilliseconds}");
                stop.Reset();

                return new MapData()
                {
                    Data = data,
                    LowestValue = lowestValue,
                    HighestValue = highestValue,
                    LowestXAxisValue = lowestXValue,
                    HighestXAxisValue = highestXValue,
                    LowestZAxisValue = lowestZValue,
                    HighestZAxisValue = highestZValue,
                    XAxisLabel = "RPM",
                    YAxisLabel = this.GetPidNameAndUnit(selectedPidId),
                    ZAxisLabel = this.GetPidNameAndUnit(!this.UseCustomYAxis ? this.mPRatioColumn.Value : this.mUserDefinedYAxisColumn.Value),
                    YAxisPIDUnit = this.GetPidUnit(selectedPidId)
                };
            });
		}
		#endregion

		#region Internal Delegates
		/// <summary>
		/// A delegate that will be used to populate the <see cref="DataGrid"/>.
		/// </summary>
		internal delegate void PopulateGridWorker();

		/// <summary>
		/// A delegate that will be used to update the text of the progress saveDialog.
		/// </summary>
		/// <param name="progressText">The text to display in the progress dialog.</param>
		internal delegate void UpdateProgressWorker(string progressText);
		#endregion

		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the CmrHistoMain class.
		/// </summary>
		public CmrHistoMain()
		{
			CultureInfo info = new CultureInfo(Settings.Default.Language);
			Thread.CurrentThread.CurrentCulture = info;
			Thread.CurrentThread.CurrentUICulture = info;
			this.InitializeComponent();
			this.WindowState = WindowState.Maximized;
			this.mPids = new List<PidInformation>();
			this.mRpmScale = Utility.SetDefaults(true);
			this.mYAxisScale = Utility.SetDefaults(false);
			this.YAxisSort = Sort.Ascending;
			this.mDataGrid.SelectionMode = DataGridSelectionMode.Extended;
			this.mDataGrid.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
			this.mDataGrid.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
			this.mDataGrid.CanUserAddRows = false;
			this.mDataGrid.CanUserResizeColumns = false;
			this.mDataGrid.CanUserSortColumns = false;
			this.mDataGrid.CanUserReorderColumns = false;
			this.mDataGrid.CanUserResizeRows = false;
			this.mDataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
			this.mDataGrid.IsReadOnly = true;
			this.mDataGrid.MinColumnWidth = 40;
			this.mDataGrid.AutoGenerateColumns = false;
		}
		#endregion

		#region IDispose Implementation
		/// <summary>
		/// Dispose method for the IDisposable implementation.
		/// </summary>
		public void Dispose()
		{
			this.mWorker.Dispose();
			this.mOriginalDataSet.Dispose();
		}
		#endregion
	}
}