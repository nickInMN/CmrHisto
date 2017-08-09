using System.Collections.Generic;
using System.Windows;
using CmrHisto.Triangulator;
using SharpGL.Enumerations;

namespace CmrHisto.ViewModels
{
    /// <summary>
    /// The view model for the Surface Map Dialog.
    /// </summary>
    public class SurfaceMapDialogViewModel : BaseViewModel
    {
        public SurfaceMapDialogViewModel()
        {
            this.RenderModes = new List<string>
            {
                "Points",
                "Lines",
                "Surface"
            };

            this.DataTypes = new List<Enums.DataType>
            {
                Enums.DataType.Minimum,
                Enums.DataType.Maximum,
                Enums.DataType.Average,
                Enums.DataType.Last
            };
        }

        private Enums.DataType _dataType;
        private string _legendHighText;
        private string _legendImagePath;
        private string _legendLowText;
        private string _legendMidText;
        private Visibility _legendVisibility;
        private IEnumerable<string> _pidNames;
        private string _renderMode;
        private bool _showColors;
        private Visibility _showColorsVisibility;
        private bool _showLines;
        private Visibility _showLinesVisibility;
        private bool _showMapEnabled;

        /// <summary>
        /// Gets or sets the <see cref="Enums.DataType"/> to use when rendering the surface map.
        /// </summary>
        public Enums.DataType DataType
        {
            get { return this._dataType; }
            set { this.SetValue(ref this._dataType, value); }
        }

        /// <summary>
        /// Gets or sets the list of <see cref="Enums.DataType"/> options.
        /// </summary>
        public List<Enums.DataType> DataTypes { get; set; }

        /// <summary>
        /// Gets or sets the high value text for the legend.
        /// </summary>
        public string LegendHighText
        {
            get { return this._legendHighText; }
            set { this.SetValue(ref this._legendHighText, value); }
        }

        /// <summary>
        /// Gets or sets the path to the legend image.
        /// </summary>
        public string LegendImagePath
        {
            get { return this._legendImagePath; }
            set { this.SetValue(ref this._legendImagePath, value); }
        }

        /// <summary>
        /// Gets or sets the low value text for the legend.
        /// </summary>
        public string LegendLowText
        {
            get { return this._legendLowText; }
            set { this.SetValue(ref this._legendLowText, value); }
        }

        /// <summary>
        /// Gets or sets the middle value text for the legend.
        /// </summary>
        public string LegendMidText
        {
            get { return this._legendMidText; }
            set { this.SetValue(ref this._legendMidText, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Visibility"/> for the legend.
        /// </summary>
        public Visibility LegendVisibility
        {
            get { return this._legendVisibility; }
            set { this.SetValue(ref this._legendVisibility, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="MapData"/> object that contains information about the map.
        /// </summary>
        public MapData MapData { get; set; }

        /// <summary>
        /// Gets or sets the list of pid names
        /// </summary>
        public IEnumerable<string> PidNames
        {
            get { return this._pidNames; }
            set { this.SetValue(ref this._pidNames, value); }
        }

        /// <summary>
        /// Gets or sets the string used to determine the rendering mode for the map.
        /// The acceptable values are:
        ///     Points - Show points for each data point.
        ///     Lines - Show a wireframe for the data points.
        ///     Surface - Show a colored surface for the data points.
        /// </summary>
        public string RenderMode
        {
            get { return this._renderMode; }
            set
            {
                if (this.SetValue(ref this._renderMode, value))
                {
                    switch (value)
                    {
                        case "Points":
                            this.ShowColorsVisibility = Visibility.Visible;
                            this.ShowLinesVisibility = Visibility.Hidden;
                            break;
                        case "Lines":
                            this.ShowColorsVisibility = Visibility.Visible;
                            this.ShowLinesVisibility = Visibility.Hidden;
                            break;
                        case "Surface":
                        default:
                            this.ShowColorsVisibility = Visibility.Hidden;
                            this.ShowLinesVisibility = Visibility.Visible;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the list of rendering modes.
        /// </summary>
        public List<string> RenderModes { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not colors should be shown on the surface map.
        /// </summary>
        public bool ShowColors
        {
            get { return this._showColors; }
            set { this.SetValue(ref this._showColors, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Visibility"/> value for the Show Colors checkbox.
        /// </summary>
        public Visibility ShowColorsVisibility
        {
            get { return this._showColorsVisibility; }
            set { this.SetValue(ref this._showColorsVisibility, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not lines should be shown on the surface map.
        /// </summary>
        public bool ShowLines
        {
            get { return this._showLines; }
            set { this.SetValue(ref this._showLines, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="Visibility"/> value for the Show Lines checkbox.
        /// </summary>
        public Visibility ShowLinesVisibility
        {
            get { return this._showLinesVisibility; }
            set { this.SetValue(ref this._showLinesVisibility, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the Show Map button should be enabled.
        /// </summary>
        public bool ShowMapEnabled
        {
            get { return this._showMapEnabled; }
            set { this.SetValue(ref this._showMapEnabled, value); }
        }

        /// <summary>
        /// Gets or sets the name of the selected pid.
        /// </summary>
        public string SelectedPidName { get; set; }

        /// <summary>
        /// Gets or sets a list of <see cref="Triangle"/> objects that represent the polygons to render the surface map.
        /// </summary>
        public List<Triangle> Triangles { get; set; }
    }
}