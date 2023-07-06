using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.SIP;

namespace Phone.SipSorcery
{
    public class PhoneConfig
    {
        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? Server { get; set; }

        public bool Register { get; set; } = true;

        public SIPProtocolsEnum Protocol { get; set; }

        public int RingTimeoutSeconds { get; set; } = 30;
    }
}
