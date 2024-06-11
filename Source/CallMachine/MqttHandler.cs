using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

internal class MqttHandler
{
    public event Action StartSignalReceived;

    public event Action StopSignalReceived;

    IManagedMqttClient? _client;
    ManagedMqttClientOptions _clientOptions;

    private string _stateTopic;
    private string _onlineStatePayload;
    private string _offlineStatePayload;
    private string _listenTopic;

    private List<string> _startPayloads;
    private List<string> _stopPayloads;

    public MqttHandler(IConfiguration cfg)
    {
        var stateCfg = cfg.GetRequiredSection("State");
        _listenTopic = cfg.GetValue<string>("ListenTopic") ?? throw new ArgumentException("No listen topic given.");
        _startPayloads = cfg.GetRequiredSection("StartPayloads").Get<List<string>>() ?? throw new ArgumentException("No start payloads given.");
        _stopPayloads = cfg.GetRequiredSection("StopPayloads").Get<List<string>>() ?? throw new ArgumentException("No stop payloads given.");
        

        ManagedMqttClientOptionsBuilder optb = new ManagedMqttClientOptionsBuilder();
        optb.WithClientOptions(co => {
            co.WithTcpServer(cfg.GetValue<string>("Host"), cfg.GetValue<int>("Port"));
            co.WithClientId($"{cfg.GetValue<string>("ClientId")}{Guid.NewGuid()}");
            if (cfg.GetValue<bool>("Authenticate"))
            {
                co.WithCredentials(cfg.GetValue<string>("User"), cfg.GetValue<string>("Password"));
            }


            _stateTopic = stateCfg.GetValue<string>("Topic") ?? throw new ArgumentException("State Topic not given");
            _onlineStatePayload = stateCfg.GetValue<string>("OnPayload") ?? throw new ArgumentException("Online Payload not given");
            _offlineStatePayload = stateCfg.GetValue<string>("OffPayload") ?? throw new ArgumentException("No offline state payload given.");

            co.WithWillTopic(_stateTopic);
            co.WithWillRetain(true);
            co.WithWillPayload(_offlineStatePayload);
        });

        

        _clientOptions = optb.Build();
    }

    public async Task Start()
    {
        if (_client != null)
        {
            throw new InvalidOperationException("Already running!");
        }

        _client = new MqttFactory().CreateManagedMqttClient();
        await _client.StartAsync(_clientOptions);

        await _client.EnqueueAsync(new MqttApplicationMessageBuilder()
            .WithTopic(_stateTopic)
            .WithPayload(_onlineStatePayload)
            .Build());

        await _client.SubscribeAsync(_listenTopic);
        _client.ApplicationMessageReceivedAsync += MessageHandler;
    }

    private async Task MessageHandler(MqttApplicationMessageReceivedEventArgs e)
    {
        await e.AcknowledgeAsync(CancellationToken.None);

        var str = e.ApplicationMessage.ConvertPayloadToString();

        if (_startPayloads.Contains(str))
        {
            StartSignalReceived?.Invoke();
        }
        else if (_stopPayloads.Contains(str))
        {
            StopSignalReceived?.Invoke();
        }
    }

    public async Task Stop()
    {
        if (_client == null)
        {
            throw new InvalidOperationException("Not running!");
        }

        await _client.EnqueueAsync(new MqttApplicationMessageBuilder()
            .WithTopic(_stateTopic)
            .WithPayload(_offlineStatePayload)
            .Build());
        
        await _client.StopAsync();
        _client = null;
    }
}