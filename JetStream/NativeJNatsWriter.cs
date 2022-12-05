using MessagePack;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client;

namespace NatsWriters
{
    internal class JNatsWriterOptions
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Topic { get; set; }
        public string StreamName { get; set; }
        public int ThreadFactor { get; set; }
    }
    internal sealed class JNatsDataWriter : IDataWriter<TestDataStruct>
    {
        private readonly INatsOutput _output;
        private IConnection? _connection;
        private IJetStream? _jstream;
        private readonly JNatsWriterOptions _opts;
        private bool disposed = false;
        private readonly SemaphoreSlim ThreadLimiter;

        public JNatsDataWriter(INatsOutput output, IOptions<JNatsWriterOptions> opts)
        {
            _output = output;
            _opts = opts.Value;
            ThreadLimiter = new SemaphoreSlim(Math.Max(_opts.ThreadFactor, 1));
        }

        private async Task InitializeStream()
        {
            if (_connection is null)
                throw new NullReferenceException("NATS connection wasn't properly initialized");
            var NManager = _connection.CreateJetStreamManagementContext();
            try
            {
                NManager.GetStreamInfo(_opts.StreamName);
                await _output.Info($"Stream {_opts.StreamName} is already present");
                return;
            }
            catch (NATSJetStreamException exc)
            {
                if (exc.ErrorCode != 404)
                    throw;
            }

            NManager.AddStream(new StreamConfiguration.StreamConfigurationBuilder()
                    .WithName(_opts.StreamName)
                    .WithSubjects(_opts.Topic)
                    .WithMaxBytes(100L * 1024 * 1024)
                    .WithStorageType(StorageType.Memory)
                    .Build());
            await _output.Info($"New stream {_opts.StreamName} created");
        }

        public async Task SendData(TestDataStruct input)
        {
            await ThreadLimiter.WaitAsync();
            byte[] data = MessagePackSerializer.Serialize(input);
            _ = Task.Factory.StartNew(StreamData, data).ConfigureAwait(false);
        }

        private void StreamData(object? boxedData)
        {
            if (boxedData is not null && boxedData is byte[])
            {
                var data = (byte[])boxedData;
                if (_jstream is not null)
                    _jstream.Publish(_opts.Topic, data);
                else
                    throw new NullReferenceException("JetStream does not exists");
            }
            else
            {
                throw new ArgumentException("Unable to determine type of passed data");
            }
            ThreadLimiter.Release();
        }

        public Task StartWorkAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                var NOpts = ConnectionFactory.GetDefaultOptions();
                NOpts.Url = _opts.Url;
                NOpts.User = _opts.User;
                NOpts.Password = _opts.Password;
                _connection = new ConnectionFactory()
                    .CreateConnection(NOpts);
                await _output.Info("Opened NATS connection");
                _jstream = _connection.CreateJetStreamContext();

                await InitializeStream();
            });
        }

        public void Dispose()
        {
            if (!disposed)
            {
                _connection?.Dispose();
                disposed = true;
            }
        }
    }
}
