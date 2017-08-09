// <copyright file="App.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using System.Windows;
using log4net;

namespace CmrHisto
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            base.OnStartup(e);
        }

        public App() : base()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => App.Log.Error("AppDomain.CurrentDomain.UnhandledException", (Exception)e.ExceptionObject);

            DispatcherUnhandledException += (s, e) => App.Log.Error("Application.Current.DispatcherUnhandledException", e.Exception);

            TaskScheduler.UnobservedTaskException += (s, e) => App.Log.Error("TaskScheduler.UnobservedTaskException", e.Exception);
        }
    }
}
