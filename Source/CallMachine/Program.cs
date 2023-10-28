using Microsoft.Extensions.Configuration;
using Phone.SipSorcery;
using Phone.SipSorcery.CallMachine.Core;
using SIPSorcery.SIP;

var cfgFile = args.Length < 1 ? "config.json" : args[0];
var cfgb = new ConfigurationBuilder();
cfgb.AddJsonFile(cfgFile);
var cfg = cfgb.Build();

PhoneConfig? pcfg = cfg.GetRequiredSection("Sip").Get<PhoneConfig>();
string soundFile = cfg.GetValue<string>("AlarmFile") ?? throw new ArgumentException("Key AlarmFile not found in config");
List<string> targets = cfg.GetRequiredSection("Targets").Get<List<string>>() ?? throw new ArgumentException("Targets list invalid in configuration");
int retries = cfg.GetValue<int>("CallRetries");

CallMachine cm = new CallMachine(pcfg, retries);

cm.Start();

cm.AddJob("**620@fritz.box", "alarm.wav");
cm.AddJob("**1@fritz.box", "alarm.wav");
cm.AddJob("**620@fritz.box", "alarm.wav");

Console.ReadLine();

cm.Stop();