using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phone.SipSorcery.CallMachine.Core
{
    internal class CallJob
    {
        public string Uri { get; set; } = string.Empty;

        public string WaveFile { get; set; } = string.Empty;
    }
}
