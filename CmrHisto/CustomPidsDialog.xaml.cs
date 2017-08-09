// <copyright file="CustomPidsDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for CustomPidsDialog.xaml
	/// </summary>
	public sealed partial class CustomPidsDialog : Window
	{
		#region Private Static Methods
		/// <summary>
		/// Make sure there is a valid XML file with PID information. If not, create and save an empty one.
		/// </summary>
		/// <returns><c>True</c> if the file is valid, <c>False</c> otherwise.</returns>
		private static bool ValidateCustomPidsFile()
		{
			if (!File.Exists("CustomPidNames.xml"))
			{
				XmlDocument pidDocument = new XmlDocument();
				pidDocument.AppendChild(pidDocument.CreateNode(XmlNodeType.XmlDeclaration, string.Empty, string.Empty));
				XmlNode pidListNode = pidDocument.CreateNode(XmlNodeType.Element, "pids", string.Empty);
				XmlNode commentNode = pidDocument.CreateNode(XmlNodeType.Comment, string.Empty, string.Empty);
				commentNode.Value = "\n\t\tAdd your own pid custom text here.  Enter the PID name EXACTLY as it appears in DSDataViewer \n\t\tin the \"name\" attribute, and the text you would like it replaced with in the \"custom\" attribute.\n\t\tFor Example, the following rule would replace \"RPM\" with \"Revolutions Per Minute\" when the PID\n\t\tis displayed in CmrHisto:\n\t\t<pid name=\"RPM\" custom=\"Revolutions Per Minute\" />\n\t";
				pidListNode.AppendChild(commentNode);
				pidDocument.AppendChild(pidListNode);
				try
				{
					pidDocument.Save("CustomPidNames.xml");
				}
				catch (XmlException)
				{
					MessageBox.Show("The CustomPidNames.xml file did not exist and there was a problem creating it.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
					return false;
				}
			}

			XmlDocument existingDocument = new XmlDocument();
			try
			{
				existingDocument.Load("CustomPidNames.xml");
			}
			catch
			{
				return false;
			}

			return true;
		}
		#endregion

		#region Private Event Handlers
		/// <summary>
		/// Handles the user clicking the add button. Validate the data entered and save the PID to the xml file if the data is valid.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void AddNewCustomPid(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(this.mNameTextbox.Text) || string.IsNullOrEmpty(this.mCustomTextbox.Text))
			{
				MessageBox.Show("You must enter a value for both Name and Custom!", CmrHisto.Properties.Resources.MessageBoxCustomPidError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else if (!CustomPidsDialog.ValidateCustomPidsFile())
			{
				MessageBox.Show("The CustomPidNames.xml file did not exist and there was a problem creating it. You custom PID was not saved.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			else
			{
				XmlDocument pidDocument = new XmlDocument();
				pidDocument.Load("CustomPidNames.xml");
				if (pidDocument.SelectSingleNode("pids").SelectSingleNode("pid[@name='" + this.mNameTextbox.Text + "']") != null)
				{
					MessageBox.Show("You already have a custom name defined for \"" + this.mNameTextbox.Text + "\"", CmrHisto.Properties.Resources.MessageExistingPidError, MessageBoxButton.OK, MessageBoxImage.Hand);
				}
				else
				{
					XmlNode pidNode = pidDocument.CreateNode(XmlNodeType.Element, "pid", string.Empty);
					XmlAttribute nameAttribute = pidDocument.CreateAttribute("name");
					nameAttribute.Value = this.mNameTextbox.Text;
					pidNode.Attributes.Append(nameAttribute);
					XmlAttribute customAttribute = pidDocument.CreateAttribute("custom");
					customAttribute.Value = this.mCustomTextbox.Text;
					pidNode.Attributes.Append(customAttribute);
					pidDocument.SelectSingleNode("pids").AppendChild(pidNode);
					try
					{
						pidDocument.Save("CustomPidNames.xml");
					}
					catch (XmlException)
					{
						MessageBox.Show("There was a problem saving the CustomPidNames.xml file. You custom PID may not have been saved.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
						return;
					}

					this.mNameTextbox.Text = string.Empty;
					this.mCustomTextbox.Text = string.Empty;
					this.GetPidsFromDocument();
				}
			}
		}

		/// <summary>
		/// Handle the user deleting a PID and remove it from the xml document.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void DeletePid(object sender, RoutedEventArgs e)
		{
			XmlDocument pidDocument = new XmlDocument();
			pidDocument.Load("CustomPidNames.xml");
			for (int i = 0; i < this.mExistingCustomPidsListbox.SelectedItems.Count; i++)
			{
				string pidName = ((string[])this.mExistingCustomPidsListbox.SelectedItems[i])[0].ToString();
				XmlNode pidNode = pidDocument.SelectSingleNode("pids").SelectSingleNode("pid[@name='" + pidName + "']");
				if (pidNode != null)
				{
					pidDocument.SelectSingleNode("pids").RemoveChild(pidNode);
				}
			}

			try
			{
				pidDocument.Save("CustomPidNames.xml");
				this.GetPidsFromDocument();
			}
			catch (XmlException)
			{
				MessageBox.Show("The CustomPidNames.xml file did not exist and there was a problem creating it.", CmrHisto.Properties.Resources.MessageBoxFileError, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
		}

		/// <summary>
		/// Handle the user clicking on a PID. Enable the delete button if they have a PID selected.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void PidListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.mDeletePidButton.IsEnabled = this.mExistingCustomPidsListbox.SelectedItems.Count > 0;
		}

		/// <summary>
		/// Update the custom PID document when this window is closed.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Event parameters.</param>
		private void Window_Closed(object sender, EventArgs e)
		{
			((CmrHistoMain)this.Owner).UpdateCustomPids();
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Get all the PIDs from the document.
		/// </summary>
		private void GetPidsFromDocument()
		{
			this.mExistingCustomPidsListbox.Items.Clear();
			XmlDocument pidDocument = new XmlDocument();
			pidDocument.Load("CustomPidNames.xml");
			foreach (XmlNode pidNode in pidDocument.SelectSingleNode("/pids"))
			{
				if ((pidNode.NodeType == XmlNodeType.Element && pidNode.Attributes != null) && pidNode.Attributes.Count == 2)
				{
					string[] pidInfo = new string[] { pidNode.Attributes["name"].Value, pidNode.Attributes["custom"].Value };
					this.mExistingCustomPidsListbox.Items.Add(pidInfo);
				}
			}
		}

		#endregion

		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomPidsDialog"/> class.
		/// </summary>
		public CustomPidsDialog()
		{
			this.InitializeComponent();
			if (!CustomPidsDialog.ValidateCustomPidsFile())
			{
				this.Close();
			}

			this.GetPidsFromDocument();
		}
		#endregion
	}
}
