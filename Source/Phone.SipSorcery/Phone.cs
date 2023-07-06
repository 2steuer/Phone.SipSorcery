﻿using System.Net.Sockets;
using Phone.SipSorcery.CallHandling;
using SIPSorcery.SIP;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery
{
    public class Phone
    {
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