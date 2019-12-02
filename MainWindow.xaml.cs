using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bezier.Processing;
using Bezier.ViewModel;

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
