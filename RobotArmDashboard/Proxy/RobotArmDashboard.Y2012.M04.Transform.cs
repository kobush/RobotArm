//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.261
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[assembly: global::System.Reflection.AssemblyVersionAttribute("1.0.0.0")]
[assembly: global::System.Reflection.AssemblyProductAttribute("RobotArmDashboard")]
[assembly: global::System.Reflection.AssemblyTitleAttribute("RobotArmDashboard")]
[assembly: global::Microsoft.Dss.Core.Attributes.ServiceDeclarationAttribute(global::Microsoft.Dss.Core.Attributes.DssServiceDeclaration.Transform, SourceAssemblyKey="RobotArmDashboard.Y2012.M04, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d6f" +
    "1900a66fa3281")]
[assembly: global::System.Security.SecurityTransparentAttribute()]
[assembly: global::System.Security.SecurityRulesAttribute(global::System.Security.SecurityRuleSet.Level1)]

namespace Dss.Transforms.TransformRobotArmDashboard {
    
    
    public class Transforms : global::Microsoft.Dss.Core.Transforms.TransformBase {
        
        static Transforms() {
            Register();
        }
        
        public static void Register() {
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Kobush.RobotArm.Dashboard.Proxy.RobotArmDashboardState), new global::Microsoft.Dss.Core.Attributes.Transform(Kobush_RobotArm_Dashboard_Proxy_RobotArmDashboardState_TO_Kobush_RobotArm_Dashboard_RobotArmDashboardState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Kobush.RobotArm.Dashboard.RobotArmDashboardState), new global::Microsoft.Dss.Core.Attributes.Transform(Kobush_RobotArm_Dashboard_RobotArmDashboardState_TO_Kobush_RobotArm_Dashboard_Proxy_RobotArmDashboardState));
        }
        
        private static global::Kobush.RobotArm.Dashboard.Proxy.RobotArmDashboardState _cachedInstance0 = new global::Kobush.RobotArm.Dashboard.Proxy.RobotArmDashboardState();
        
        private static global::Kobush.RobotArm.Dashboard.RobotArmDashboardState _cachedInstance = new global::Kobush.RobotArm.Dashboard.RobotArmDashboardState();
        
        public static object Kobush_RobotArm_Dashboard_Proxy_RobotArmDashboardState_TO_Kobush_RobotArm_Dashboard_RobotArmDashboardState(object transformFrom) {
            return _cachedInstance;
        }
        
        public static object Kobush_RobotArm_Dashboard_RobotArmDashboardState_TO_Kobush_RobotArm_Dashboard_Proxy_RobotArmDashboardState(object transformFrom) {
            return _cachedInstance0;
        }
    }
}