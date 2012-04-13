using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.Core.DsspHttpUtilities;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using usbWrapper = Pololu.UsbWrapper;
using Pololu.Usc;
using W3C.Soap;

using submgr = Microsoft.Dss.Services.SubscriptionManager;

namespace PololuMaestro
{
    [Contract(Contract.Identifier)]
    [DisplayName("PololuMaestroBoard")]
    [Description("Pololu Maestro Servo Controller")]
    class PololuMaestroBoard : DsspServiceBase
    {
        private const int MinPollingInterval = 20;

        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        [InitialStatePartner(Optional = true, ServiceUri = @"kobush\config\PololuMaestro.config.xml")]
        private PololuMaestroState _state;

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/PololuMaestroBoard", AllowMultipleInstances = true)]
        private PololuMaestroOperations _mainPort = new PololuMaestroOperations();

        [SubscriptionManagerPartner]
        private submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        DsspHttpUtilitiesPort _httpUtilities = new DsspHttpUtilitiesPort();

        // internal port to synchronize polling
        private Port<DateTime> _servoPollingPort = new Port<DateTime>();

        private Usc _usc;

        private Port<DateTime>[] _setChannelThrotling;
        private Port<SetChannel>[] _setServoPorts;

        /// <summary>
        /// Service constructor
        /// </summary>
        public PololuMaestroBoard(DsspServiceCreationPort creationPort)
            : base(creationPort)
        { }

        /// <summary>
        /// Service start
        /// </summary>
        protected override void Start()
        {
            _httpUtilities = DsspHttpUtilitiesService.Create(this.Environment);

            // initialize state
            if (_state == null)
            {
                // No persisted state file, initialize with default values.
                _state = new PololuMaestroState();
                _state.Channels = new List<ChannelInfo>();
                _state.PollingInterval = 100; // default interval 100msec

                // Save state to file.
                SaveState(_state);
            }
            base.Start();

            MainPortInterleave.CombineWith(
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<SetChannel>(true, _mainPort, SetChannelHandler),
                        /*// http://social.msdn.microsoft.com/Forums/en-US/roboticsccr/thread/c3c27886-9cce-437e-b7bc-f0c1c0d5d9d9
                        Arbiter.JoinedReceiveWithIterator<DateTime, SetChannel>(true,
                            _setChannelThrotling, _mainPort, SetChannelHandler),*/
                        Arbiter.ReceiveWithIterator(true, _servoPollingPort, PollChannelStatus)
                        ),
                    new ConcurrentReceiverGroup())
            );

            // Display browser accesible Service URI
            LogInfo("Service uri: " + ServiceInfo.Service);

            if (ConnectToDevice())
            {
                // notify state has changed
                SendNotification(_submgrPort, new Replace());

                // start polling
                _servoPollingPort.Post(DateTime.Now);
            }
        }

        protected override void Shutdown()
        {

            DisconnectFromDevice();

            Activate(Arbiter.Choice(SaveState(_state),
                                    success => { }, 
                                    fault => LogError("Error saving state", fault)));

            base.Shutdown();
        }

        #region DSS Handlers

/*
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual void HttpGetHandler(HttpGet httpGet)
        {
            httpGet.ResponsePort.Post(new HttpResponseType(_state));
        }
*/
            
        [ServiceHandler(ServiceHandlerBehavior.Concurrent)]
        public virtual void GetHandler(Get get)
        {
            get.ResponsePort.Post(_state);
        }

        /// <summary>
        /// Handles Subscribe messages
        /// </summary>
        /// <param name="subscribe">the subscribe request</param>
        [ServiceHandler (ServiceHandlerBehavior.Concurrent)]
        public IEnumerator<ITask> SubscribeHandler(Subscribe subscribe)
        {
            SubscribeRequestType request = subscribe.Body;
            LogInfo("Subscribe request from: " + request.Subscriber);

            // Send a notification on successful subscription so that the 
            // subscriber can initialize its own state
            yield return Arbiter.Choice(
                SubscribeHelper(_submgrPort, request, subscribe.ResponsePort),
                success => SendNotificationToTarget<Replace>(request.Subscriber, _submgrPort, _state),
                exception => LogError("Subscribe failed", exception));
        }

        [ServiceHandler(ServiceHandlerBehavior.Exclusive)]
        public void GetDeviceListHandler(GetDeviceList get)
        {
            try
            {
                List<usbWrapper.DeviceListItem> devices = Usc.getConnectedDevices();
                
                var response = new GetDeviceListResponseType();
                response.Devices = new DeviceListItem[devices.Count];
                for (int i = 0; i < devices.Count; i++)
                {
                    response.Devices[i] = new DeviceListItem
                                              {
                                                  DisplayName = devices[i].text,
                                                  SerialNumber = devices[i].serialNumber,
                                                  ProductId = devices[i].productId,
                                                  Guid = devices[i].guid
                                              };
                }

                get.ResponsePort.Post(response);
            }
            catch (Exception ex)
            {
                LogError("Error reading device list", ex);
                throw;
            }
        }

        #endregion

        private bool ConnectToDevice()
        {
            DisconnectFromDevice();

            List<usbWrapper.DeviceListItem> devices = Usc.getConnectedDevices();
            if (devices.Count == 0)
            {
                LogWarning(LogGroups.Console, "No device connected.");
                return false;
            }

            usbWrapper.DeviceListItem dev = null;
            if (!string.IsNullOrEmpty(_state.SerialNumber))
                dev = devices.Find(d => d.serialNumber == _state.SerialNumber);

            if (dev == null)
            {
                // user first device
                dev = devices[0];
                _state.SerialNumber = dev.serialNumber;
            }

            LogVerbose("Connecting to device " + dev.text);
            try
            {
                _usc = new Usc(dev);
            }
            catch (Exception ex)
            {
                LogError("Could not connect to device", ex);
                throw;
            }

            LogInfo(string.Format("Connected to device #{0} (firmware {1}) with {2} servo",
                _usc.getSerialNumber(), _usc.firmwareVersionString, _usc.servoCount));
            
            _state.Connected = true;
            if (_state.Channels == null || _state.Channels.Count != _usc.servoCount)
            {
                
                _state.Channels = new List<ChannelInfo>(_usc.servoCount);
                
                for (int i = 0; i < _usc.servoCount; i++)
                {
                    _state.Channels.Add(new ChannelInfo {Index = i});
                }
            }
            UpdateChannelSettings();

            _setServoPorts = new Port<SetChannel>[_usc.servoCount];
            _setChannelThrotling = new Port<DateTime>[_usc.servoCount];
            for (int i = 0; i < _usc.servoCount; i++)
            {
                _setServoPorts[i] = new Port<SetChannel>();
                _setChannelThrotling[i] = new Port<DateTime>();
                RegisterSetChannelReceiver(i);
            }

            return true;
        }

        private void DisconnectFromDevice()
        {
            _state.Connected = false;

            if (_setServoPorts != null)
            {
                foreach (var servoPort in _setServoPorts)
                    servoPort.Clear();
            }
            _setServoPorts = null;
            if (_setChannelThrotling != null)
            {
                foreach (var port in _setChannelThrotling)
                    port.Clear();
            }
            _setChannelThrotling = null;
            if (_usc != null)
            {
                try
                {
                    _usc.Dispose();
                    _usc = null;
                }
                catch (Exception ex)
                {
                    LogError("Error disconnecting device", ex);
                    throw;
                }
            }
        }

        private void UpdateChannelSettings()
        {
            try
            {
                UscSettings settings = _usc.getUscSettings();
                DateTime dt = DateTime.Now;

                for (int i = 0; i < settings.channelSettings.Count; i++)
                {
                    var cs = settings.channelSettings[i];
                    var s = _state.Channels[i].Setting ?? (_state.Channels[i].Setting = new ChannelSetting());

                    if (!string.IsNullOrEmpty(cs.name)) s.Name = cs.name;
                    s.Mode = (ChannelMode) cs.mode;
                    s.HomeMode = (HomeMode) cs.homeMode;
                    s.HomePosition = cs.home;
                    s.MinimumPosition = cs.minimum;
                    s.MaximumPosition = cs.maximum;
                    s.NeutralPosition = cs.neutral;
                    s.Range = cs.range;
                    s.MaximumSpeed = cs.speed;
                    s.MaximumAcceleration = cs.acceleration;
                    s.Timestamp = dt;
                }
            }
            catch(Exception ex)
            {
                LogError("Error updating device settings.", ex);
                throw;
            }
        }

        private int frames = 0;
        private DateTime lastUpdate;

        private IEnumerator<ITask> PollChannelStatus(DateTime dt)
        {
            if (_usc == null)
                throw new Exception("Device not initialized.");

            try
            {


                ServoStatus[] servoStatuses;
                _usc.getVariables(out servoStatuses);
                var now = DateTime.UtcNow;

                bool anyChanged = false;
                for (int i = 0; i < servoStatuses.Length; i++)
                {
                    var changed = false;
                    var servo = _state.Channels[i];
                    if (servo.Pose == null)
                        servo.Pose = new ChannelPose();

                    if (servo.Pose.Target != servoStatuses[i].target)
                    {
                        servo.Pose.Target = servoStatuses[i].target;
                        changed = true;
                    }
                    if (servo.Pose.Position != servoStatuses[i].position)
                    {
                        servo.Pose.Position = servoStatuses[i].position;
                        changed = true;
                    }
                    if (servo.Pose.Acceleration != servoStatuses[i].acceleration)
                    {
                        servo.Pose.Acceleration = servoStatuses[i].acceleration;
                        changed = true;
                    }
                    if (servo.Pose.Speed != servoStatuses[i].speed)
                    {
                        servo.Pose.Speed = servoStatuses[i].speed;
                        changed = true;
                    }
                    if (changed)
                    {
                        servo.Pose.Timestamp = now;
                        SendNotification(_submgrPort, new ChannelChange(new ServoChangeRequestType(servo.Index, servo.Pose)));
                        anyChanged = true;
                    }
                }

                if (anyChanged)
                {
                    var save = SaveState(_state);
                    yield return (Choice) save;
                    var fault = (Fault) save;
                    if (fault != null)
                    {
                        LogError("Error saving state", fault);
                    }
                }

                frames++;
                if ((now - lastUpdate) > TimeSpan.FromSeconds(1))
                {
                    LogInfo("Polling FPS: " + frames/(now - lastUpdate).TotalSeconds);
                    frames = 0;
                    lastUpdate = now;
                }
            }
            finally
            {
                // Ensure we haven't been droppped
                if (ServicePhase == ServiceRuntimePhase.Started)
                {
                    // polling is enabled when PollingInterval is greater then zero
                    if (_state.Connected && _state.PollingInterval > 0)
                    {
                        // Issue another polling request
                        Activate(TimeoutPort(_state.PollingInterval > MinPollingInterval ? _state.PollingInterval : MinPollingInterval).Receive(_servoPollingPort.Post));
                    }
                }
            }
        }

        private void SetChannelHandler(SetChannel setChannel)
        {
            _setServoPorts[setChannel.Body.ServoIndex].Post(setChannel);
        }

        private void RegisterSetChannelReceiver(int i)
        {
            MainPortInterleave.CombineWith(
                new Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.JoinedReceiveWithIterator(false,
                                                          _setChannelThrotling[i], _setServoPorts[i],
                                                          SetChannelHandlerCore)
                        ),
                    new ConcurrentReceiverGroup()
                    ));

            Activate(TimeoutPort(100).Receive(_setChannelThrotling[i].Post));
        }

        private IEnumerator<ITask> SetChannelHandlerCore(DateTime dt, SetChannel setChannel)
        {
            if (_usc == null)
            {
                setChannel.ResponsePort.Post(Fault.FromException(new Exception("Device not initialized")));
                yield break;
            }

            var i = (byte)setChannel.Body.ServoIndex;

            try
            {
                LogVerbose("SetChannel received for servo " + i);

                // drain newer update commands and get to the latest
                var pending = _setServoPorts[i].ItemCount;
                if (pending > 0) LogInfo(string.Format("SetChannel #{0}: Draining {1} pending request", i, pending));
                while (pending > 0)
                {
                    SetChannel newerSetServo;
                    if (_setServoPorts[i].Test(out newerSetServo))
                    {
/*
                        if (newerSetServo.ResponsePort != null)
                            newerSetServo.ResponsePort.Post(DefaultUpdateResponseType.Instance);
*/
                        setChannel = newerSetServo;
                    }
                    pending--;
                }
                var servoState = setChannel.Body;

                // set servo methods
                if (servoState.Acceleration != null)
                    _usc.setSpeed(i, (ushort)servoState.Acceleration);
                if (servoState.Speed != null)
                    _usc.setSpeed(i, (ushort)servoState.Speed);
                if (servoState.Target != null)
                    _usc.setTarget(i, (ushort)servoState.Target);

                // notify on completion
                setChannel.ResponsePort.Post(DefaultUpdateResponseType.Instance);

                // trigger update state
                _servoPollingPort.Post(DateTime.Now);
            }
            catch (Exception ex)
            {
                LogError("Error updating servo state");
                setChannel.ResponsePort.Post(Fault.FromException(ex));
            }
            finally
            {
                // send next throtling message
                RegisterSetChannelReceiver(i);
            }
        }
    }
}


