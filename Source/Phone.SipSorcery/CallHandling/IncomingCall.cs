using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioBrix.SipSorcery;
using SIPSorcery.Media;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery.CallHandling
{
    public class IncomingCall : Call
    {
        private SIPServerUserAgent _serverAgent;

        internal IncomingCall(SIPUserAgent ua, SIPServerUserAgent sua) 
            : base(ua, CallDirection.In)
        {
            _serverAgent = sua;

            ua.ServerCallCancelled += UaOnServerCallCancelled;
            ua.ServerCallRingTimeout += UaOnServerCallRingTimeout;
            State = CallState.Ringing;
        }

        private void UaOnServerCallRingTimeout(ISIPServerUserAgent uas)
        {
            State = CallState.Failed;
        }

        private void UaOnServerCallCancelled(ISIPServerUserAgent uas)
        {
            State = CallState.Failed;
        }

        public async Task Answer()
        {
            Audio = new AudioBrixEndpoint(new AudioEncoder());
            await UserAgent.Answer(_serverAgent, new VoIPMediaSession(Audio.ToMediaEndpoints()));
            State = CallState.Established;
        }
    }
}
