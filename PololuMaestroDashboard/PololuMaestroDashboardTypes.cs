using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using W3C.Soap;

namespace PololuMaestro.Dashboard
{
    /// <summary>
    /// PololuMaestroTest contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// DSS contract identifer for PololuMaestroTest
        /// </summary>
        [DataMember]
        public const string Identifier = "http://schemas.tempuri.org/2012/04/pololumaestrodashboard.html";
    }

    /// <summary>
    /// PololuMaestroTest state
    /// </summary>
    [DataContract]
    public class PololuMaestroDashboardState
    {
    }

    /// <summary>
    /// PololuMaestroTest main operations port
    /// </summary>
    [ServicePort]
    public class PololuMaestroDashboardOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop, Get>
    {
    }

    /// <summary>
    /// PololuMaestroTest get operation
    /// </summary>
    public class Get : Get<GetRequestType, PortSet<PololuMaestroDashboardState, Fault>>
    {
        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        public Get()
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        public Get(GetRequestType body)
            : base(body)
        {
        }

        /// <summary>
        /// Creates a new instance of Get
        /// </summary>
        /// <param name="body">the request message body</param>
        /// <param name="responsePort">the response port for the request</param>
        public Get(GetRequestType body, PortSet<PololuMaestroDashboardState, Fault> responsePort)
            : base(body, responsePort)
        {
        }
    }
}


