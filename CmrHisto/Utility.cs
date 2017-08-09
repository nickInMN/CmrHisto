// <copyright file="Utility.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CmrHisto.Enums;
using CmrHisto.Properties;
using Microsoft.Win32;

namespace CmrHisto
{
	/// <summary>
	/// A static class full of utility methods.
	/// </summary>
	internal static class Utility
	{
		#region Internal Static Methods
		/// <summary>
		/// Creates a new matrix of size 17x17 initializing all the cells to new <see cref="CellData"/> objects.
		/// </summary>
		/// <param name="rpmSize">The number of RPM buckets.</param>
		/// <param name="yAxisSize">The number of y-axis (PRatio by default) buckets.</param>
		/// <returns>A new two-dimensional array of <see cref="CellData"/> objects.</returns>
		internal static CellData[,] CreateEmtpyMatrix(int rpmSize, int yAxisSize)
		{
			CellData[,] dataArray = new CellData[17, 17];
			for (int i = 0; i < rpmSize; i++)
			{
				for (int j = 0; j < yAxisSize; j++)
				{
					dataArray[i, j] = new CellData();
				}
			}

			return dataArray;
		}

		/// <summary>
		/// Creates a scale based on the provided upper and lower bounds.
		/// </summary>
		/// <param name="lowerBound">The low number for the scale.</param>
		/// <param name="upperBound">The high number for the scale.</param>
		/// <param name="rounding">The number of decimals to round.</param>
		/// <param name="yAxisText">Text for the y-axis.</param>
		/// <returns>A new <see cref="List"/> of <see cref="ScalingInformation"/> objects.</returns>
		internal static List<ScalingInformation> CreateScale(double lowerBound, double upperBound, int rounding, string yAxisText)
		{
			List<ScalingInformation> newScale = new List<ScalingInformation>(17);
			if (lowerBound >= upperBound)
			{
				MessageBox.Show("The upper limit must be greater than the lower limit!", Resources.MessageBoxScaleError, MessageBoxButton.OK, MessageBoxImage.Hand);
				return null;
			}

			double scaleSize = upperBound - lowerBound;
			double bucketSize = Math.Round((double)(scaleSize / 17), rounding);
			if (rounding == 2 && bucketSize < 0.001)
			{
				MessageBox.Show("The " + yAxisText + " range is not large enough, please select more cells.", Resources.MessageBoxScaleError, MessageBoxButton.OK, MessageBoxImage.Hand);
				return null;
			}

			if (rounding == 1 && bucketSize < 0.1)
			{
				MessageBox.Show("The RPM range is not large enough, please select more cells.", Resources.MessageBoxScaleError, MessageBoxButton.OK, MessageBoxImage.Hand);
				return null;
			}

			double roundValue = 0;
			double lowerValue = Math.Round(lowerBound, rounding);
			if (rounding == 1)
			{
				roundValue = 0.1;
			}
			else
			{
				roundValue = 0.001;
			}

			for (int i = 0; i < 17; i++)
			{
				ScalingInformation information;
				information = new ScalingInformation
				{
					MinValue = Math.Round(lowerValue, rounding),
					MaxValue = Math.Round((double)(lowerValue + bucketSize), rounding),
					Value = Math.Round((double)(lowerValue + (bucketSize / 2)), rounding)
				};

				newScale.Add(information);
				lowerValue = information.MaxValue + roundValue;
			}

			return newScale;
		}

		/// <summary>
		/// Creates a scale using the provided upper and lower bounds.
		/// </summary>
		/// <param name="isRpm">A value indicating whether this scale is for RPM or not.</param>
		/// <param name="lowerBound">The lower value for the scale.</param>
		/// <param name="upperBound">The upper value for the scale.</param>
		/// <param name="roundLength">The number of decimal points to round.</param>
		/// <returns>A <see cref="List"/> of <see cref="ScalingInformation"/> objects.</returns>
		internal static List<ScalingInformation> CreateScaleFromUpperAndLowerBounds(bool isRpm, double lowerBound, double upperBound, int roundLength)
		{
			List<ScalingInformation> newScale = new List<ScalingInformation>();
			double bucketSize = (upperBound - lowerBound) / 17;
			double bucketLowerValue = lowerBound;
			for (int i = 0; i < 17; i++)
			{
				ScalingInformation information;
				information = new ScalingInformation
				{
					MinValue = Math.Round(bucketLowerValue, roundLength),
					Value = Math.Round((double)(bucketLowerValue + (bucketSize / 2)), roundLength),
					MaxValue = Math.Round((double)(bucketLowerValue + bucketSize), roundLength)
				};

				newScale.Add(information);
				if (isRpm)
				{
					bucketLowerValue = information.MaxValue + 0.1;
				}
				else
				{
					bucketLowerValue = information.MaxValue + 0.001;
				}
			}

			return newScale;
		}

		/// <summary>
		/// Find the parent <see cref="DataGrid"/> based on the child.
		/// </summary>
		/// <param name="dataGridPart">The child control, for which we're trying to find the parent.</param>
		/// <returns>The <see cref="DataGrid"/> of the supplied control</returns>
		internal static DataGrid GetDataGridFromChild(DependencyObject dataGridPart)
		{
			if (VisualTreeHelper.GetParent(dataGridPart) == null)
			{
				throw new ArgumentException("Control is null.");
			}

			if (VisualTreeHelper.GetParent(dataGridPart) is DataGrid)
			{
				return (DataGrid)VisualTreeHelper.GetParent(dataGridPart);
			}

			return GetDataGridFromChild(VisualTreeHelper.GetParent(dataGridPart));
		}

		/// <summary>
		/// Gets the index of the row for the supplied child.
		/// </summary>
		/// <param name="dataGridCell">The cell whose row we're trying to find.</param>
		/// <returns>The index of the row.</returns>
		internal static int GetRowIndex(DataGridCell dataGridCell)
		{
			PropertyInfo property = dataGridCell.GetType().GetProperty("RowDataItem", BindingFlags.NonPublic | BindingFlags.Instance);
			return GetDataGridFromChild(dataGridCell).Items.IndexOf(property.GetValue(dataGridCell, null));
		}

		/// <summary>
		/// Gets the value from the specified <see cref="DataRow"/> with the corresponding colum number and type.
		/// </summary>
		/// <param name="columnType">The data type for the column.</param>
		/// <param name="row">The <see cref="DataRow"/> we're trying to get the value for.</param>
		/// <param name="columnNumber">The column number (PID) to get the value of.</param>
		/// <returns>The value of the specified PID in the row.</returns>
		internal static double? GetValueFromDataRow(string columnType, DataRow row, int columnNumber)
		{
			double? returnValue = null;
			try
			{
				if (string.Compare(columnType, "Double", StringComparison.OrdinalIgnoreCase) == 0)
				{
					return row.Field<double?>(columnNumber);
				}

				if ((string.Compare(columnType, "Int16", StringComparison.OrdinalIgnoreCase) == 0 || (string.Compare(columnType, "Int32", StringComparison.OrdinalIgnoreCase) == 0)) || (string.Compare(columnType, "Int", StringComparison.OrdinalIgnoreCase) == 0))
				{
					int? intValue = row.Field<int?>(columnNumber);
					if (intValue.HasValue)
					{
						returnValue = new double?((double)intValue.Value);
					}

					return returnValue;
				}

				if (string.Compare(columnType, "Int64", StringComparison.OrdinalIgnoreCase) == 0)
				{
					long? longValue = row.Field<long?>(columnNumber);
					if (longValue.HasValue)
					{
						returnValue = new double?((double)longValue.Value);
					}

					return returnValue;
				}

				double num;
				if (double.TryParse(row.Field<string>(columnNumber), NumberStyles.Number, CultureInfo.CurrentCulture, out num))
				{
					returnValue = new double?(num);
				}
			}
			catch (InvalidCastException)
			{
			}

			return returnValue;
		}

		/// <summary>
		/// Create a scale from the provided file.
		/// </summary>
		/// <param name="isRpm">A value indicating if an RPM scale is being loaded.</param>
		/// <param name="yAxisPid">The name of the PID to use for the Y-Axis.</param>
		/// <param name="fileName">The name of the scale file. Defaults to the empty string.</param>
		/// <returns>A new <see cref="List"/> of <see cref="ScalingInformation"/> objects.</returns>
		internal static List<ScalingInformation> LoadScaleFromFile(bool isRpm, string yAxisPid, string fileName = "")
		{
			bool wasOpened = false;
			if (string.IsNullOrEmpty(fileName))
			{
				wasOpened = true;
				OpenFileDialog dialog = new OpenFileDialog();
				if (isRpm)
				{
					dialog.Filter = "RPM Scale (*.csv)|*.csv";
				}
				else
				{
					dialog.Filter = yAxisPid + " Scale (*.csv)|*.csv";
				}

				dialog.RestoreDirectory = true;
				bool? showResult = dialog.ShowDialog().Value;
				if (showResult.HasValue && showResult.Value)
				{
					fileName = dialog.FileName;
				}
				else
				{
					// Return an emtpy scale so the dialog doesn't do anything.
					return new List<ScalingInformation>();
				}
			}

			List<ScalingInformation> newScale = new List<ScalingInformation>(17);
			try
			{
				FileInfo fileInfo = new FileInfo(fileName);
				if (fileInfo.Length == 0L)
				{
					if (wasOpened)
					{
						MessageBox.Show("The file appears to be empty.", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					}

					return null;
				}

				string[] scaleLines = File.ReadAllLines(fileName);

				if (scaleLines.Length != 18)
				{
					if (wasOpened)
					{
						MessageBox.Show("The file did not contain the expected number of rows. Please make sure it was exported from CmrHisto.", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					}

					return null;
				}

				string[] lineData = scaleLines[0].Split(new char[] { ',' });

				if (isRpm && (string.Compare(lineData[1], "RPM SCALE", StringComparison.OrdinalIgnoreCase) != 0))
				{
					if (wasOpened)
					{
						MessageBox.Show("This does not appear to be an RPM Scale file", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					}

					return null;
				}

				if (!isRpm && (string.Compare(lineData[1], yAxisPid + " SCALE", StringComparison.OrdinalIgnoreCase) != 0))
				{
					if (wasOpened)
					{
						MessageBox.Show("This does not appear to be a " + yAxisPid + " Scale file", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					}

					return null;
				}

				// Start at the second row because the first is the PID name, the second is the unit.
				for (int i = 1; i < scaleLines.Length; i++)
				{
					lineData = scaleLines[i].Split(new char[] { ',' });

					ScalingInformation item = new ScalingInformation
					{
						MinValue = Utility.ParseNullableDouble(lineData[1].ToString()).Value,
						Value = Utility.ParseNullableDouble(lineData[2].ToString()).Value,
						MaxValue = Utility.ParseNullableDouble(lineData[3].ToString()).Value
					};

					newScale.Add(item);
				}
			}
			catch (Exception)
			{
				if (wasOpened)
				{
					MessageBox.Show("There was an error opening the file, please make sure it is valid and try again.", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}

				return null;
			}

			return newScale;
		}

		/// <summary>
		/// Get the PID units from the csv file.
		/// </summary>
		/// <param name="fileName">The name of the log file to open.</param>
		/// <returns>An array of string with the PID units.</returns>
		internal static string[] LoadUnits(string fileName)
		{
			StreamReader reader = null;
			string[] units;
			try
			{
				reader = new StreamReader(fileName);
				string unitsLine = string.Empty;
				for (int i = 0; i < 2; i++)
				{
					unitsLine = reader.ReadLine();
				}

				reader.Close();
				reader = null;
				units = unitsLine.Split(new string[] { "," }, StringSplitOptions.None);
			}
			catch (ArgumentNullException)
			{
				units = null;
			}
			catch (ArgumentException)
			{
				units = null;
			}
			catch (FileNotFoundException)
			{
				units = null;
			}
			catch (DirectoryNotFoundException)
			{
				units = null;
			}
			catch (OutOfMemoryException)
			{
				units = null;
			}
			catch (IOException)
			{
				units = null;
			}
			finally
			{
				if (reader != null)
				{
					reader.Dispose();
				}
			}

			return units;
		}

		/// <summary>
		/// Parse the supplied text to a <see cref="double"/>.
		/// </summary>
		/// <param name="textValue">The string to try parsing.</param>
		/// <returns>The double value, or 0 if parsing failed.</returns>
		internal static double ParseDouble(string textValue)
		{
			double doubleValue;

			if (double.TryParse(textValue, NumberStyles.Number, CultureInfo.CurrentCulture, out doubleValue))
			{
				return doubleValue;
			}

			return 0d;
		}

		/// <summary>
		/// Parse the supplied text into a double, if it isn't valid return a null double.
		/// </summary>
		/// <param name="textValue">The string to try parsing.</param>
		/// <returns>The double value, or a null value if parsing failed.</returns>
		internal static double? ParseNullableDouble(string textValue)
		{
			double doubleValue;

			if (double.TryParse(textValue, NumberStyles.Number, CultureInfo.InvariantCulture, out doubleValue))
			{
				return new double?(doubleValue);
			}

			return new double?();
		}

		/// <summary>
		/// Read a CSV file.
		/// </summary>
		/// <param name="table">The <see cref="DataTable"/> to put the contents of the CSV into.</param>
		/// <param name="fileName">The name of the CSV file.</param>
		/// <param name="userAcceptedLargeFileWarning">A flag indicating whether or not the user agreed to the large file warning.</param>
		/// <returns>A <see cref="Status"/> code explaining the outcome of the operation.</returns>
		internal static Status ReadCsv(DataTable table, string fileName, bool userAcceptedLargeFileWarning)
		{
			Status result = Status.None;
			try
			{
				FileInfo fileInfo = new FileInfo(fileName);
				if (fileInfo.Length == 0L)
				{
					return Status.EmptyFile;
				}

				if (fileInfo.Length > 10485760 && !userAcceptedLargeFileWarning)
				{
					return Status.LargeFile;
				}

				string[] logLines = File.ReadAllLines(fileName);
				string[] headerData = logLines[0].Split(new char[] { ',' });
				int columnCount = headerData.Length;
				int timeColumn = -1;

				for (int i = 0; i < columnCount; i++)
				{
					if (string.Compare("Time", headerData[i], StringComparison.OrdinalIgnoreCase) == 0)
					{
						table.Columns.Add(headerData[i], typeof(string));
						timeColumn = i;
					}
					else
					{
						table.Columns.Add(headerData[i], typeof(double));
					}
				}

				// Start at the second row because the first is the PID name, the second is the unit.
				DataRow row;
				double? cellValue;
				string[] lineData = null;
				for (int i = 2; i < logLines.Length; i++)
				{
					lineData = logLines[i].Split(new char[] { ',' });
					row = table.NewRow();
					if (lineData.Length == columnCount)
					{
						for (int f = 0; f < columnCount; f++)
						{
							if (f == timeColumn)
							{
								row[f] = lineData[f];
							}
							else
							{
								cellValue = Utility.ParseNullableDouble(lineData[f]);
								if (cellValue.HasValue)
								{
									row[f] = cellValue.Value;
								}
								else
								{
									row[f] = DBNull.Value;
								}
							}
						}

						table.Rows.Add(row);
					}
				}

				result = Status.Success;
			}
			catch (ArgumentNullException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (SecurityException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (ArgumentException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (UnauthorizedAccessException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (PathTooLongException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (NotSupportedException)
			{
				result = Status.ErrorOpeningFile;
			}
			catch (InvalidOperationException)
			{
				result = Status.DataError;
			}
			catch (OleDbException)
			{
				result = Status.DataError;
			}

			return result;
		}

		/// <summary>
		/// Flips the array, which effectively allows the user to sort ascending or descending along the Y-Axis.
		/// </summary>
		/// <param name="data">The <see cref="CmrHistoDataCollection"/> object to sort.</param>
		/// <returns>A reversed version of the supplied <see cref="CmrHistoDataCollection"/>.</returns>
		internal static CmrHistoDataCollection ReverseData(CmrHistoDataCollection data)
		{
			CmrHistoDataCollection reversedData = new CmrHistoDataCollection();
			for (int i = data.Count - 1; i >= 0; i--)
			{
				reversedData.AddDataRow(data[i]);
			}

			return reversedData;
		}

		/// <summary>
		/// Creates a default scale.
		/// </summary>
		/// <param name="isRpm">A flag indicating whether an RPM scale is being created or not.</param>
		/// <returns>A new <see cref="List"/> of <see cref="ScalingInformation"/> objects.</returns>
		internal static List<ScalingInformation> SetDefaults(bool isRpm)
		{
			List<ScalingInformation> newScale = new List<ScalingInformation>();
			if (isRpm)
			{
				ScalingInformation rpmScaleInfo = new ScalingInformation
				{
					MinValue = 0,
					Value = 416,
					MaxValue = 506
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 506.1,
					Value = 596,
					MaxValue = 686
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 686.1,
					Value = 776,
					MaxValue = 931
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 931.1,
					Value = 1086,
					MaxValue = 1185
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 1185.1,
					Value = 1284,
					MaxValue = 1366.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 1366.6,
					Value = 1449,
					MaxValue = 1589.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 1589.6,
					Value = 1730,
					MaxValue = 1900
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 1900.1,
					Value = 2070,
					MaxValue = 2240
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 2240.1,
					Value = 2410,
					MaxValue = 2655
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 2655.1,
					Value = 2900,
					MaxValue = 3179
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 3179.1,
					Value = 3458,
					MaxValue = 3660.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 3660.6,
					Value = 3863,
					MaxValue = 4047.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 4047.6,
					Value = 4232,
					MaxValue = 4376.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 4376.6,
					Value = 4521,
					MaxValue = 4703.5
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 4703.6,
					Value = 4886,
					MaxValue = 5069
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 5069.1,
					Value = 5252,
					MaxValue = 5435
				};
				newScale.Add(rpmScaleInfo);

				rpmScaleInfo = new ScalingInformation
				{
					MinValue = 5435.1,
					Value = 5618,
					MaxValue = 99999
				};
				newScale.Add(rpmScaleInfo);

				return newScale;
			}

			ScalingInformation yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0,
				Value = 0.15,
				MaxValue = 0.175
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.176,
				Value = 0.2,
				MaxValue = 0.22
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.221,
				Value = 0.24,
				MaxValue = 0.265
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.266,
				Value = 0.29,
				MaxValue = 0.315
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.316,
				Value = 0.34,
				MaxValue = 0.36
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.361,
				Value = 0.38,
				MaxValue = 0.405
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.406,
				Value = 0.43,
				MaxValue = 0.455
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.456,
				Value = 0.48,
				MaxValue = 0.505
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.506,
				Value = 0.53,
				MaxValue = 0.55
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.551,
				Value = 0.57,
				MaxValue = 0.595
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.596,
				Value = 0.62,
				MaxValue = 0.645
			};
			newScale.Add(yAxisScaleInfo);
			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.646,
				Value = 0.67,
				MaxValue = 0.69
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.691,
				Value = 0.71,
				MaxValue = 0.735
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.736,
				Value = 0.76,
				MaxValue = 0.785
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.786,
				Value = 0.81,
				MaxValue = 0.83
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.831,
				Value = 0.85,
				MaxValue = 0.875
			};
			newScale.Add(yAxisScaleInfo);

			yAxisScaleInfo = new ScalingInformation
			{
				MinValue = 0.876,
				Value = 0.9,
				MaxValue = 99999
			};
			newScale.Add(yAxisScaleInfo);

			return newScale;
		}

		/// <summary>
		/// Displays a <see cref="MessageBox"/> with the appropriate message based on the supplied <see cref="Status"/>.
		/// </summary>
		/// <param name="status">The <see cref="Status"/>.</param>
		internal static void ShowMessageBox(Status status)
		{
			switch (status)
			{
				case Status.GeneralError:
					MessageBox.Show("There was an error processing the file.", Resources.MessageBoxProcessingError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return;

				case Status.ErrorOpeningFile:
					MessageBox.Show("There was an error opening or reading the selected file.", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return;

				case Status.EmptyFile:
					MessageBox.Show("The file you selected is empty.", Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return;

				case Status.LargeFile:
					break;

				case Status.DataError:
					MessageBox.Show("The data in the csv file was invalid or the file is open in another program.", Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return;

				case Status.RpmNotFound:
					MessageBox.Show("No columns that could be used for RPM were found!", Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return;

				case Status.RatioNotFound:
					MessageBox.Show("No columns that could be used for PRatio were found!", Resources.MessageBoxDataError, MessageBoxButton.OK, MessageBoxImage.Hand);
					break;

				default:
					return;
			}
		}

		/// <summary>
		/// Scales the provided value to match a desired scale.
		/// </summary>
		/// <param name="actualLowerBound">The real value of the lowest end of the range.</param>
		/// <param name="actualUpperBound">The real value of the highest end of the range.</param>
		/// <param name="newLowerBound">The new lower value of the lowest end of the range.</param>
		/// <param name="newUpperBound">The new lower value of the highest end of the range.</param>
		/// <param name="value">The value to scale.</param>
		/// <returns>The converted value based on the original value and range.</returns>
		internal static double ConvertRange(double actualLowerBound, double actualUpperBound, double newLowerBound, double newUpperBound, double value)
		{
			double scale = (double)(newUpperBound - newLowerBound) / (actualUpperBound - actualLowerBound);
			return newLowerBound + ((value - actualLowerBound) * scale);
		}
		#endregion
	}
}