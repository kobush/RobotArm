using System;

namespace Kobush.Simulation.RobotArm
{
    public static class Conversions
    {
        public static float DegreesToRadians(float degrees)
        {
            return (float) (degrees/180f*Math.PI);
        }
        
        public static double DegreesToRadians(double degrees)
        {
            return (degrees/180.0)*Math.PI;
        }

        public static float RadiansToDegrees(float radians)
        {
            return (float) (radians/Math.PI*180f);
        }
    }
}