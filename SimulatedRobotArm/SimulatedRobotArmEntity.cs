using System;
using System.Collections.Generic;
using Kobush.RobotArm.Common;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

namespace Kobush.RobotArm.Simulation
{
    [DataContract]
    public class SimulatedRobotArmEntity : VisualEntity
    {
        // base
        public const float BaseHeight = 0.004f + 0.004f + 0.045f;
        public const float BaseRadius = 0.095f/2f;
        // top servo base
        private const float TopRadius = 0.090f/2f;
        public const float TopGap = 0.006f;
        private const float TopHeight = 0.037f;
        // lower arm
        public static readonly Vector3 TopLowerJointOffset = new Vector3(-0.012f, 0.0203f, 0.0104f);
        private const float LowerHeight = 0.145f;
        private const float LowerWidth = 0.047f + 0.004f*2f;
        private const float LowerDepth = 0.024f;
        //upper arm
        private const float UpperHeight = 0.180f;
        private const float UpperWidth = 0.047f - 0.0002f;
        private const float UpperDepth = 0.028f;
        private const float UpperBottomJointOffset = 0.039f;
        private static readonly Vector3 LowerUpperJointOffset = new Vector3(0, LowerHeight - LowerDepth, 0);
        //wrist
        public const float WristHeight = 0.011f + 0.052f;
        private const float WristWidth = 0.047f + 0.004f*2f;
        private const float WristDepth = 0.022f;
        //gripper
        public const float GripperHeight = 0.0925f;
        private const float GripperWidth = 0.0485f;
        private const float GripperDepth = 0.0016f;

        // This class holds a description of each of the joints in the arm.
        class JointDesc
        {
            public readonly string Name;
            public readonly float Min;  // minimum allowable angle
            public float Max;  // maximum allowable angle

            private readonly VisualEntity _parentEntity;
            private readonly bool _useNormal;

            public PhysicsJoint Joint; // Phyics Joint
            public PhysicsJoint Joint2; // Alternate Physics Joint (used for gripper)
            public float Target;  // Target joint position
            public float Current;  // Current joint position
            public float Speed;  // Rate of moving toward the target position
            public JointDesc(string name, float min, float max, VisualEntity parentEntity, bool useNormal)
            {
                Name = name; Min = min; Max = max;
                _parentEntity = parentEntity;
                _useNormal = useNormal;

                Joint = null;
                Joint2 = null;
                Current = Target = 0;
                Speed = 30;
            }

            // Returns true if the specified target is within the valid bounds
            public bool ValidTarget(float target)
            {
                return ((target >= Min) && (target <= Max));
            }

            // Returns true if the joint is not yet at the target position
            public bool NeedToMove(float epsilon)
            {
                if (Joint == null) return false;
                return (Math.Abs(Target - Current) > epsilon);
            }

            // Takes one step toward the target position based on the specified time
            public void UpdateCurrent(double time)
            {
                float delta = (float)(time * Speed);
                if (Target > Current)
                    Current = Math.Min(Current + delta, Target);
                else
                    Current = Math.Max(Current - delta, Target);
            }

            public bool Update(double prevTime, float epsilon)
            {
                if (Joint == null)
                    Joint = (PhysicsJoint)_parentEntity.ParentJoint;

                if (!NeedToMove(epsilon))
                    return false;


                UpdateCurrent(prevTime);

                Vector3 axis = _useNormal ? Joint.State.Connectors[0].JointNormal : Joint.State.Connectors[0].JointAxis;
                Joint.SetAngularDriveOrientation(
                    Quaternion.FromAxisAngle(axis.X, axis.Y, axis.Z, Conversions.DegreesToRadians(Current)));

                return true;
            }
        }

        // Initialize an array of descriptions for each joint in the arm
        List<JointDesc> _joints = new List<JointDesc>();
/*
                                  {
                                      new JointDesc("Base", -90, 90),
                                      new JointDesc("Shoulder", -90, 54),
                                      new JointDesc("Elbow", -155, 155),
                                      new JointDesc("Wrist", -90, 90),
                                      new JointDesc("WristRotate", -90, 90),
                                      new JointDesc("Gripper", 0, 2)
                                  };
*/

        private SingleShapeEntity _baseEntity;
        private SingleShapeSegmentEntity _topEntity;
        private SingleShapeSegmentEntity _lowerEntity;
        private SingleShapeSegmentEntity _upperEntity;
        private SingleShapeSegmentEntity _wristEntity;
        private SingleShapeSegmentEntity _gripperEntity;

        // default construcotr used for deserialization
        public SimulatedRobotArmEntity()
        {}

        // initialize constructor
        public SimulatedRobotArmEntity(string name, Vector3 position)
        {
            // set default states
            State.Name = name;
            State.Pose.Position = position;
            State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
        }

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            /*base.CreateAndInsertPhysicsEntity(physicsEngine);
            base.PhysicsEntity.SolverIterationCount = 128;*/

            var position = new Vector3();
            AddBase(ref position);
            AddTop(ref position);
            AddLowerArm(ref position);
            AddUpperArm(ref position);
            AddWrist(ref position);
            AddGripper(ref position);

            base.Initialize(device, physicsEngine);
        }

        private void AddBase(ref Vector3 position)
        {
            var baseShape = new BoxShape(new BoxShapeProperties(
                                             "base", // name
                                             150, // mass kg
                                             new Pose(new Vector3(0, BaseHeight/2, 0), new Quaternion(0, 0, 0, 1)),
                                             new Vector3(BaseRadius*2, BaseHeight, BaseRadius*2)
                                             ));

            _baseEntity = new SingleShapeEntity(baseShape, position);
            _baseEntity.State.Name = State.Name + " Base";
            _baseEntity.State.Flags |= EntitySimulationModifiers.Kinematic; // make the base immobile
            _baseEntity.State.Assets.Mesh = @"RobotArm_Base.obj";
            _baseEntity.Parent = this;
            _baseEntity.MeshTranslation = new Vector3(0, 0.004f, 0); // move above ground
            InsertEntity(_baseEntity);
        }

        private void AddTop(ref Vector3 position)
        {
            var topShape = new BoxShape(new BoxShapeProperties(
                                            "top", // name
                                            100, // mass
                                            new Pose(new Vector3(0, TopHeight/2, 0), new Quaternion(0, 0, 0, 1)),
                                            new Vector3(TopRadius*2, TopHeight, TopRadius*2)
                                            ));

            position += new Vector3(0, BaseHeight + TopGap, 0);

            _topEntity = new SingleShapeSegmentEntity(topShape, position);
            _topEntity.State.Name = this.State.Name + " Top";
            _topEntity.State.Assets.Mesh = @"RobotArm_Top.obj";
            _topEntity.Parent = _baseEntity;

            var topAngular = new JointAngularProperties
            {
                TwistMode = JointDOFMode.Limited,
                TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000),
                UpperTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(90.1f), 0, new SpringProperties()),
                LowerTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(-90.1f), 0, new SpringProperties()),
            };
            var topConnector = new[]
            {
                new EntityJointConnector(_topEntity, new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 0, 0)),
                new EntityJointConnector(_baseEntity, new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, BaseHeight + TopGap, 0))
            };

            _topEntity.CustomJoint = new Joint();
            _topEntity.CustomJoint.State = new JointProperties(topAngular, topConnector);
            _topEntity.CustomJoint.State.Name = "BaseJoint|-90|90|";

            // add as child of base entity
            _baseEntity.InsertEntity(_topEntity);

            _joints.Add(new JointDesc(_topEntity.CustomJoint.State.Name, 
                Conversions.RadiansToDegrees(topAngular.LowerTwistLimit.LimitThreshold), 
                Conversions.RadiansToDegrees(topAngular.UpperTwistLimit.LimitThreshold), 
                _topEntity, true));
        }

        private void AddLowerArm(ref Vector3 position)
        {
            var lowerShape = new BoxShape(new BoxShapeProperties(
                                            "lower arm", // name
                                            100, // mass
                                            new Pose(new Vector3(0, LowerHeight / 2f - LowerDepth/2f, 0), new Quaternion(0, 0, 0, 1)),
                                            new Vector3(LowerWidth, LowerHeight, LowerDepth) // dimension
                                            ));

            position = TopLowerJointOffset;
            _lowerEntity = new SingleShapeSegmentEntity(lowerShape, position);
            _lowerEntity.State.Name = this.State.Name + " Lower Arm";
            _lowerEntity.State.Flags |= EntitySimulationModifiers.DisableCollisions;
            _lowerEntity.State.Assets.Mesh = @"RobotArm_Lower.obj";
            //_lowerEntity.MeshTranslation = new Vector3(-LowerWidth/2f, -0.012f, -LowerDepth/2f);

            var jointAngularProperties = new JointAngularProperties
            {
                TwistMode = JointDOFMode.Limited,
                TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, Conversions.DegreesToRadians(52)), 100000000),
                UpperTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(54.1f), 0, new SpringProperties()),
                LowerTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(-90.1f), 0, new SpringProperties()),
            };
            var connectors = new[]
            {
                new EntityJointConnector(_lowerEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0)),
                new EntityJointConnector(_topEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), TopLowerJointOffset)
            };
            _lowerEntity.CustomJoint = new Joint();
            _lowerEntity.CustomJoint.State = new JointProperties(jointAngularProperties, connectors);
            _lowerEntity.CustomJoint.State.Name = "Shoulder|-90|54|";

            _lowerEntity.Parent = _topEntity;
            _topEntity.InsertEntity(_lowerEntity);

            _joints.Add(new JointDesc(_lowerEntity.CustomJoint.State.Name,
                                      Conversions.RadiansToDegrees(jointAngularProperties.LowerTwistLimit.LimitThreshold),
                                      Conversions.RadiansToDegrees(jointAngularProperties.UpperTwistLimit.LimitThreshold),
                                      _lowerEntity, false));
        }

        private void AddUpperArm(ref Vector3 position)
        {
            var upperShape = new BoxShape(new BoxShapeProperties(
                                              "upper", // name
                                              100, // mass
                                              new Pose(new Vector3(0, UpperHeight/2f - UpperBottomJointOffset, 0), new Quaternion(0, 0, 0, 1)),
                                              new Vector3(UpperWidth, UpperHeight, UpperDepth) // dimension
                                              ));

            position = LowerUpperJointOffset;
            _upperEntity = new SingleShapeSegmentEntity(upperShape, position);
            _upperEntity.State.Name = this.State.Name + " Upper";
            //_upperEntity.State.Flags |= EntitySimulationModifiers.DisableCollisions;
            _upperEntity.State.Assets.Mesh = @"RobotArm_Upper.obj";

            var jointAngularProperties = new JointAngularProperties
            {
                TwistMode = JointDOFMode.Limited,
                TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000),
                UpperTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(155), 0, new SpringProperties()),
                LowerTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(-155), 0, new SpringProperties()),
            };
            var connectors = new[]
            {
                new EntityJointConnector(_upperEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0)),
                new EntityJointConnector(_lowerEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), LowerUpperJointOffset)
            };
            _upperEntity.CustomJoint = new Joint();
            _upperEntity.CustomJoint.State = new JointProperties(jointAngularProperties, connectors);
            _upperEntity.CustomJoint.State.Name = "Elbow|-155|155|";

            _upperEntity.Parent = _lowerEntity;
            _lowerEntity.InsertEntity(_upperEntity);

            _joints.Add(new JointDesc(_upperEntity.CustomJoint.State.Name,
                                      Conversions.RadiansToDegrees(jointAngularProperties.LowerTwistLimit.LimitThreshold),
                                      Conversions.RadiansToDegrees(jointAngularProperties.UpperTwistLimit.LimitThreshold),
                                      _upperEntity, false));
        }

        private void AddWrist(ref Vector3 position)
        {
            var upperShape = new BoxShape(new BoxShapeProperties(
                                              "wrist", // name
                                              100, // mass
                                              new Pose(new Vector3(0, WristHeight / 2f - WristDepth/2f, 0), new Quaternion(0, 0, 0, 1)),
                                              new Vector3(WristWidth, WristHeight, WristDepth) // dimension
                                              ));

            position = new Vector3(0,0.122f,0);
            _wristEntity = new SingleShapeSegmentEntity(upperShape, position);
            _wristEntity.State.Name = this.State.Name + " Wrist";
            _wristEntity.State.Flags |= EntitySimulationModifiers.DisableCollisions;
            _wristEntity.State.Assets.Mesh = @"RobotArm_Wrist.obj";

            var jointAngularProperties = new JointAngularProperties
            {
                TwistMode = JointDOFMode.Limited,
                TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000),
                UpperTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(90), 0, new SpringProperties()),
                LowerTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(-90), 0, new SpringProperties()),
            };
            var connectors = new[]
            {
                new EntityJointConnector(_wristEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 0)),
                new EntityJointConnector(_upperEntity, new Vector3(0, 1, 0), new Vector3(1, 0, 0), position)
            };
            _wristEntity.CustomJoint = new Joint();
            _wristEntity.CustomJoint.State = new JointProperties(jointAngularProperties, connectors);
            _wristEntity.CustomJoint.State.Name = "Wrist|-90|90|";  //-126|126 - to collision

            _wristEntity.Parent = _upperEntity;
            _upperEntity.InsertEntity(_wristEntity);

            _joints.Add(new JointDesc(_wristEntity.CustomJoint.State.Name,
                                      Conversions.RadiansToDegrees(jointAngularProperties.LowerTwistLimit.LimitThreshold),
                                      Conversions.RadiansToDegrees(jointAngularProperties.UpperTwistLimit.LimitThreshold),
                                      _wristEntity, false));

        }

        private void AddGripper(ref Vector3 position)
        {
            var upperShape = new BoxShape(new BoxShapeProperties(
                                              "wrist", // name
                                              100, // mass
                                              new Pose(new Vector3(0, GripperHeight / 2f, 0), new Quaternion(0, 0, 0, 1)),
                                              new Vector3(GripperWidth, GripperHeight, GripperDepth) // dimension
                                              ));

            position = new Vector3(0, WristHeight-WristDepth/2f+0.009f, 0);
            _gripperEntity = new SingleShapeSegmentEntity(upperShape, position);
            _gripperEntity.State.Name = this.State.Name + " Gripper";
            _gripperEntity.State.Flags |= EntitySimulationModifiers.DisableCollisions;
            _gripperEntity.State.Assets.Mesh = @"RobotArm_Gripper.obj";

            var jointAngularProperties = new JointAngularProperties
            {
                TwistMode = JointDOFMode.Limited,
                TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000),
                UpperTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(90), 0, new SpringProperties()),
                LowerTwistLimit = new JointLimitProperties(Conversions.DegreesToRadians(-90), 0, new SpringProperties()),
            };
            var connectors = new[]
            {
                new EntityJointConnector(_gripperEntity, new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0)),
                new EntityJointConnector(_wristEntity, new Vector3(1, 0, 0), new Vector3(0, 1, 0), position)
            };
            _gripperEntity.CustomJoint = new Joint();
            _gripperEntity.CustomJoint.State = new JointProperties(jointAngularProperties, connectors);
            _gripperEntity.CustomJoint.State.Name = "WristRotate|-90|90|";

            _gripperEntity.Parent = _wristEntity;
            _wristEntity.InsertEntity(_gripperEntity);

            _joints.Add(new JointDesc(_gripperEntity.CustomJoint.State.Name,
                                      Conversions.RadiansToDegrees(jointAngularProperties.LowerTwistLimit.LimitThreshold),
                                      Conversions.RadiansToDegrees(jointAngularProperties.UpperTwistLimit.LimitThreshold),
                                      _gripperEntity, true));

            //TODO: temporary
            _joints.Add(new JointDesc("Grip", 0, 2f, _gripperEntity, false));

        }

        // These variables are used to keep track of the state of the arm while it is moving
        bool _moveToActive = false;
        const float _epsilon = 0.01f;
        SuccessFailurePort _moveToResponsePort = null;
        private double _prevTime;

        // This is the basic method used to move the arm.  The target position for each joint is specified along
        // with a time for the movement to be completed.  A port is returned which will receive a success message when the 
        // movement is completed or an exception message if an error is encountered.
        public SuccessFailurePort MoveTo(
            float baseVal,
            float shoulderVal,
            float elbowVal,
            float wristVal,
            float rotateVal,
            float gripperVal,
            float time)
        {
            var responsePort = new SuccessFailurePort();

            if (_moveToActive)
            {
                responsePort.Post(new Exception("Previous MoveTo still active."));
                return responsePort;
            }

            var values = new[] { baseVal, shoulderVal, elbowVal, wristVal, rotateVal, gripperVal};

            // check bounds.  If the target is invalid, post an exception message to the response port with a helpful error.
            for (int i = 0; i < _joints.Count; i++)
            {
                var val = values[i];
                if (!_joints[i].ValidTarget(val))
                {
                    responsePort.Post(new Exception(_joints[i].Name + "Joint set to invalid value: " + val));
                    return responsePort;
                }               
            }

/*
            if ((_joints[5].Target > gripperVal) && (Payload == null))
            {
                _attachPayload = true;
            }
            else if ((_joints[5].Target < gripperVal) && (Payload != null))
            {
                _dropPayload = true;
            }
*/

            // set the target values on the joint descriptors
            for (int i = 0; i < _joints.Count; i++)
                _joints[i].Target = values[i];

            // calculate a speed value for each joint that will cause it to complete its motion in the specified time
            for (int i = 0; i < _joints.Count; i++)
                _joints[i].Speed = Math.Abs(_joints[i].Target - _joints[i].Current) / time;

            // set this flag so that the motion is evaluated in the update method
            _moveToActive = true;

            // keep a pointer to the response port so we can post a result message to it.
            _moveToResponsePort = responsePort;

            return responsePort;
        }

        public override void Update(FrameUpdate update)
        {
            base.Update(update);

            if (_moveToActive)
            {
                bool done = true;

                // Check each joint and update it if necessary.
                for (int i = 0; i < _joints.Count; i++)
                {
                    if (_joints[i].Update(_prevTime, _epsilon))
                        done = false;
                }

                /*// gripper is special case
                if (_joints[5].NeedToMove(_epsilon / 100f))
                {
                    done = false;
                    _joints[5].UpdateCurrent(_prevTime);
                    float jointValue = _joints[5].Current / 2;
                    if (_joints[5].Joint != null)
                        _joints[5].Joint.SetLinearDrivePosition(new Vector3(-jointValue, 0, 0));
                    if (_joints[5].Joint2 != null)
                        _joints[5].Joint2.SetLinearDrivePosition(new Vector3(jointValue, 0, 0));
                }
*/
                if (done)
                {
                    // move completed; send the completion message
                    _moveToActive = false;
                    _moveToResponsePort.Post(new SuccessResult());
                }
            }

            _prevTime = update.ElapsedTime;
        }

        // Get the Target joint angles but put them into separate variables
        public void GetTargetJointAngles(
            out float baseVal, 
            out float shoulderVal, 
            out float elbowVal, 
            out float wristVal, 
            out float rotateVal, 
            out float gripperVal)
        {
            // Get the target values on the joint descriptors
            // NOTE: This assumes that the arm is already there!
            baseVal = _joints[0].Target;
            shoulderVal = _joints[1].Target;
            elbowVal = _joints[2].Target;
            wristVal = _joints[3].Target;
            rotateVal = _joints[4].Target;
            gripperVal = _joints[5].Target;
        }
    }
}