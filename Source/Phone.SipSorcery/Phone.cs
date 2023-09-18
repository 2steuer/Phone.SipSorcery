using System.Net.Sockets;
using AudioBrix.SipSorcery;
using Phone.SipSorcery.CallHandling;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery
{
    public class Phone
    {
        public event Action<object, IncomingCall>? OnIncomingCall;
        public event Action<object, RegistrationState>? OnRegistrationStateChanged; 

        private SIPTransport _transport;

        private SIPRegistrationUserAgent? _regUa = null;

        private SIPUserAgent _incomingUa;

        private PhoneConfig _cfg;

        private RegistrationState _regState = RegistrationState.Unregistered;

        private List<Call> _activeCalls = new List<Call>();

        public RegistrationState RegistrationState
        {
            get => _regState;
            private set
            {
                _regState = value;
                OnRegistrationStateChanged?.Invoke(this, value);
            }
        }

        public Phone(PhoneConfig cfg)
        {
            _cfg = cfg;
            _transport = new SIPTransport();

            _transport.AddSIPChannel(_transport.CreateChannel(cfg.Protocol, AddressFamily.InterNetwork));

            if (cfg.Register)
            {
                _regUa = new SIPRegistrationUserAgent(_transport, cfg.Username, cfg.Password, cfg.Server, 5, sendUsernameInContactHeader: true);
                _regUa.RegistrationSuccessful += RegUaOnRegistrationSuccessful;
                _regUa.RegistrationFailed += RegUaOnRegistrationFailed;
                _regUa.RegistrationTemporaryFailure += RegUaOnRegistrationFailed;
                _regUa.RegistrationRemoved += RegUaOnRegistrationRemoved;
            }

            _incomingUa = CreateIncomingUserAgent();
        }

        private void RegUaOnRegistrationRemoved(SIPURI arg1, SIPResponse arg2)
        {
            RegistrationState = RegistrationState.Unregistered;
        }

        private void RegUaOnRegistrationFailed(SIPURI arg1, SIPResponse arg2, string arg3)
        {
            RegistrationState = RegistrationState.RegistrationFailed;
        }

        private void RegUaOnRegistrationSuccessful(SIPURI arg1, SIPResponse arg2)
        {
            RegistrationState = RegistrationState.Registered;
        }

        private SIPUserAgent CreateIncomingUserAgent()
        {
            var newUa = new SIPUserAgent(_transport,
                _regUa?.OutboundProxy ?? SIPEndPoint.ParseSIPEndPoint(_cfg.Server));

            newUa.OnIncomingCall += IncomingCall;

            return newUa;
        }

        private void IncomingCall(SIPUserAgent uas, SIPRequest request)
        {
            _incomingUa.OnIncomingCall -= IncomingCall;
            _incomingUa = CreateIncomingUserAgent();

            var server = uas.AcceptCall(request);
            var ic = new IncomingCall(uas, server);
            ic.OnStateChanged += Ic_OnStateChanged;

            _activeCalls.Add(ic);

            OnIncomingCall?.Invoke(this, ic);
        }

        private void Ic_OnStateChanged(Call arg1, CallState arg2)
        {
            switch (arg2)
            {
                case CallState.Failed:
                case CallState.Ended:
                    _activeCalls.Remove(arg1);
                    break;
            }
        }

        public async Task<Call> Call(string destination)
        {
            SIPUserAgent callUA = new SIPUserAgent(_transport,
                _regUa?.OutboundProxy ?? SIPEndPoint.ParseSIPEndPoint(_cfg.Server));

            SIPURI dstUri;

            if (!SIPURI.TryParse(destination, out dstUri))
            {
                dstUri = new SIPURI(destination, _cfg.Server, string.Empty);
            }

            if (string.IsNullOrEmpty(dstUri.Host))
            {
                dstUri.Host = _cfg.Server;
            }

            string from = SIPConstants.SIP_DEFAULT_FROMURI;

            if (!string.IsNullOrEmpty(_cfg.Username))
            {
                from = new SIPURI(_cfg.Username, _cfg.Server, string.Empty).ToParameterlessString();
            }

            var cd = new SIPCallDescriptor(
                _cfg.Username,
                _cfg.Password,
                dstUri.ToString(),
                from,
                dstUri.CanonicalAddress,
                null, null, null,
                SIPCallDirection.Out,
                SDP.SDP_MIME_CONTENTTYPE,
                null,
                null
            );

            var audioEndpoint = new AudioBrixEndpoint(new AudioEncoder());
            audioEndpoint.SetSourceLatency(TimeSpan.FromMilliseconds(25));
            var mediaSession = new VoIPMediaSession(audioEndpoint.ToMediaEndpoints());

            await callUA.InitiateCallAsync(cd, mediaSession, _cfg.RingTimeoutSeconds);

            var newCall = new OutgoingCall(callUA, new CallPartyDescriptor(string.Empty, dstUri), audioEndpoint);

            _activeCalls.Add(newCall);

            return newCall;
        }

        public void Start()
        {
            _regUa?.Start();
        }

        public void Stop()
        {
            foreach (var activeCall in _activeCalls)
            {
                activeCall.Hangup();
            }

            _regUa?.Stop();
        }
    }
}