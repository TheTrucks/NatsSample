using AlterNats;
using Microsoft.Extensions.Options;

namespace NatsWriters
{
    internal class TestDataWriterOptions
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Topic { get; set; }
    }
    internal sealed class TestDataWriter : IDataWriter<TestDataStruct>
    {
        private readonly TestDataWriterOptions _options;
        private readonly INatsOutput _output;
        private readonly NatsConnection _nconn;
        private bool disposed = false;
        private bool built = false;

        public TestDataWriter(IOptions<TestDataWriterOptions> opts, INatsOutput output)
        {
            _options = opts.Value;
            _output = output;
            _nconn = new NatsConnection(NatsOptions.Default with
            {
                Url = _options.Url,
                Serializer = new MessagePackNatsSerializer(),
                ConnectOptions = ConnectOptions.Default with
                {
                    Echo = true,
                    Username = _options.User,
                    Password = _options.Password
                }
            });
        }

        public async Task StartWorkAsync()
        {
            if (!built)
            {
                try
                {
                    await _nconn.ConnectAsync();
                    await _output.Info("Connected");
                    built = true;
                }
                catch (Exception exc)
                {
                    await _output.Info(exc.ToString());
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (_nconn is not null)
                    _nconn.DisposeAsync().AsTask().GetAwaiter().GetResult();
                disposed = true;
            }
        }

        public async Task SendData(TestDataStruct input)
        {
            await _nconn.PublishAsync(_options.Topic, input);
        }
    }
}
