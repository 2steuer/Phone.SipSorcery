using AudioBrix.Interfaces;
using AudioBrix.SipSorcery;
using SIPSorcery.SIP.App;

namespace Phone.SipSorcery.CallHandling
{
    public class Call
    {
        public event Action<Call, CallState>? OnStateChanged;

        public event EventHandler<FormatChangedEventArgs>? AudioSourceFormatChanged;
        public event EventHandler<FormatChangedEventArgs>? AudioSinkFormatChanged;
        public event EventHandler<EventArgs>? AudioStart;
        public event EventHandler<EventArgs>? AudioStop;
        public event EventHandler<EventArgs>? AudioPause;
        public event EventHandler<EventArgs>? AudioResume;


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
                    if (_audio != null)
                    {
                        // remove event registrations

                        _audio.OnStart -= _audio_OnStart;
                        _audio.OnStop -= _audio_OnStop;
                        _audio.OnPause -= _audio_OnPause;
                        _audio.OnResume -= _audio_OnResume;
                        _audio.OnSinkFormatChanged -= _audio_OnSinkFormatChanged;
                        _audio.OnSourceFormatChanged -= _audio_OnSourceFormatChanged;

                    }

                    _audio = value;

                    if (_audio != null)
                    {
                        _audio.Source = _audioSource;
                        _audio.Sink = _audioSink;

                        // add event registrations

                        _audio.OnStart += _audio_OnStart;
                        _audio.OnStop += _audio_OnStop;
                        _audio.OnPause += _audio_OnPause;
                        _audio.OnResume += _audio_OnResume;
                        _audio.OnSinkFormatChanged += _audio_OnSinkFormatChanged;
                        _audio.OnSourceFormatChanged += _audio_OnSourceFormatChanged;
                    }
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
            protected set {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged?.Invoke(this, value);
                }
            }
        }

        public CallDirection Direction { get; }

        public CallPartyDescriptor CallParty { get; }

        internal Call(SIPUserAgent ua, CallDirection direction, CallPartyDescriptor callParty)
        {
            _ua = ua;
            Direction = direction;

            _ua.OnCallHungup += _ua_OnCallHungup;
            CallParty = callParty;
        }

        private void _ua_OnCallHungup(SIPSorcery.SIP.SIPDialogue obj)
        {
            State = CallState.Ended;
        }

        public void Hangup()
        {
            _ua.Hangup();
            State = CallState.Ended;
        }
        
        public virtual void Dispose()
        {
            _ua.Dispose();
        }

        private void _audio_OnSourceFormatChanged(object? sender, FormatChangedEventArgs e)
        {
            AudioSourceFormatChanged?.Invoke(this, e);
        }

        private void _audio_OnSinkFormatChanged(object? sender, FormatChangedEventArgs e)
        {
            AudioSinkFormatChanged?.Invoke(this, e);
        }

        private void _audio_OnResume(object? sender, EventArgs e)
        {
            AudioResume?.Invoke(this, e);
        }

        private void _audio_OnPause(object? sender, EventArgs e)
        {
            AudioPause?.Invoke(this, e);
        }

        private void _audio_OnStop(object? sender, EventArgs e)
        {
            AudioStop?.Invoke(this, e);
        }

        private void _audio_OnStart(object? sender, EventArgs e)
        {
            AudioStart?.Invoke(this, e);
        }
    }

}
