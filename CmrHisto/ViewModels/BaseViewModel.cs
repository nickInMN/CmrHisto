using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CmrHisto.ViewModels
{
    /// <summary>
    /// Base View Model.
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        private string _busyText;
        private bool _isBusy;
        private string _title;

        /// <summary>
        /// Property Changed event handler.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Implementation of the OnPropertyChanged event handler.
        /// </summary>
        /// <param name="propertyName">The name of the calling property</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Update the value of the specified property with the supplied value and call the PropertyChanged event handler.
        /// </summary>
        /// <typeparam name="T">The type of property.</typeparam>
        /// <param name="field">The backing field to set the value to.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property beting set.</param>
        /// <returns><c>True</c> if the value was changed, <c>false</c> if it wasn't.</returns>
        protected bool SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Gets or sets the text to display when the busy indicator is visible.
        /// </summary>
        public string BusyText
        {
            get { return this._busyText; }
            set { this.SetValue(ref this._busyText, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the application should show the busy indicator.
        /// </summary>
        public bool IsBusy
        {
            get { return this._isBusy; }
            set { this.SetValue(ref this._isBusy, value); }
        }

        /// <summary>
        /// Gets or sets the title for the current window/dialog.
        /// </summary>
        public string Title
        {
            get { return this._title; }
            set { this.SetValue(ref this._title, value); }
        }
    }
}