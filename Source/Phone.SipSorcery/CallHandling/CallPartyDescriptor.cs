using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.SIP;

namespace Phone.SipSorcery.CallHandling
{
    public class CallPartyDescriptor
    {
        public string? Name { get; set; }

        private SIPURI _uri;

        public string URI => _uri.ToString();

        public string User => _uri.User;

        public string Host => _uri.Host;

        public CallPartyDescriptor(string? name, SIPURI uri)
        {
            Name = name;
            _uri = uri;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return $"{Name} <{_uri}>";
            }

            return _uri.ToString();
        }
    }
}
