using System;
using System.Windows.Forms;
//using System.Data;
//using System.Drawing;

namespace Kobush.Simulation.RobotArm
{
    public partial class SimulatedRobotArmForm : Form
    {
        FromWinformEvents _fromWinformPort;

        // Short term memory for Save/Restore
        Single _x;
        Single _y;
        Single _z;
        Single _gripAngle;
        Single _gripRotation;
        Single _grip;
        Single _time;

        public SimulatedRobotArmForm(FromWinformEvents EventsPort)
        {
            _fromWinformPort = EventsPort;

            InitializeComponent();

            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Loaded, null, this));
        }

        public void SetErrorText(string error)
        {
            _errorLabel.Text = error;
        }

        public void SetPositionText(float x, float y, float z, float p, float w, float grip, float time)
        {
            _xText.Text = x.ToString();
            _yText.Text = y.ToString();
            _zText.Text = z.ToString();
            _gripAngleText.Text = p.ToString();
            _gripRotationText.Text = w.ToString();
            _gripText.Text = grip.ToString();
            _timeText.Text = time.ToString();
        }

        private void _startButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Start, null));
        }

        private void _resetButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Reset, null));
        }

        private void _submitButton_Click(object sender, EventArgs e)
        {
            try
            {
                MoveToPositionParameters moveParams = new MoveToPositionParameters();

                _errorLabel.Text = string.Empty;

                moveParams.X = Single.Parse(_xText.Text);
                moveParams.Y = Single.Parse(_yText.Text);
                moveParams.Z = Single.Parse(_zText.Text);
                moveParams.GripAngle = Single.Parse(_gripAngleText.Text);
                moveParams.GripRotation = Single.Parse(_gripRotationText.Text);
                moveParams.Grip = Single.Parse(_gripText.Text);
                moveParams.Time = Single.Parse(_timeText.Text);

                _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.MoveToPosition, null, moveParams));
            }
            catch
            {
                _errorLabel.Text = "Invalid Value";
            }
        }

        private void _reverseButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.ReverseDominos, null, null));
        }

        private void _parkButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Park, null, null));
        }

        private void _toppleButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.ToppleDominos, null, null));
        }

        private void _randomButton_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.RandomMove, null, null));
        }

        // Save and Restore functions for pose
        private void _saveButton_Click(object sender, EventArgs e)
        {
            Single x, y, z, gripAngle, gripRotation, grip, time;
            try
            {
                _errorLabel.Text = string.Empty;

                x = Single.Parse(_xText.Text);
                y = Single.Parse(_yText.Text);
                z = Single.Parse(_zText.Text);
                gripAngle = Single.Parse(_gripAngleText.Text);
                gripRotation = Single.Parse(_gripRotationText.Text);
                grip = Single.Parse(_gripText.Text);
                time = Single.Parse(_timeText.Text);
                // Succeeded in parsing value so set them now
                _x = x;
                _y = y;
                _z = z;
                _gripAngle = gripAngle;
                _gripRotation = gripRotation;
                _grip = grip;
                _time = time;
            }
            catch
            {
                _errorLabel.Text = "Invalid Value";
            }

        }

        private void _restoreButton_Click(object sender, EventArgs e)
        {
            SetPositionText(_x, _y, _z, _gripAngle, _gripRotation, _grip, _time);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MoveToParameters moveParams = new MoveToParameters();

                _errorLabel.Text = string.Empty;

                moveParams.BaseAngle = Single.Parse(_baseText.Text);
                moveParams.ShoulderAngle = Single.Parse(_shoulderText.Text);
                moveParams.ElbowAngle = Single.Parse(_elbowText.Text);
                moveParams.GripAngle = Single.Parse(_wristText.Text);
                moveParams.GripRotation = Single.Parse(_gripRotationText2.Text);
                moveParams.Grip = Single.Parse(_gripText2.Text);
                moveParams.Time = Single.Parse(_timeText2.Text);

                _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.MoveTo, null, moveParams));
            }
            catch
            {
                _errorLabel.Text = "Invalid Value";
            }
        }

        public void SetJointsText(float baseAngle, float shoulder, float elbow, float wrist, float wristRotate, float grip, float time)
        {
            _baseText.Text = baseAngle.ToString("F2");
            _shoulderText.Text = shoulder.ToString("F2");
            _elbowText.Text = elbow.ToString("F2");
            _wristText.Text = wrist.ToString("F2");
            _gripRotationText2.Text = wristRotate.ToString("F2");
            _gripText2.Text = grip.ToString("F2");
            _timeText2.Text = time.ToString("F2");
        }
    }
}