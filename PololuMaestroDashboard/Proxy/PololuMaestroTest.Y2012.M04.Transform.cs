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
[assembly: global::System.Reflection.AssemblyProductAttribute("PololuMaestroTest")]
[assembly: global::System.Reflection.AssemblyTitleAttribute("PololuMaestroTest")]
[assembly: global::Microsoft.Dss.Core.Attributes.ServiceDeclarationAttribute(global::Microsoft.Dss.Core.Attributes.DssServiceDeclaration.Transform, SourceAssemblyKey="PololuMaestroTest.Y2012.M04, Version=1.0.0.0, Culture=neutral, PublicKeyToken=d6f" +
    "1900a66fa3281")]
[assembly: global::System.Security.SecurityTransparentAttribute()]
[assembly: global::System.Security.SecurityRulesAttribute(global::System.Security.SecurityRuleSet.Level1)]

namespace Dss.Transforms.TransformPololuMaestroTest {
    
    
    public class Transforms : global::Microsoft.Dss.Core.Transforms.TransformBase {
        
        static Transforms() {
            Register();
        }
        
        public static void Register() {
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::PololuMaestroTest.Proxy.PololuMaestroTestState), new global::Microsoft.Dss.Core.Attributes.Transform(PololuMaestroTest_Proxy_PololuMaestroTestState_TO_PololuMaestroTest_PololuMaestroTestState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::PololuMaestroTest.PololuMaestroTestState), new global::Microsoft.Dss.Core.Attributes.Transform(PololuMaestroTest_PololuMaestroTestState_TO_PololuMaestroTest_Proxy_PololuMaestroTestState));
        }
        
        private static global::PololuMaestroTest.Proxy.PololuMaestroTestState _cachedInstance0 = new global::PololuMaestroTest.Proxy.PololuMaestroTestState();
        
        private static global::PololuMaestroTest.PololuMaestroTestState _cachedInstance = new global::PololuMaestroTest.PololuMaestroTestState();
        
        public static object PololuMaestroTest_Proxy_PololuMaestroTestState_TO_PololuMaestroTest_PololuMaestroTestState(object transformFrom) {
            return _cachedInstance;
        }
        
        public static object PololuMaestroTest_PololuMaestroTestState_TO_PololuMaestroTest_Proxy_PololuMaestroTestState(object transformFrom) {
            return _cachedInstance0;
        }
    }
}
