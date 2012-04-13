using System;
using GalaSoft.MvvmLight;
using PololuMaestro.Proxy;

namespace PololuMaestro.Dashboard.ViewModel
{
    public class ServoStateViewModel : ViewModelBase
    {
        private const double EPSILON = 0.0001;

        private ushort _acceleration;
        private int _index;
        private double _maximum;
        private double _minimum;
        private string _name;
        private double _position;
        private ushort _speed;
        private double _target;
        private double _neutral;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value) return;
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        public int Index
        {
            get { return _index; }
            set
            {
                if (_index == value) return;
                _index = value;
                RaisePropertyChanged("Index");
            }
        }

        public bool IsMoving
        {
            get { return Math.Abs(Target - Position) > EPSILON; }
        }

        public double Target
        {
            get { return _target; }
            set
            {
                if (Math.Abs(_target - value) < EPSILON) return;

                _target = value;
                RaisePropertyChanged("Target");
                RaisePropertyChanged("IsMoving");
            }
        }

        public double Position
        {
            get { return _position; }
            set
            {
                if (Math.Abs(_position - value) < EPSILON) return;
                _position = value;
                RaisePropertyChanged("Position");
                RaisePropertyChanged("IsMoving");
            }
        }

        public ushort Speed
        {
            get { return _speed; }
            set
            {
                if (_speed == value) return;
                _speed = value;
                RaisePropertyChanged("Speed");
            }
        }

        public ushort Acceleration
        {
            get { return _acceleration; }
            set
            {
                if (_acceleration == value) return;
                _acceleration = value;
                RaisePropertyChanged("Acceleration");
            }
        }

        public double Minimum
        {
            get { return _minimum; }
            set
            {
                if (Math.Abs(_minimum - value) < EPSILON) return;
                _minimum = value;
                RaisePropertyChanged("Minimum");
            }
        }

        public double Maximum
        {
            get { return _maximum; }
            set
            {
                if (Math.Abs(_maximum - value) < EPSILON) return;
                _maximum = value;
                RaisePropertyChanged("Maximum");
            }
        }

        public double Neutral
        {
            get { return _neutral; }
            set
            {
                if (Math.Abs(_neutral - value) < EPSILON) return;
                _neutral = value;
                RaisePropertyChanged("Neutral");
            }
        }

        public void Update(ChannelInfo s)
        {
            Index = s.Index;
            UpdatePose(s.Pose);
            UpdateSetting(s.Setting);
        }

        public void UpdatePose(ChannelPose s)
        {
            Position = s.Position/4.0;
            Target = s.Target/4.0;
            Speed = s.Speed;
            Acceleration = s.Acceleration;
        }

        private void UpdateSetting(ChannelSetting s)
        {
            if (s != null)
            {
                Name = s.Name;
                Neutral = s.NeutralPosition/4.0;
                Minimum = s.MinimumPosition/4.0;
                Maximum = s.MaximumPosition/4.0;
            }
            else
            {
                Minimum = 0;
                Maximum = 9000;
            }
        }
    }
}