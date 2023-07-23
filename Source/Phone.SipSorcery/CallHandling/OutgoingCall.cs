using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioBrix.SipSorcery;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery.CallHandling
{
    internal class OutgoingCall : Call
    {
        internal OutgoingCall(SIPUserAgent ua, CallPartyDescriptor callParty, AudioBrixEndpoint audio) : base(ua, CallDirection.Out, callParty)
        {
            Audio = audio;

            UserAgent.ClientCallAnswered += UserAgent_ClientCallAnswered;
            UserAgent.ClientCallRinging += UserAgent_ClientCallRinging;
            UserAgent.ClientCallFailed += UserAgent_ClientCallFailed;
            UserAgent.ClientCallTrying += UserAgent_ClientCallTrying;
        }

        private void UserAgent_ClientCallTrying(ISIPClientUserAgent uac, SIPSorcery.SIP.SIPResponse sipResponse)
        {
            State = CallState.Trying;
        }

        private void UserAgent_ClientCallFailed(ISIPClientUserAgent uac, string errorMessage, SIPSorcery.SIP.SIPResponse sipResponse)
        {
            State = CallState.Failed;
        }

        private void UserAgent_ClientCallRinging(ISIPClientUserAgent uac, SIPSorcery.SIP.SIPResponse sipResponse)
        {
            State = CallState.Ringing;
        }

        private void UserAgent_ClientCallAnswered(ISIPClientUserAgent uac, SIPSorcery.SIP.SIPResponse sipResponse)
        {
            State = CallState.Established;
        }
    }
}
