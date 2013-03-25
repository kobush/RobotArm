using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Kobush.RobotArm.Dashboard.ViewModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;

using submgr = Microsoft.Dss.Services.SubscriptionManager;
using articulatedarm = Microsoft.Robotics.Services.ArticulatedArm.Proxy;
using ccrwpf = Microsoft.Ccr.Adapters.Wpf;

namespace Kobush.RobotArm.Dashboard
{
    [Contract(Contract.Identifier)]
    [DisplayName("RobotArmDashboard")]
    [Description("RobotArmDashboard service (no description provided)")]
    class RobotArmDashboardService : DsspServiceBase
    {
        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        RobotArmDashboardState _state = new RobotArmDashboardState();

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/RobotArmDashboard", AllowMultipleInstances = true)]
        RobotArmDashboardOperations _mainPort = new RobotArmDashboardOperations();

        [SubscriptionManagerPartner]
        submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// Arm partner
        /// </summary>
        [Partner("Arm", Contract = articulatedarm.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        articulatedarm.ArticulatedArmOperations _armPort = new articulatedarm.ArticulatedArmOperations();
        articulatedarm.ArticulatedArmOperations _armNotify = new articulatedarm.ArticulatedArmOperations();

        private ccrwpf.WpfServicePort _wpfServicePort;
        private MainWindow _mainWindow;

        /// <summary>
        /// Service constructor
        /// </summary>
        public RobotArmDashboardService(DsspServiceCreationPort creationPort)
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
            var runWindow = _wpfServicePort.RunWindow(() => new MainWindow {ViewModel = new MainViewModel(this)});
            yield return (Choice) runWindow;
            var exception = (Exception) runWindow;
            if (exception != null)
            {
                LogError(exception);
                StartFailed();
                yield break;
            }
            _mainWindow = (Window) runWindow as MainWindow;

            // subscribe to Articulated Arm notifications
            var subscribe = _armPort.Subscribe(_armNotify);
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

            //TODO:
            MainPortInterleave.CombineWith(
                new Interleave());

            SpawnIterator(StartCompleted);
        }

        private IEnumerator<ITask> StartCompleted()
        {
            //TODO: update arm positions
            yield break;
        }

        /// <summary>
        /// Handles Subscribe messages
        /// </summary>
        /// <param name="subscribe">the subscribe request</param>
        [ServiceHandler]
        public void SubscribeHandler(Subscribe subscribe)
        {
            SubscribeHelper(_submgrPort, subscribe.Body, subscribe.ResponsePort);
        }
    }
}


