using Microsoft.Extensions.Hosting;

namespace NatsWriters
{
    internal class Runner : BackgroundService
    {
        private readonly IDataWriter<TestDataStruct> _writer;
        private readonly INatsDataInput<TestDataStruct> _input;
        public Runner(IDataWriter<TestDataStruct> writer, INatsDataInput<TestDataStruct> input)
        {
            _writer = writer;
            _input = input;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _writer.StartWorkAsync();
            foreach (var Data in _input.NextValue())
            {
                if (stoppingToken.IsCancellationRequested)
                    break;
                await _writer.SendData(Data);
            }
        }

    }
}
