using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace NatsWriters
{
    internal class JRunner : IHostedService
    {
        private readonly IAsyncNatsDataInput<TestDataStruct> _dataInput;
        private readonly IDataWriter<TestDataStruct> _writer;
        private readonly INatsOutput _output;
        private PeriodicTimer _timer;

        public JRunner(IAsyncNatsDataInput<TestDataStruct> dataInput, IDataWriter<TestDataStruct> writer, IOptions<DbDataInputOptions> opts, INatsOutput output)
        {
            _dataInput = dataInput;
            _writer = writer;
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(opts.Value.SecondsBack));
            _output = output;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _output.Info("Worker starting");
            await _writer.StartWorkAsync();
            var BackgroundTask = Task.Factory.StartNew(() => WorkUnit(), cancellationToken);
            await BackgroundTask.ConfigureAwait(false);
            await _output.Info("Worker started");
        }

        private async Task WorkUnit()
        {
            while (true) 
            {
                await _timer.WaitForNextTickAsync();
                await _output.Info("Timer tick");
                var dataVals = _dataInput.NextValue().GetAsyncEnumerator();
                while (await dataVals.MoveNextAsync())
                {
                    await _writer.SendData(dataVals.Current);
                    await _output.Info($"Pushed {dataVals.Current.Datetime}:{dataVals.Current.Value}");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _output.Info("Worker stopping");
            _dataInput.Dispose();
            _writer.Dispose();
            _timer.Dispose();
            await _output.Info("Worker stopped");
        }
    }
}
