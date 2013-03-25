using Microsoft.Dss.Core.Attributes;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.Simulation.Physics;

namespace Kobush.RobotArm.Simulation
{
    [DataContract]
    public class SingleShapeSegmentEntity : SingleShapeEntity
    {
        [DataMember]
        public Joint CustomJoint { get; set; }

        public SingleShapeSegmentEntity() 
        {}

        public SingleShapeSegmentEntity(Shape shape, Vector3 initialPos)
            : base(shape, initialPos)
        {}

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);

            if (CustomJoint != null)
            {
                if (ParentJoint != null)
                    PhysicsEngine.DeleteJoint((PhysicsJoint)ParentJoint);

                if (CustomJoint.State.Connectors[0].Entity == null)
                    CustomJoint.State.Connectors[0].Entity = FindConnectedEntity(CustomJoint.State.Connectors[0].EntityName);
                if (CustomJoint.State.Connectors[1].Entity == null)
                    CustomJoint.State.Connectors[1].Entity = FindConnectedEntity(CustomJoint.State.Connectors[1].EntityName);

                ParentJoint = CustomJoint;
                PhysicsEngine.InsertJoint((PhysicsJoint)ParentJoint);
            }
        }

        public override void PreSerialize()
        {
            base.PreSerialize();
            PrepareJointsForSerialization();
        }
    }
}