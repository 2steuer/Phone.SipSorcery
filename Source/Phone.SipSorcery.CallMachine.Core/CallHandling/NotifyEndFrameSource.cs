using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioBrix.Interfaces;
using AudioBrix.Material;

namespace Phone.SipSorcery.CallMachine.Core.CallHandling
{
    internal class NotifyEndFrameSource : IFrameSource
    {
        private IFrameSource _source;

        public event Action<object>? FrameSourceEnded; 

        public AudioFormat Format => _source.Format;

        public NotifyEndFrameSource(IFrameSource source)
        {
            _source = source;
        }

        public Span<float> GetFrames(int frameCount)
        {
            var s = _source.GetFrames(frameCount);

            if (s.Length == 0)
            {
                FrameSourceEnded?.Invoke(this);
            }

            return s;
        }

    }
}
