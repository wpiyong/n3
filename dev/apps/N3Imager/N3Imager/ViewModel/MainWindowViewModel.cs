using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModelLib;

namespace N3Imager.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        public PhosphorescenceViewModel ResultVM { get; set; }
        public StatusViewModel StatusVM { get; set; }
        public CameraViewModel CameraVM { get; set; }
        public MeasurementViewModel MeasurementVM { get; set; }
        public AnalyzerViewModel AnalyzerVM { get; set; }

        public MainWindowViewModel()
        {
            base.DisplayName = "MainWindowViewModel";

            StatusVM = new StatusViewModel();
            StatusVM.Busy = 0;

            ResultVM = new PhosphorescenceViewModel(StatusVM);
            AnalyzerVM = new AnalyzerViewModel(StatusVM);
            CameraVM = new CameraViewModel(StatusVM);
            MeasurementVM = new MeasurementViewModel(this, StatusVM, CameraVM, ResultVM, AnalyzerVM);
        }

        int _tabIndex = 0;//0 = camera; 1 = results
        public int TabIndex
        {
            get { return _tabIndex; }
            set
            {
                _tabIndex = value;
                OnPropertyChanged("TabIndex");
                if(_tabIndex == 2)
                {
                    AnalyzerVM.InitCurrentImageListSelection();
                }
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            CameraVM.Dispose();
            MeasurementVM.Dispose();
            AnalyzerVM.Dispose();
            App.Current.Shutdown();

        }
    }
}
