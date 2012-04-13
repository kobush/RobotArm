using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;

using pololuproxy = PololuMaestro.Proxy;
using armproxy = Microsoft.Robotics.Services.ArticulatedArm.Proxy;

namespace RobotArm
{
    /// <summary>
    /// RobotArm contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for RobotArm
        /// </summary>
        [DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2012/04/robotarm.html";
    }


    /// <summary>
    /// RobotArm main operations port
    /// </summary>
    [ServicePort]
    public class RobotArmOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get, Subscribe>
    {
    }

    /// <summary>
    /// RobotArm get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<RobotArmState, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        public Get()
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        public Get(GetRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Get(GetRequestType body, PortSet<RobotArmState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }

    /// <summary>
    /// RobotArm subscribe operation
    /// </summary>
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        public Subscribe()
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        public Subscribe(SubscribeRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Subscribe(SubscribeRequestType body, PortSet<SubscribeResponseType, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }


    /// <summary>
    /// RobotArm state
    /// </summary>
    [DataContract]
    public class RobotArmState : armproxy.ArticulatedArmState
    {
        RobotArmState ()
        {
        }
    }

    [DataContract]
    public class JointState
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int Channel { get; set; }

        [DataMember]
        public double Angle { get; set; }

        [DataMember]
        public double TargetAngle { get; set; }

        [DataMember]
        public double MinAngle { get; set; }

        [DataMember]
        public double MaxAngle { get; set; }

        [XmlIgnore]
        public pololuproxy.ChannelSetting ChannelSetting { get; set; }
    }
}


