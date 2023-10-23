using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;

namespace Phone.SipSorcery.AudioEncoders
{
    internal class PhoneAudioEncoder : IAudioEncoder
    {
        private const int G722_BIT_RATE = 64000;

        private G722Codec? _g722Codec = null;
        private G722CodecState? _g722CodecState = null;
        private G722Codec? _g722Decoder = null;
        private G722CodecState? _g722DecoderState = null;

        private G729Encoder? _g729Encoder = null;
        private G729Decoder? _g729Decoder = null;

        public List<AudioFormat> SupportedFormats { get; } = new List<AudioFormat>
        {
            //new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU),
            new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMA),
            //new AudioFormat(SDPWellKnownMediaFormatsEnum.G722),
            //new AudioFormat(SDPWellKnownMediaFormatsEnum.G729),
        };


        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.Codec == AudioCodecsEnum.G722)
            {
                if (_g722Codec == null)
                {
                    _g722Codec = new G722Codec();
                    _g722CodecState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
                }

                int outputBufferSize = pcm.Length / 2;
                byte[] encodedSample = new byte[outputBufferSize];
                int res = _g722Codec.Encode(_g722CodecState, encodedSample, pcm, pcm.Length);

                return encodedSample;
            }
            else if (format.Codec == AudioCodecsEnum.G729)
            {
                if (_g729Encoder == null)
                    _g729Encoder = new G729Encoder();

                byte[] pcmBytes = new byte[pcm.Length * sizeof(short)];
                Buffer.BlockCopy(pcm, 0, pcmBytes, 0, pcmBytes.Length);
                return _g729Encoder.Process(pcmBytes);
            }
            else if (format.Codec == AudioCodecsEnum.PCMU)
            {
                return pcm.Select(x => MuLawEncoder.LinearToMuLawSample(x)).ToArray();
            }
            else if (format.Codec == AudioCodecsEnum.PCMA)
            {
                return pcm.Select(s => ALaw_Encode(s)).ToArray();
            }
            else
            {
                throw new ApplicationException($"Audio format {format.Codec} cannot be encoded.");
            }
        }

        byte ALaw_Encode(short number)
        {
            const short ALAW_MAX = 0xFFF;
            short mask = 0x800;
            byte sign = 0;
            byte position = 11;
            byte lsb = 0;
            if (number < 0)
            {
                number = (short) -number;
                sign = 0x80;
            }
            if (number > ALAW_MAX)
            {
                number = ALAW_MAX;
            }
            for (; ((number & mask) != mask && position >= 5); mask >>= 1, position--) ;
            lsb = (byte)((number >> ((position == 4) ? (1) : (position - 4))) & 0x0f);
            return (byte)((sign | ((position - 4) << 4) | lsb) ^ 0x55);
        }

        short ALaw_Decode(byte unumber)
        {
            int number = unumber;
            int sign = 0x00;
            int position = 0;
            short decoded = 0;
            number ^= 0x55;
            if ((number & 0x80) != 0)
            {
                number &= ~(1 << 7);
                sign = -1;
            }
            position = ((number & 0xF0) >> 4) + 4;
            if (position != 4)
            {
                decoded = (short) ((1 << position) | ((number & 0x0F) << (position - 4))
                                           | (1 << (position - 5)));
            }
            else
            {
                decoded = (short) ((number << 1) | 1);
            }
            return (short) ((sign == 0) ? (decoded) : (-decoded));
        }


        public short[] DecodeAudio(byte[] encodedSample, AudioFormat format)
        {
            if (format.Codec == AudioCodecsEnum.G722)
            {
                if (_g722Decoder == null)
                {
                    _g722Decoder = new G722Codec();
                    _g722DecoderState = new G722CodecState(G722_BIT_RATE, G722Flags.None);
                }

                short[] decodedPcm = new short[encodedSample.Length * 2];
                int decodedSampleCount = _g722Decoder.Decode(_g722DecoderState, decodedPcm, encodedSample, encodedSample.Length);

                return decodedPcm.Take(decodedSampleCount).ToArray();
            }
            if (format.Codec == AudioCodecsEnum.G729)
            {
                if (_g729Decoder == null)
                    _g729Decoder = new G729Decoder();

                byte[] decodedBytes = _g729Decoder.Process(encodedSample);
                short[] decodedPcm = new short[decodedBytes.Length / sizeof(short)];
                Buffer.BlockCopy(decodedBytes, 0, decodedPcm, 0, decodedBytes.Length);
                return decodedPcm;
            }
            else if (format.Codec == AudioCodecsEnum.PCMU)
            {
                return encodedSample.Select(x => MuLawDecoder.MuLawToLinearSample(x)).ToArray();
            }
            else if (format.Codec == AudioCodecsEnum.PCMA)
            {
                return encodedSample.Select(s => ALaw_Decode(s)).ToArray();
            }
            else
            {
                throw new ApplicationException($"Audio format {format.Codec} cannot be decoded.");
            }
        }
    }
}
