using System;

namespace Kobush.RobotArm.Common
{
    public class KinematicsSolver
    {
        private readonly KinematicsConfiguration _config;

        public KinematicsSolver(KinematicsConfiguration config)
        {
            if (config == null) 
                throw new ArgumentNullException("config");
            
            _config = config;
        }

        public bool InverseKinematics(
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
            float L1 = _config.LowerArmLength;  // humerus
            float L2 = _config.UpperArmLength;  // ulna
            float L3 = _config.WristLength + _config.GripperLength; // hand = wrist + gripper length
            float H = _config.BaseHeight ; 

            float r = (float)Math.Sqrt(mx * mx + mz * mz); // horizontal distance to the target
            baseAngle = (float)Math.Atan2(mx, mz); // angle to the target

            // calculates coordinates of wrist joint (end of ulna)
            float pRad = Conversions.DegreesToRadians(p);
            float rb = (float) ((r - L3*Math.Cos(pRad))/(L1 + L2));
            float yb = (float) ((my - H - L3*Math.Sin(pRad))/(L1 + L2));

            float q = (float)(Math.Sqrt(1 / (rb * rb + yb * yb) - 1));
            float p1 = (float)(Math.Atan2(yb + q * rb, rb - q * yb)); // angle of humerus from ground
            float p2 = (float)(Math.Atan2(yb - q * rb, rb + q * yb)); // angle of ulna from ground

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
    }
}