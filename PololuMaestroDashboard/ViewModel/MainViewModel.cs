using System.Collections.ObjectModel;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using PololuMaestro.Proxy;

namespace PololuMaestro.Dashboard.ViewModel
{

    public class MainViewModel : ViewModelBase
    {
        private readonly PololuMaestroDashboard _service;
        private bool _updatingServoState;

        private ObservableCollection<ServoStateViewModel> _servos;
        private string _deviceSerialNumber;
        private ObservableCollection<DeviceListItem> _devices;
        private RelayCommand<string> _connectCommand;
        private RelayCommand<ServoStateViewModel> _setNeutralPositionCommand;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        /// <param name="service"> </param>
        public MainViewModel(PololuMaestroDashboard service)
        {
            _service = service;
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        public RelayCommand<string> ConnectCommand
        {
            get { return _connectCommand ?? (_connectCommand = new RelayCommand<string>(ExecuteConnect, CanExecuteConnect)); }
        }

        public RelayCommand<ServoStateViewModel> SetNeutralPositionCommand
        {
            get
            {
                return _setNeutralPositionCommand ??
                       (_setNeutralPositionCommand = new RelayCommand<ServoStateViewModel>(SetNeutralPosition));
            }
        }

        public ObservableCollection<ServoStateViewModel> Servos
        {
            get { return _servos; }
            private set
            {
                if (_servos == value) return;
                _servos = value;
                RaisePropertyChanged("Servos");
            }
        }

        public ObservableCollection<DeviceListItem> Devices
        {
            get { return _devices; }
            private set
            {
                if (_devices == value) return;
                _devices = value;
                RaisePropertyChanged("Devices");
            }
        }

        public string DeviceSerialNumber
        {
            get { return _deviceSerialNumber; }
            set
            {
                if (_deviceSerialNumber == value) return;
                _deviceSerialNumber = value;
                RaisePropertyChanged("DeviceSerialNumber");
            }
        }

        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                RaisePropertyChanged("IsConnected");
            }
        } 


        public void UpdateState(PololuMaestroState state)
        {
            DeviceSerialNumber = state.SerialNumber;
            IsConnected = state.Connected;

            if (Servos == null || Servos.Count != state.Channels.Count)
            {
                Servos = new ObservableCollection<ServoStateViewModel>();
                for (int i = 0; i < state.Channels.Count; i++)
                {
                    var vm = new ServoStateViewModel();
                    vm.PropertyChanged += OnServoStatePropertyChanged;
                    Servos.Add(vm);
                }
            }

            try
            {
                _updatingServoState = true;

                for (int i = 0; i < _servos.Count; i++)
                    Servos[i].Update(state.Channels[i]);
            }
            finally
            {
                _updatingServoState = false;
            }
        }

        public void UpdateChannel(int index, ChannelPose currentPose)
        {
            try
            {
                _updatingServoState = true;
                _servos[index].UpdatePose(currentPose);
            }
            finally
            {
                _updatingServoState = false;
            }
        }

        private void OnServoStatePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Target" && !_updatingServoState)
            {
                var vm = (ServoStateViewModel)sender;
                _service.SetServoState(vm.Index, (ushort) (vm.Target*4), vm.Speed, vm.Acceleration);
            }
        }

        public void UpdateDeviceList(GetDeviceListResponseType deviceList)
        {
            var temp = DeviceSerialNumber;

            Devices = new ObservableCollection<DeviceListItem>();
            Devices.Add(new DeviceListItem{DisplayName = "(none)"});
            foreach (var item in deviceList.Devices)
                Devices.Add(item);

            DeviceSerialNumber = temp;
        }

        private bool CanExecuteConnect(string serialNumber)
        {
            return !(string.IsNullOrEmpty(serialNumber));
        }

        private void ExecuteConnect(string serialNumber)
        {
            _service.ConnectDevice(serialNumber);
        }

        private void SetNeutralPosition(ServoStateViewModel vm)
        {
            _service.SetServoState(vm.Index, (ushort) (vm.Neutral*4), vm.Speed, vm.Acceleration);
        }


    }
}