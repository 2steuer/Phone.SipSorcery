// See https://aka.ms/new-console-template for more information

using AudioBrix.Bricks.Basic;
using AudioBrix.Bricks.Generators;
using AudioBrix.Bricks.Generators.Signals;
using AudioBrix.Material;
using AudioBrix.NAudio;
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
    RingTimeoutSeconds = 30
};

var phone = new Phone.SipSorcery.Phone(cfg);

phone.OnRegistrationStateChanged += (o, state) => Console.WriteLine($"New State: {state}");

ManualResetEvent signal = new ManualResetEvent(false);

phone.Start();

var myCall = await phone.Call("**1@fritz.box");

myCall.OnStateChanged += (call, state) =>
{
    Console.WriteLine($"Call State: {state}");

    if (state == CallState.Established)
    {
        signal.Set();
    }
};

var wfr = new WaveFileReader(@"K:\tmp\01 Hung Up.wav");
var rs = new WdlResamplingSampleProvider(wfr.ToSampleProvider(), 8000);
var mono = new StereoToMonoSampleProvider(rs);
var fs = mono.ToFrameSource();

myCall.AudioSource = fs;

myCall.AudioStart += (sender, eventArgs) => Console.WriteLine("Audio Start");
myCall.AudioStop += (sender, eventArgs) => Console.WriteLine("Audio Stop");
myCall.AudioSourceFormatChanged += (sender, eventArgs) => Console.WriteLine($"Source Format: {eventArgs.NewFormat}");

signal.WaitOne();
Thread.Sleep(2500);
await Task.Delay(5000);

var sg = new WaveForm<Sine>(new AudioFormat(8000, 1), new Sine(660));
myCall.AudioSource = sg;

await Task.Delay(5000);
myCall.Hangup();

Console.ReadLine();

phone.Stop();