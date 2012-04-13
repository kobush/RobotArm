using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel.Proxy;
using W3C.Soap;

using submgr = Microsoft.Dss.Services.SubscriptionManager;
using pololuproxy = PololuMaestro.Proxy;
using armproxy = Microsoft.Robotics.Services.ArticulatedArm.Proxy;

namespace RobotArm
{
    [Contract(Contract.Identifier)]
    [DisplayName("RobotArm")]
    [Description("RobotArm service (no description provided)")]
    [AlternateContract(armproxy.Contract.Identifier)]
    class RobotArmService : DsspServiceBase
    {
        /// <summary>
        /// Service state
        /// </summary>
        [ServiceState]
        RobotArmState _state = new RobotArmState();

        /// <summary>
        /// Main service port
        /// </summary>
        [ServicePort("/RobotArm", AllowMultipleInstances = true)]
        RobotArmOperations _mainPort = new RobotArmOperations();

        [SubscriptionManagerPartner]
        submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// PololuMaestroBoard partner
        /// </summary>
        [Partner("PololuMaestroBoard", Contract = pololuproxy.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        pololuproxy.PololuMaestroOperations _pololuPort = new pololuproxy.PololuMaestroOperations();
        pololuproxy.PololuMaestroOperations _pololuNotify = new pololuproxy.PololuMaestroOperations();

        /// <summary>
        /// Alternate service port
        /// </summary>
        [AlternateServicePort(AlternateContract = armproxy.Contract.Identifier)]
        armproxy.ArticulatedArmOperations _armPort = new armproxy.ArticulatedArmOperations();

        private readonly Dictionary<string,JointState> _jointLookup = new Dictionary<string, JointState>();

        /// <summary>
        /// Service constructor
        /// </summary>
        public RobotArmService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        /// <summary>
        /// Service start
        /// </summary>
        protected override void Start()
        {
            InitializeArmState();

            ActivateDsspOperationHandlers()
                .CombineWith(
                    new Interleave(
                        new TeardownReceiverGroup(
                                Arbiter.Receive<DsspDefaultDrop>(false, _mainPort, DefaultDropHandler)
                            ), 
                            new ExclusiveReceiverGroup(), 
                            new ConcurrentReceiverGroup()
                        )
                );

            // Publish the service to the local Node Directory
            DirectoryInsert();

            // display HTTP service Uri
            LogInfo(LogGroups.Activation, "Service uri: " + base.FindServiceAliasFromScheme(Uri.UriSchemeHttp));
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

        /// <summary>
        /// Handles Subscribe requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="subscribe">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void ArmSubscribeHandler(armproxy.Subscribe subscribe)
        {
            SubscribeHelper(_submgrPort, subscribe.Body, subscribe.ResponsePort);
        }

        /// <summary>
        /// Handles Get requests on main port
        /// </summary>
        /// <param name="get">request message</param>
        [ServiceHandler]
        public IEnumerator<ITask> GetHandler(Get get)
        {
            yield return Arbiter.Choice(
                    UpdateState(),
                    success => get.ResponsePort.Post(_state),
                    ex => get.ResponsePort.Post(Fault.FromException(ex))
                );
        }

        /// <summary>
        /// Handles Get requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="get">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public IEnumerator<ITask> ArmGetHandler(armproxy.Get get)
        {
            yield return Arbiter.Choice(
                    UpdateState(),
                    success => get.ResponsePort.Post((armproxy.ArticulatedArmState) (_state.Clone())),
                    ex => get.ResponsePort.Post(Fault.FromException(ex))
                );
        }

        /// <summary>
        /// Handles SetEndEffectorPose requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="setendeffectorpose">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void SetEndEffectorPoseHandler(armproxy.SetEndEffectorPose update)
        {
            Pose pose = update.Body.EndEffectorPose;
            Quaternion or = pose.Orientation;
            Vector3 pos = pose.Position;
        }

        /// <summary>
        /// Handles GetEndEffectorPose requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="getendeffectorpose">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void GetEndEffectorPoseHandler(armproxy.GetEndEffectorPose getendeffectorpose)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handles SetJointTargetPose requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="update">request message</param>
        /// <remarks>Sets the pose of a single joint (in most cases this is just an angle)</remarks>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void SetJointTargetPoseHandler(armproxy.SetJointTargetPose update)
        {
            string name = update.Body.JointName;
            JointState j = _jointLookup[name];

            update.Body.TargetOrientation;
            update.Body.TargetPosition;

        }

        /// <summary>
        /// Handles SetJointTargetVelocity requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="setjointtargetvelocity">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void SetJointTargetVelocityHandler(armproxy.SetJointTargetVelocity update)
        {
            update.Body.JointName;
            update.Body.TargetVelocity;
        }

        /// <summary>
        /// Handles ReliableSubscribe requests on alternate port ArticulatedArm
        /// </summary>
        /// <param name="reliablesubscribe">request message</param>
        [ServiceHandler(PortFieldName = "_armPort")]
        public void ArmReliableSubscribeHandler(armproxy.ReliableSubscribe reliablesubscribe)
        {
            throw new NotImplementedException();
        }

        private SuccessFailurePort UpdateState()
        {
            var resultPort = new SuccessFailurePort();
            //TODO: read state from service
            return resultPort;
        }
    }
}


