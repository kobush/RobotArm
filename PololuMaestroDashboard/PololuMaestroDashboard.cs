using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using PololuMaestro.Dashboard.ViewModel;
using W3C.Soap;

using pololumaestro = PololuMaestro.Proxy;
using ccrwpf = Microsoft.Ccr.Adapters.Wpf;

namespace PololuMaestro.Dashboard
{
    [Contract(Contract.Identifier)]
    [DisplayName("PololuMaestroDashboard")]
    [Description("Dashboard for Pololu Maestro Board service")]
    public class PololuMaestroDashboard : DsspServiceBase
    {
        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        PololuMaestroDashboardState _state = new PololuMaestroDashboardState();

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/PololuMaestroDashboard", AllowMultipleInstances = true)]
        PololuMaestroDashboardOperations _mainPort = new PololuMaestroDashboardOperations();

        /// <summary>
        /// PololuMaestroService partner
        /// </summary>
        [Partner("PololuMaestroService", Contract = pololumaestro.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        pololumaestro.PololuMaestroOperations _pololuMaestroServicePort = new pololumaestro.PololuMaestroOperations();
        pololumaestro.PololuMaestroOperations _pololuMaestroServiceNotify = new pololumaestro.PololuMaestroOperations();

        private ccrwpf.WpfServicePort _wpfServicePort;
        private PololuMaestroUI _userInterface;

        /// <summary>
        /// Service constructor
        /// </summary>
        public PololuMaestroDashboard(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        /// <summary>
        /// Service start
        /// </summary>
        protected override void Start()
        {
            SpawnIterator(Initialize);
        }

        private IEnumerator<ITask> Initialize()
        {
            // create WPF adapter
            _wpfServicePort = ccrwpf.WpfAdapter.Create(TaskQueue);
            var runWindow = _wpfServicePort.RunWindow(() => new PololuMaestroUI{ ViewModel =  new MainViewModel(this)});
            yield return (Choice) runWindow;
            var exception = (Exception) runWindow;
            if (exception != null)
            {
                LogError(exception);
                StartFailed();
                yield break;
            }
            _userInterface = (Window) runWindow as PololuMaestroUI;

            // subscribe to Pololu Maestro
            var subscribe = _pololuMaestroServicePort.Subscribe(_pololuMaestroServiceNotify);
            yield return (Choice)subscribe;
            var fault = (Fault)subscribe;
            if (fault != null)
            {
                LogError(fault);
                StartFailed();
                yield break;
            }

            // register
            base.Start();

            // activate a handler for viseme notifications
            MainPortInterleave.CombineWith(
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<pololumaestro.Replace>(true, _pololuMaestroServiceNotify, ReplaceNotifyHandler),
                        Arbiter.Receive<pololumaestro.ChannelChange>(true, _pololuMaestroServiceNotify, ServoChangeNotifyHandler)
                        ),
                    new ConcurrentReceiverGroup()
                    )
                );

            SpawnIterator(StartCompleted);
        }

        private IEnumerator<ITask> StartCompleted()
        {
            var getDeviceList = _pololuMaestroServicePort.GetDeviceList();
            yield return (Choice) getDeviceList;
            
            var fault = (Fault) getDeviceList;
            if (fault != null)
            {
                LogError(fault);
                yield break;
            }

            var deviceList = (pololumaestro.GetDeviceListResponseType) getDeviceList;
            _wpfServicePort.Invoke(()=>_userInterface.ViewModel.UpdateDeviceList(deviceList));
        }

        private void ReplaceNotifyHandler(pololumaestro.Replace replace)
        {
            _wpfServicePort.Invoke(() => _userInterface.ViewModel.UpdateState(replace.Body));
        }

        private void ServoChangeNotifyHandler(pololumaestro.ChannelChange channelChange)
        {
            _wpfServicePort.Invoke(() => _userInterface.ViewModel.UpdateChannel(channelChange.Body.Index, channelChange.Body.CurrentPose));
        }

        public void UpdateFromUi()
        {
            var get = new GetRequestType();

            Activate(Arbiter.Choice(
                _pololuMaestroServicePort.Get(get),
                state => _wpfServicePort.Invoke(() => _userInterface.ViewModel.UpdateState(state)),
                fault => _wpfServicePort.Invoke(() => _userInterface.ShowFault(fault)))
            );
        }

        public void SetServoState(int servoIndex, ushort target, ushort speed, ushort acceleration)
        {
            var setServo = new pololumaestro.SetServoRequestType
                               {
                                   ServoIndex = servoIndex, 
                                   Target = target, 
                                   Speed = speed, 
                                   Acceleration = acceleration
                               };


            Activate(Arbiter.Choice(
                _pololuMaestroServicePort.SetChannel(setServo),
                state => { },
                fault => _wpfServicePort.Invoke(() => _userInterface.ShowFault(fault))
             ));
        }

        public void ConnectDevice(string serialNumber)
        {
            
        }
    }
}


