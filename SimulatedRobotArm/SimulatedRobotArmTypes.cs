using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;
using armproxy = Microsoft.Robotics.Services.ArticulatedArm.Proxy;

namespace Kobush.RobotArm.Simulation
{
	public sealed class Contract
	{
		[DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2012/04/simulatedrobotarm.html";
	}
	
	[DataContract]
    public class RobotArmState : armproxy.ArticulatedArmState
	{
	}
	
	[ServicePort]
	public class RobotArmOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
	{
	}
	
	public class Get : Get<GetRequestType, PortSet<RobotArmState, Fault>>
	{
		public Get()
		{
		}
		
		public Get(GetRequestType body)
			: base(body)
		{
		}
		
		public Get(GetRequestType body, PortSet<RobotArmState, Fault> responsePort)
			: base(body, responsePort)
		{
		}
	}

    #region WinForms communication
    public class FromWinformEvents : Port<FromWinformMsg>
    {
    }

    public class FromWinformMsg
    {
        public enum MsgEnum
        {
            Loaded,
            MoveToPosition,
            MoveTo,
            Reset,
            Test,
            Start,
            ReverseDominos,
            Park,
            ToppleDominos,
            RandomMove
        }

        private string[] _parameters;
        public string[] Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        private MsgEnum _command;
        public MsgEnum Command
        {
            get { return _command; }
            set { _command = value; }
        }

        private object _object;
        public object Object
        {
            get { return _object; }
            set { _object = value; }
        }

        public FromWinformMsg(MsgEnum command, string[] parameters)
        {
            _command = command;
            _parameters = parameters;
        }
        public FromWinformMsg(MsgEnum command, string[] parameters, object objectParam)
        {
            _command = command;
            _parameters = parameters;
            _object = objectParam;
        }
    }

    public class MoveToPositionParameters
    {
        public float X;
        public float Y;
        public float Z;
        public float GripAngle;
        public float GripRotation;
        public float Grip;
        public float Time;
    }

    public class MoveToParameters
    {
        public float BaseAngle;
        public float ShoulderAngle;
        public float ElbowAngle;
        public float GripAngle;
        public float GripRotation;
        public float Grip;
        public float Time;
    }

    #endregion

}


