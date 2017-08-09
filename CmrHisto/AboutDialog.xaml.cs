// <copyright file="AboutDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System.Reflection;
using System.Windows;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for AboutDialog.xaml.
	/// </summary>
	public sealed partial class AboutDialog : Window
	{
		#region Public Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="AboutDialog"/> class.
		/// </summary>
		public AboutDialog()
		{
			this.InitializeComponent();
			this.mVersionLabel.Content = string.Concat("CmrHisto Version ", Assembly.GetExecutingAssembly().GetName().Version.ToString());
		}
		#endregion
	}
}