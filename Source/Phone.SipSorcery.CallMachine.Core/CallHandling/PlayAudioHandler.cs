﻿using Phone.SipSorcery.CallHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioBrix.Material;
using AudioBrix.NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Phone.SipSorcery.CallMachine.Core.CallHandling
{
    internal class PlayAudioHandler
    {
        public Action<object, CallHandlerState>? StateChanged;

        private CallHandlerState _state = CallHandlerState.Waiting;

        public CallHandlerState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        private Call _call;

        public Call Call => _call;

        private AudioFormat _format = new();
        private string _waveFile;

        private WaveFileReader? _reader = null;

        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public PlayAudioHandler(Call call, string waveFile)
        {
            _call = call;
            _call.OnStateChanged += _call_OnStateChanged;
            _call.AudioSourceFormatChanged += _call_AudioSourceFormatChanged;
            
            _waveFile = waveFile;
        }

        private void _call_AudioSourceFormatChanged(object? sender, AudioBrix.SipSorcery.FormatChangedEventArgs e)
        {
            _format = e.NewFormat;
        }

        public async Task<bool> WaitForResult(CancellationToken ct)
        {
            bool cancelled = false;

            var reg = ct.Register(() =>
            {
                cancelled = true;
                _call.Hangup();
            });

            try
            {
                return (await _tcs.Task) || cancelled; // return true when cancelled so we dont get added again
            }
            finally
            {
                reg.Dispose();
            }
        }

        private void _call_OnStateChanged(Call arg1, CallState arg2)
        {
            switch (arg2)
            {
                case CallState.Established:
                {
                    _reader = new WaveFileReader(_waveFile);

                    IWaveProvider sprov = _reader;

                    if (_reader.WaveFormat.Channels != _format.Channels)
                    {
                        if (_reader.WaveFormat.Channels == 2)
                        {
                            sprov = new StereoToMonoProvider16(_reader);
                        }
                        else if (_reader.WaveFormat.Channels == 1)
                        {
                            // _format then has to denote two channels
                            sprov = new MonoToStereoProvider16(_reader);
                        }
                    }

                    var resample = new WdlResamplingSampleProvider(sprov.ToSampleProvider(), (int)_format.SampleRate);
                    
                    _call.AudioSource = resample.ToFrameSource();
                    State = CallHandlerState.Active;
                    break;
                }

                case CallState.Ended:
                    _call.AudioSource = null;
                    _call.OnStateChanged -= _call_OnStateChanged;
                    _call.AudioSourceFormatChanged -= _call_AudioSourceFormatChanged;
                    _reader?.Close();
                    State = CallHandlerState.Finished;
                    _tcs.SetResult(true);
                    break;

                case CallState.Failed:
                    State = CallHandlerState.Failed;
                    _call.OnStateChanged -= _call_OnStateChanged;
                    _call.AudioSourceFormatChanged -= _call_AudioSourceFormatChanged;
                    _tcs.SetResult(false);
                    break;
            }
        }
    }
}
