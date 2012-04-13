using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttp;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace PololuMaestro
{
    /// <summary>
    /// PololuMaestro contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for PololuMaestro
        /// </summary>
        [DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2012/04/pololumaestro.html";
    }

    /// <summary>
    /// PololuMaestro main operations port
    /// </summary>
    [ServicePort]
    public class PololuMaestroOperations : PortSet<
        DsspDefaultLookup, 
        DsspDefaultDrop, 
        Get, 
        HttpGet,
        Subscribe,
        Replace, 
        GetDeviceList,
        SetChannel,
        ChannelChange
        >
    { }

    #region Get operation

    /// <summary>
    /// PololuMaestro get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<PololuMaestroState, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        public Get()
        { }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        public Get(GetRequestType body)
            : base(body)
        { }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Get(GetRequestType body, PortSet<PololuMaestroState, Fault> responsePort)
            : base(body, responsePort)
        { }
    }

    #endregion

    #region Subscribe operation

    /// <summary>
    /// PololuMaestro subscribe operation
    /// </summary>
    public class Subscribe : Subscribe<SubscribeRequestType, PortSet<SubscribeResponseType, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        public Subscribe()
        {}

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        public Subscribe(SubscribeRequestType body)
            : base(body)
        {}

        /// <summary>
        /// Creates a new instance of Subscribe
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Subscribe(SubscribeRequestType body, PortSet<SubscribeResponseType, Fault> responsePort)
            : base(body, responsePort)
        {}
    }

    #endregion 

    #region Replace operation

    /// <summary>
    /// PololuMaestro Replace Operation
    /// </summary>
    /// <remarks>The Replace class is specific to a service because it uses the service state.</remarks>
    public class Replace : Replace<PololuMaestroState, PortSet<DefaultReplaceResponseType, Fault>>
    {
        public Replace()
            :base(new PololuMaestroState())
        {}

        public Replace(PololuMaestroState body) 
            : base(body)
        {}
    }

    #endregion

    #region GetDeviceList operation

    public class GetDeviceList : Get<GetRequestType, PortSet<GetDeviceListResponseType, Fault>>
    {}

    [DataContract]
    public class GetDeviceListResponseType
    {
        [DataMember]
        public DeviceListItem[] Devices { get; set; }
    }

    [DataContract]
    public class DeviceListItem
    {
        [DataMember]
        public string SerialNumber { get; set; }
        
        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public ushort ProductId { get; set; }

        [DataMember]
        public Guid Guid { get; set; }
    }

    #endregion

    #region SetChannel operation

    public class SetChannel : Update<SetServoRequestType, PortSet<DefaultUpdateResponseType, Fault>>
    {
        public SetChannel ()
            : base(new SetServoRequestType())
        {}
    }

    [DataContract]
    public class SetServoRequestType
    {
        [DataMember]
        public int ServoIndex { get; set; }

        [DataMember]
        public ushort? Target { get; set; }

        [DataMember]
        public ushort? Acceleration { get; set; }

        [DataMember]
        public ushort? Speed { get; set; }
    }

    #endregion

    #region ChannelChange notification 

    public class ChannelChange : Update<ServoChangeRequestType, PortSet<DefaultUpdateResponseType, Fault>>
    {
        public ChannelChange() 
            : base(new ServoChangeRequestType())
        {}

        public ChannelChange(ServoChangeRequestType body)
            : base(body)
        {}
    }

    [DataContract]
    public class ServoChangeRequestType
    {
        public ServoChangeRequestType()
        { }

        public ServoChangeRequestType(int index, ChannelPose pose)
        {
            Index = index;
            CurrentPose = pose;
        }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public ChannelPose CurrentPose { get; set; }
    }

    #endregion

    #region State

    /// <summary>
    /// PololuMaestro state
    /// </summary>
    [DataContract]
    public class PololuMaestroState
    {
        public PololuMaestroState ()
        {
            PollingInterval = 100;
        }

        /// <summary>
        /// Gets or sets the list of servos
        /// </summary>
        [DataMember]
        public List<ChannelInfo> Channels { get; set; }

        [DataMember]
        [Description("Serial number of selected device.")]
        public string SerialNumber { get; set; }

        [DataMember]
        public bool Connected { get; set; }

        [DataMember]
        public int PollingInterval { get; set; }
    }

    [DataContract]
    [Description("The state of a servo.")]
    public class ChannelInfo
    {
        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public ChannelSetting Setting { get; set; }

        [DataMember]
        public ChannelPose Pose { get; set; }
    }

    [DataContract]
    public class ChannelPose
    {
        [DataMember]
        public ushort Target { get; set; }

        [DataMember]
        public ushort Position { get; set; }

        [DataMember]
        public byte Acceleration { get; set; }

        [DataMember]
        public ushort Speed { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    [DataContract]
    [Description("Servo settings")]
    public class ChannelSetting
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public ChannelMode Mode { get; set; }

        [DataMember]
        public HomeMode HomeMode { get; set; }

        [DataMember]
        public ushort HomePosition { get; set; }

        [DataMember]
        public ushort MinimumPosition { get; set; }

        [DataMember]
        public ushort MaximumPosition { get; set; }

        [DataMember]
        public ushort NeutralPosition { get; set; }

        [DataMember]
        public ushort Range { get; set; }

        [DataMember]
        public ushort MaximumSpeed { get; set; }

        [DataMember]
        public byte MaximumAcceleration { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    public enum ChannelMode
    {
        Servo = 0,
        ServoMultiplied = 1,
        Output = 2,
        Input = 3,
    }

    public enum HomeMode
    {
        Off,
        Ignore,
        Goto
    }
    #endregion
}


