using System.Collections.Concurrent;
using Phone.SipSorcery.CallMachine.Core.CallHandling;

namespace Phone.SipSorcery.CallMachine.Core
{
    public class CallMachine
    {
        private Phone _phone;

        private ConcurrentQueue<CallJob> _jobs = new();

        private SemaphoreSlim _semaphore = new(0);

        private bool _running = false;
        private Thread? _worker = null;
        private CancellationTokenSource? _workerCancelSource = null;
        private CancellationTokenSource? _currentCallCancel = null;

        public CallMachine(PhoneConfig cfg)
        {
            _phone = new Phone(cfg);
        }

        public void Start()
        {
            if (_running)
            {
                throw new InvalidOperationException("Cannot start twice");
            }

            _running = true;
            _workerCancelSource = new CancellationTokenSource();
            _phone.Start();

            _worker = new Thread(WorkerThread);
            _worker.IsBackground = true;
            _worker.Start();
        }

        public void Stop()
        {
            if (!_running)
            {
                throw new InvalidOperationException("Not running");
            }

            _workerCancelSource!.Cancel();
            _worker!.Join();
            _phone.Stop();

            _running = false;
        }

        private async void WorkerThread()
        {
            CancellationToken ct = _workerCancelSource!.Token;

            while (!ct.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(ct);

                Dequeue:
                if (!_jobs.TryDequeue(out CallJob? currentJob))
                {
                    goto Dequeue;
                }

                var call =  await _phone.Call(currentJob.Uri);
                var hdlr = new PlayAudioHandler(call, currentJob.WaveFile);

                _currentCallCancel = new CancellationTokenSource();

                var internalCts =
                    CancellationTokenSource.CreateLinkedTokenSource(_workerCancelSource.Token, _currentCallCancel.Token);

                var result = await hdlr.WaitForResult(internalCts.Token);

                _currentCallCancel = null;

                if (!result)
                {
                    AddJob(currentJob.Uri, currentJob.WaveFile);
                }
            }
        }

        public void AddJob(string uri, string waveFile)
        {
            _jobs.Enqueue(new CallJob()
            {
                Uri = uri,
                WaveFile = waveFile,
            });
            _semaphore.Release();
        }

        public void CancelQueue()
        {
            while (_jobs.Count > 0)
            {
                _semaphore.Wait();

                NextTry:
                if (!_jobs.TryDequeue(out CallJob? _))
                {
                    goto NextTry;
                }
            }

            _currentCallCancel?.Cancel();
        }
    }
}