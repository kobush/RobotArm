using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Adapters.WinForms;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;
using W3C.Soap;
using xna = Microsoft.Xna.Framework;
using armproxy = Microsoft.Robotics.Services.ArticulatedArm.Proxy;
using engineproxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using submgr = Microsoft.Dss.Services.SubscriptionManager;

namespace Kobush.Simulation.RobotArm
{
	[Contract(Contract.Identifier)]
    [DisplayName("SimulatedRobotArm")]
	[Description("Simulation of custom robotic arm")]
	public class SimulatedRobotArm : DsspServiceBase
	{
	    private const string RobotArmEntityName = "RobotArm";

	    #region Simulation Variables
        SimulationEnginePort _simEngine;
	    SimulationEnginePort _notificationTarget;
	    
        // entities
        private SimulatedRobotArmEntity _robotArm;
	    private SingleShapeEntity _moveTargetEntity;

	    #endregion

		[ServiceState] 
        readonly RobotArmState _state = new RobotArmState();

        [Partner("Engine", 
            Contract = engineproxy.Contract.Identifier, 
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private engineproxy.SimulationEnginePort _engineStub = new engineproxy.SimulationEnginePort();

        [ServicePort("/SimulatedRobotArm", 
            AllowMultipleInstances = true)]
		RobotArmOperations _mainPort = new RobotArmOperations();

        [Partner("SubMgr", 
            Contract = submgr.Contract.Identifier, 
            CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        private readonly submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        [AlternateServicePort("/arm", 
            AlternateContract = armproxy.Contract.Identifier)]
        private readonly armproxy.ArticulatedArmOperations _armPort = new armproxy.ArticulatedArmOperations();

        // This port receives events from the user interface
	    private readonly FromWinformEvents _fromWinformPort = new FromWinformEvents();

        SimulatedRobotArmForm _form = null;

		public SimulatedRobotArm(DsspServiceCreationPort creationPort)
			: base(creationPort)
		{
		}
		
		protected override void Start()
		{
		    _simEngine = SimulationEngine.GlobalInstancePort;
		    _notificationTarget = new SimulationEnginePort();

		    // request a notification when the arm is inserted into the sim engine
		    var esrt = new EntitySubscribeRequestType {Name = RobotArmEntityName};
		    _simEngine.Subscribe(esrt, _notificationTarget);

		    base.Start();

		    // Add the winform message handler to the interleave
		    MainPortInterleave.CombineWith(
		        new Interleave(
		            new TeardownReceiverGroup(),
		            new ExclusiveReceiverGroup
                        (
		                    Arbiter.Receive<InsertSimulationEntity>(false, _notificationTarget, InsertEntityNotificationHandlerFirstTime),
                            Arbiter.ReceiveWithIterator<FromWinformMsg>(true, _fromWinformPort, OnWinformMessageHandler)
                        ),
		            new ConcurrentReceiverGroup()
                ));

            // Set the initial viewpoint
            SetupCamera();
            
            // Set up the world
            PopulateWorld();
		}

	    private void InsertEntityNotificationHandlerFirstTime(InsertSimulationEntity ins)
	    {
            InsertEntityNotificationHandler(ins);

            // Create the user interface form
            WinFormsServicePort.Post(new RunForm(CreateForm));

            // Listen on the main port for requests and call the appropriate handler.
            MainPortInterleave.CombineWith(
                new Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive<InsertSimulationEntity>(true, _notificationTarget, InsertEntityNotificationHandler),
                        Arbiter.Receive<DeleteSimulationEntity>(true, _notificationTarget, DeleteEntityNotificationHandler),
                        Arbiter.ReceiveWithIterator<armproxy.SetEndEffectorPose>(true, _armPort, SetEndEffectorHandler)
                        /*Arbiter.ReceiveWithIterator<armproxy.SetJointTargetPose>(true, _armPort, SetJointPoseHandler),
                        Arbiter.ReceiveWithIterator<armproxy.SetJointTargetVelocity>(true, _armPort, SetJointVelocityHandler)*/
                    ),
                    new ConcurrentReceiverGroup(
                        /*Arbiter.ReceiveWithIterator<armproxy.Subscribe>(true, _armPort, SubscribeHandler),
                        Arbiter.ReceiveWithIterator<armproxy.GetEndEffectorPose>(true, _armPort, GetEndEffectorHandler),
                        Arbiter.Receive<DsspDefaultLookup>(true, _armPort, DefaultLookupHandler),
                        Arbiter.ReceiveWithIterator<armproxy.Get>(true, _armPort, ArmGetHandler)*/
                    )
                )
            );
	    }

	    private void InsertEntityNotificationHandler(InsertSimulationEntity ins)
	    {
            _robotArm = (SimulatedRobotArmEntity)ins.Body;
            _robotArm.ServiceContract = Contract.Identifier;

	    }

	    private void DeleteEntityNotificationHandler(DeleteSimulationEntity parameter0)
	    {
	        _robotArm = null;
	    }

        #region Windows form methods

        // Create the UI form
        System.Windows.Forms.Form CreateForm()
        {
            return new SimulatedRobotArmForm(_fromWinformPort);
        }

        // process messages from the UI Form
        IEnumerator<ITask> OnWinformMessageHandler(FromWinformMsg msg)
        {
            switch (msg.Command)
            {
                case FromWinformMsg.MsgEnum.Loaded:
                    // the windows form is ready to go
                    _form = (SimulatedRobotArmForm)msg.Object;
                    break;

                case FromWinformMsg.MsgEnum.MoveToPosition:
                    {
                        // move the arm to the specified position
                        MoveToPositionParameters moveParams = (MoveToPositionParameters)msg.Object;
                        yield return Arbiter.Choice(
                            MoveToPosition(moveParams.X, moveParams.Y, moveParams.Z,
                            moveParams.GripAngle, moveParams.GripRotation, moveParams.Grip, moveParams.Time),
                            delegate(SuccessResult s) { },
                            delegate(Exception e)
                            {
                                WinFormsServicePort.FormInvoke(() => _form.SetErrorText(e.Message));
                            }
                        );

                        break;
                    }
                case FromWinformMsg.MsgEnum.MoveTo:
                    {
                        var moveParams = (MoveToParameters) msg.Object;
                        yield return Arbiter.Choice(
                            MoveTo(moveParams.BaseAngle, moveParams.ShoulderAngle, moveParams.ElbowAngle,
                            moveParams.GripAngle, moveParams.GripRotation, moveParams.Grip, moveParams.Time),
                            delegate(SuccessResult s) { },
                            delegate(Exception e)
                            {
                                WinFormsServicePort.FormInvoke(() => _form.SetErrorText(e.Message));
                            }
                        );
                    }
                    break;

                /*case FromWinformMsg.MsgEnum.Reset:
                    _moveSequenceActive = false;    // terminate sequence
                    ResetDominos();
                    ResetArmPosition();
                    ResetBlocks();
                    break;

                case FromWinformMsg.MsgEnum.ReverseDominos:
                    SpawnIterator(ReverseDominos);
                    break;

                case FromWinformMsg.MsgEnum.Park:
                    SpawnIterator(ParkArm);
                    break;

                case FromWinformMsg.MsgEnum.ToppleDominos:
                    SpawnIterator(ToppleDominos);
                    break;

                case FromWinformMsg.MsgEnum.RandomMove:
                    SpawnIterator(RandomMove);
                    break;*/
            }
            yield break;
        }
        #endregion

	    private void PopulateWorld()
        {
            AddSky();
            AddGround();
     //       AddBlocks();

            // Add an overhead camera
       //     AddCamera();

            // Create and place the dominos
         //   SpawnIterator(CreateDominos);

            // Create a LynxL6Arm Entity positioned at the origin
            var robotArm = new SimulatedRobotArmEntity(RobotArmEntityName, new Vector3(0, 0, 0));
            SimulationEngine.GlobalInstancePort.Insert(robotArm);

	        var targetProps = new SphereShapeProperties(0, new Pose(), 0.0025f);
	        var shape = new SphereShape(targetProps);
	        shape.State.DiffuseColor = new Vector4(0.1f, 0f, 1f, 1f);
            _moveTargetEntity = new SingleShapeEntity(shape, new Vector3(0f, 0.2f, 0.1f));
	        _moveTargetEntity.State.Name = "Move To Target";
	        SimulationEngine.GlobalInstancePort.Insert(_moveTargetEntity);
        }

        void AddSky()
        {
            // Add a sky using a static texture. We will use the sky texture
            // to do per pixel lighting on each simulation visual entity
            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            // Add a directional light to simulate the sun.
            LightSourceEntity sun = new LightSourceEntity();
            sun.State.Name = "Sun";
            sun.Type = LightSourceEntityType.Directional;
            sun.Color = new Vector4(0.7f, 0.7f, 0.7f, 1);
            sun.Direction = new Vector3(0.5f, -.75f, 0.5f);
            SimulationEngine.GlobalInstancePort.Insert(sun);
        }

        void AddGround()
        {
            // create a large horizontal plane, at zero elevation.
            HeightFieldEntity ground = new HeightFieldEntity(
                "simple ground", // name
                "Wood_cherry.jpg", // texture image
                new MaterialProperties("ground",
                    0.2f, // restitution
                    0.5f, // dynamic friction
                    0.5f) // static friction
                );
            SimulationEngine.GlobalInstancePort.Insert(ground);
        }

        // Set up initial view
        private void SetupCamera()
        {
            var view = new CameraView();
            view.EyePosition = new Vector3(0.52f, 0.25f, 0.36f);
            view.LookAtPoint = new Vector3(-2.1f, 0.15f, -1.06f);
            SimulationEngine.GlobalInstancePort.Update(view);
        }

        public SuccessFailurePort MoveTo(
            float baseAngle,
            float shoulder,
            float elbow,
            float wrist,
            float wristRotate,
            float grip,
            float time)
        {

            var result = _robotArm.MoveTo(
                baseAngle,
                shoulder,
                elbow,
                wrist,
                wristRotate,
                grip,
                time);

            return result;
        }

        // This method calculates the joint angles necessary to place the arm into the 
        // specified position.  The arm position is specified by the X,Y,Z coordinates
        // of the gripper tip as well as the angle of the grip, the rotation of the grip, 
        // and the open distance of the grip.  The motion is completed in the 
        // specified time.
        public SuccessFailurePort MoveToPosition(
            float mx, // x position
            float my, // y position
            float mz, // z position
            float p, // angle of the grip
            float w, // rotation of the grip
            float grip, // distance the grip is open
            float time) // time to complete the movement
        {
            float baseAngle, shoulder, elbow, wrist;

            _moveTargetEntity.Position = new xna.Vector3(mx, my, mz); 

            if (!InverseKinematics(mx, my, mz, p, out baseAngle, out shoulder, out elbow, out wrist))
            {
                var s = new SuccessFailurePort();
                s.Post(new Exception("Inverse Kinematics failed"));
                return s;
            }

            // Update the form with these parameters
            WinFormsServicePort.FormInvoke(() => _form.SetPositionText(mx, my, mz, p, w, grip, time));

            // Update the form with these parameters
            WinFormsServicePort.FormInvoke(() => _form.SetJointsText(baseAngle, shoulder, elbow, wrist, w, grip, time));

            var result = _robotArm.MoveTo(
                baseAngle,
                shoulder,
                elbow,
                wrist,
                w,
                grip,
                time);

            return result;
        }

	    private static bool InverseKinematics(
            float mx, 
            float my, 
            float mz, 
            float p,
            out float baseAngle,
            out float shoulder,
            out float elbow,
            out float wrist)
	    {
            // physical attributes of the arm
	        float L1 = 0.121f; //LowerHeight - LowerDepth; // length of lower arm
	        float L2 = 0.122f; // UpperHeight - UpperBottomJointOffset; // length of lower
	        float Grip = SimulatedRobotArmEntity.GripperHeight;
	        float L3 = SimulatedRobotArmEntity.WristHeight + Grip; // wrist + gripper length
	        float H = SimulatedRobotArmEntity.BaseHeight + SimulatedRobotArmEntity.TopGap +
	                  SimulatedRobotArmEntity.TopLowerJointOffset.Y; // height from the base to first joint
	        float G = SimulatedRobotArmEntity.BaseRadius; // radius of the base

	        float r = (float) Math.Sqrt(mx*mx + mz*mz); // horizontal distance to the target
	        baseAngle = (float) Math.Atan2(mx, mz); // angle to the target

            // calculates coordinates of wrist joint (end of ulna)
	        float pRad = Conversions.DegreesToRadians(p);
	        float rb = (float) ((r - L3*Math.Cos(pRad))/(2*L1));
	        float yb = (float) ((my - H - L3*Math.Sin(pRad))/(2*L1));

	        float q = (float) (Math.Sqrt(1/(rb*rb + yb*yb) - 1));
	        float p1 = (float) (Math.Atan2(yb + q*rb, rb - q*yb)); // angle of humerus from ground
	        float p2 = (float) (Math.Atan2(yb - q*rb, rb + q*yb)); // angle of ulna from ground

	        shoulder = p1 - Conversions.DegreesToRadians(90); // angle of the shoulder joint
            elbow = p2 - shoulder - Conversions.DegreesToRadians(90); // angle of the wrist joint
	        wrist = pRad - p2; // angle of the wrist joint

            // Convert all values to degrees
            baseAngle = Conversions.RadiansToDegrees(baseAngle);
            shoulder = Conversions.RadiansToDegrees(shoulder);
            elbow = Conversions.RadiansToDegrees(elbow);
            wrist = Conversions.RadiansToDegrees(wrist);

	        // Check to make sure that the solution is valid
	        if (Single.IsNaN(baseAngle) ||
	            Single.IsNaN(shoulder) ||
	            Single.IsNaN(elbow) ||
	            Single.IsNaN(wrist))
	        {
	            // Use for debugging only!
	            Console.WriteLine("No solution to Inverse Kinematics");
	            return false;
	        }

            // solution found
            return true;
	    }

        // This method is executed if the MoveTo method fails
        void ShowError(Exception e)
        {
            //TODO: show on form
            Console.WriteLine(e.Message);
        }

        /// <summary>
        /// Set End Effector
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// <remarks>Moves the arm end effector to a specified pose</remarks>
        private IEnumerator<ITask> SetEndEffectorHandler(armproxy.SetEndEffectorPose update)
        {
            bool success = false;

            float baseAngle0, shoulder0, elbow0, wrist0, rotateVal0, gripperVal0;

            // Get the target joint angles for all joints
            // We have to do this so that we can preserve the values for the joints
            // that are NOT being changed. Note that we do not use the CURRENT joint
            // angles because this could potentially stop a motion in progress.
            _robotArm.GetTargetJointAngles(out baseAngle0, out shoulder0, out elbow0, out wrist0, 
                out rotateVal0, out gripperVal0);

            // Get all the pose information required
            // NOTE: The angle is assumed to be about the X axis of the gripper
            Quaternion q = new Quaternion(
                update.Body.EndEffectorPose.Orientation.X,
                update.Body.EndEffectorPose.Orientation.Y,
                update.Body.EndEffectorPose.Orientation.Z,
                update.Body.EndEffectorPose.Orientation.W);
            
            AxisAngle a = Quaternion.ToAxisAngle(q);
            float p = Conversions.RadiansToDegrees(a.Angle * Math.Sign(a.Axis.X));
            float x = update.Body.EndEffectorPose.Position.X;
            float y = update.Body.EndEffectorPose.Position.Y;
            float z = update.Body.EndEffectorPose.Position.Z;

            _moveTargetEntity.Position = new xna.Vector3(x, y, z); 

            float baseAngle, shoulder, elbow, wrist;

            // Use Inverse Kinematics to calculate the joint angles
            if (!InverseKinematics(x, y, z, p, out baseAngle, out shoulder, out elbow, out wrist))
            {
                // The results were not a valid pose!
                // Don't attempt the move or it might break the arm
                // Should put something in the Fault!
                Fault f = Fault.FromException(new Exception("No solution to Inverse Kinematics"));
                update.ResponsePort.Post(f);
                yield break;
            }

            // calculate the time needed to make the motion
            float maxAngle = Math.Max(Math.Abs(baseAngle0 - baseAngle), Math.Abs(shoulder0 - shoulder));
            maxAngle = Math.Max(maxAngle, Math.Abs(elbow0 - elbow));
            maxAngle = Math.Max(maxAngle, Math.Abs(wrist0 - wrist));
            float time = maxAngle * 11f / 360f;

            // Set the arm
            if (time <= 0.1f)
            {
                time = 0.1f;
            }

            yield return Arbiter.Choice(_robotArm.MoveTo(baseAngle, shoulder, elbow, wrist, rotateVal0, gripperVal0, time),
                delegate(SuccessResult s) { success = true; },
                ShowError);

            if (success)
                update.ResponsePort.Post(DefaultUpdateResponseType.Instance);
            else
            {
                // Should put something in the Fault!
                Fault f = Fault.FromException(new Exception("MoveTo failed"));
                update.ResponsePort.Post(f);
            }

            // NOT FINISHED!
            // we send a replace notification since end effector pose updates affect all joints
            var replace = new armproxy.Replace {Body = _state};
            base.SendNotification(_submgrPort, replace);

            // send an end effector update notification as well
            base.SendNotification(_submgrPort, update);

            yield break;

        }
	}
}


