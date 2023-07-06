using AudioBrix.Interfaces;
using AudioBrix.SipSorcery;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery.CallHandling
{
    public class Call
    {
        public event Action<Call, CallState>? OnStateChanged; 

        private SIPUserAgent _ua;

        protected SIPUserAgent UserAgent => _ua;

        private CallState _state = CallState.Unknown;

        private Mutex _endpointMutex = new Mutex();
        private AudioBrixEndpoint? _audio;
        protected AudioBrixEndpoint? Audio
        {
            get
            {
                try
                {
                    _endpointMutex.WaitOne();
                    return _audio;

                }
                finally
                {
                    _endpointMutex.ReleaseMutex();
                }
            }
            set
            {
                try
                {
                    _endpointMutex.WaitOne();
                    _audio = value;
                    _audio.Source = _audioSource;
                    _audio.Sink = _audioSink;
                }
                finally
                {
                    _endpointMutex.ReleaseMutex();
                }
            }
        }

        private IFrameSource? _audioSource = null;

        private IFrameSink? _audioSink = null;

        public IFrameSource? AudioSource
        {
            get => _audioSource;
            set
            {
                _audioSource = value;
                try
                {
                    _endpointMutex.WaitOne();

                    if (_audio != null)
                    {
                        _audio.Source = value;
                    }
                }
                finally
                {
                    _endpointMutex.ReleaseMutex();
                }
            }
        }

        public IFrameSink? AudioSink
        {
            get => _audioSink;
            set
            {
                _audioSink = value;

                try
                {
                    _endpointMutex.WaitOne();

                    if (_audio != null)
                    {
                        _audio.Sink = value;
                    }
                }
                finally
                {
                    _endpointMutex.ReleaseMutex();
                }
            }
        }

        public CallState State
        {
            get => _state;
            protected set  {
                _state = value;
                OnStateChanged?.Invoke(this, value);
            }
        }

        public CallDirection Direction { get; }

        internal Call(SIPUserAgent ua, CallDirection direction)
        {
            _ua = ua;
            Direction = direction;
        }


        public void Hangup()
        {
            _ua.Hangup();
            State = CallState.Ended;
        }
    }

}
