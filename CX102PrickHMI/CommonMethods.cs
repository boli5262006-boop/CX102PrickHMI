using S7.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
 
using thinger.CommunicationLib;
using thinger.ConfigLib;

namespace CX102PrickHMI
{
    public  class CommonMethods
    {
        public static OPCUADevice plcDevice = new OPCUADevice();

        public static OPCUA plc = new OPCUA();

        public static Dictionary<string, string> positionName = new Dictionary<string, string>();

    
    }
}
