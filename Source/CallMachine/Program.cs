using Microsoft.Extensions.Configuration;
using NLog;
using Phone.SipSorcery;
using Phone.SipSorcery.CallMachine.Core;
using SIPSorcery.SIP;

var log = LogManager.GetCurrentClassLogger();

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

log.Info("System running");

TaskCompletionSource cancelSource = new TaskCompletionSource();

bool haveSigInt = false;

Console.CancelKeyPress += (sender, eventArgs) =>
{
    log.Debug("Processing SIGINT");
    eventArgs.Cancel = true;
    haveSigInt = true;
    cancelSource.TrySetResult();
};

AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
{
    if (!haveSigInt)
    {
        log.Debug("Processing SIGTERM");
        cancelSource.TrySetResult();
    }
    else
    {
        log.Debug($"Got SIGTERM but ignoring it because of SIGINT before");
    }
};

await cancelSource.Task;

log.Info("System shutting down");

cm.Stop();