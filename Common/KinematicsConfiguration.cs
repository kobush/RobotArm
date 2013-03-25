namespace Kobush.RobotArm.Common
{
    public class KinematicsConfiguration
    {
        /// <summary>
        /// Height from the base to first joint
        /// </summary>
        public float BaseHeight = 0.053f + 0.006f + 0.0203f;

        public float BaseRadius = 0.095f / 2f;

        public float LowerArmLength = 0.121f;
        public float UpperArmLength = 0.122f;
        public float GripperLength = 0.0925f;
        public float WristLength = 0.011f + 0.052f;
    }
}
