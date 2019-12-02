using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bezier.Model;
using System.Drawing;

namespace Bezier.ViewModel
{
    public class ParametersViewModel : ViewModelBase
    {
        Parameters _parameters;
        public ParametersViewModel()
        {
            _parameters = new Parameters();
            SetDefaultValues();
        }

        private void SetDefaultValues()
        {
            IsPolylineVisible = true;
            IsGrayColors = false;
            IsRunning = false;
            Rotation = Rotation.Naive;
            Animation = Animation.Move;
        }
        public uint NumberOfPoints {
            get
            {
                return _parameters.NumberOfPoints;
            }
            set
            {
                _parameters.NumberOfPoints = value;
                RaisePropertyChanged(nameof(NumberOfPoints));
            }
        }

        public bool IsPolylineVisible
        {
            get
            {
                return _parameters.IsPolylineVisible;
            }
            set
            {
                _parameters.IsPolylineVisible = value;
                RaisePropertyChanged(nameof(IsPolylineVisible));
            }
        }
        public bool IsGrayColors {
            get
            {
                return _parameters.IsGrayColors;
            }
            set
            {
                _parameters.IsGrayColors = value;
                RaisePropertyChanged(nameof(IsGrayColors));
            }
        }
        public bool IsRunning {
            get
            {
                return _parameters.IsRunning;
            }
            set
            {
                _parameters.IsRunning = value;
                RaisePropertyChanged(nameof(IsRunning));
            }
        }
        public Bitmap Image {
            get
            {
                return _parameters.Image;
            }
            set
            {
                _parameters.Image = value;
                RaisePropertyChanged(nameof(Image));
            }
        }
        public Rotation Rotation {
            get
            {
                return _parameters.Rotation;
            }
            set
            {
                _parameters.Rotation = value;
                RaisePropertyChanged(nameof(Rotation));
            }
        }
        public Animation Animation {
            get
            {
                return _parameters.Animation;
            }
            set
            {
                _parameters.Animation = value;
                RaisePropertyChanged(nameof(Animation));
            }
        }

    }
}
