// See https://aka.ms/new-console-template for more information

using AudioBrix.Bricks.Basic;
using AudioBrix.Bricks.Buffer;
using AudioBrix.Bricks.Generators;
using AudioBrix.Bricks.Generators.Signals;
using AudioBrix.Material;
using AudioBrix.NAudio;
using AudioBrix.PortAudio.Helper;
using AudioBrix.PortAudio.Streams;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Phone.SipSorcery;
using Phone.SipSorcery.CallHandling;
using SIPSorcery.SIP;

Console.WriteLine("Hello, World!");

var cfg = new PhoneConfig()
{
    Username = "testfon1",
    Password = "testfon1pw",
    Protocol = SIPProtocolsEnum.udp,
    Register = true,
    Server = "fritz.box",
    RingTimeoutSeconds = 10
};

var phone = new Phone.SipSorcery.Phone(cfg);

phone.OnRegistrationStateChanged += (o, state) => Console.WriteLine($"New State: {state}");

ManualResetEvent signal = new ManualResetEvent(false);

phone.Start();

var wfr = new WaveFileReader(@"K:\tmp\01 Hung Up.wav");
var rs = new WdlResamplingSampleProvider(wfr.ToSampleProvider(), 8000);
var mono = new StereoToMonoSampleProvider(rs);
var fs = mono.ToFrameSource();

var dha = PortAudioHelper.GetDefaultHostApi();
var dod = PortAudioHelper.GetDefaultOutputDevice(dha);

var os = new PortAudioOutput(dha, dod.index, 8000, 1, 0.05);
var obuf = new AudioBuffer(new AudioFormat(8000, 1), 8000);

obuf.FillWithZero = true;
obuf.WaitOnEmpty = false;

os.Source = obuf;


var myCall = await phone.Call("**1@fritz.box");

myCall.OnStateChanged += (call, state) =>
{
    Console.WriteLine($"Call State: {state}");

    if (state == CallState.Established)
    {
        signal.Set();
    }
};

myCall.AudioStart += (sender, eventArgs) =>
{
    os.Start();
    Console.WriteLine("Audio Start");
};

myCall.AudioStop += (sender, eventArgs) =>
{
    os.Stop();
    Console.WriteLine("Audio Stop");
};
myCall.AudioSourceFormatChanged += (sender, eventArgs) => Console.WriteLine($"Source Format: {eventArgs.NewFormat}");


myCall.AudioSink = obuf;
myCall.AudioSource = fs;


signal.WaitOne();
Thread.Sleep(2500);
await Task.Delay(5000);

var sg = new WaveForm<Sine>(new AudioFormat(8000, 1), new Sine(660));
myCall.AudioSource = sg;

await Task.Delay(5000);
myCall.Hangup();

Console.ReadLine();

phone.Stop();