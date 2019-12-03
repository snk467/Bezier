using Bezier.ViewModel;
using System.Windows;

namespace Bezier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ParametersViewModel parameters;
        ControlsViewModel controls;
        public MainWindow()
        {
            InitializeComponent();
            parameters = new ParametersViewModel();
            controls = new ControlsViewModel(parameters, polylineLayerCanvas, imageLayerCanvas, numberOfPointsTextBox, generateButton, loadPolylineButton, savePolylineButton, imageLoadButton, startButton, stopButton, thumbnialImage);
            this.DataContext = parameters;
        }
    }
}
