using Microsoft.Extensions.Options;
using Npgsql;

namespace NatsWriters
{
    internal sealed class TestDataInputRand : INatsDataInput<TestDataStruct>
    {
        private readonly Random _rand = Random.Shared;
        private bool disposed = false;

        public void Dispose()
        {
            disposed = true;
        }

        public IEnumerable<TestDataStruct> NextValue()
        {
            while (!disposed)
            {
                yield return new TestDataStruct { Datetime = DateTime.UtcNow, Value = _rand.NextDouble() };
            }
        }
    }

    internal sealed class DbDataInputOptions 
    {
        public string ConnString { get; set; }
        public string TableName { get; set; }
        public int SecondsBack { get; set; }
    }
    internal sealed class DbDataInput : IAsyncNatsDataInput<TestDataStruct>
    {
        private readonly NpgsqlConnection _conn;
        private readonly DbDataInputOptions _opts;
        private readonly INatsOutput _output;
        private bool disposed = false;

        public DbDataInput(IOptions<DbDataInputOptions> opts, INatsOutput output)
        {
            _opts = opts.Value;
            _conn = new NpgsqlConnection(_opts.ConnString);
            _output = output;
        }

        private async Task<NpgsqlConnection> CheckConnection()
        {
            if (_conn.State != System.Data.ConnectionState.Open)
            {
                await _conn.OpenAsync();
                await _output.Info("Psql connection opened");
            }
            return _conn;
        }

        public async IAsyncEnumerable<TestDataStruct> NextValue()
        {
            await CheckConnection();
            using (var cmd = new NpgsqlCommand($"select * from {_opts.TableName} where datetime > $1", _conn))
            {
                cmd.Parameters.Add(new() { Value = DateTime.UtcNow.AddSeconds(-_opts.SecondsBack) });
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    await _output.Info("Psql queried");
                    while (rdr.Read())
                    {
                        var DataRead = new TestDataStruct 
                            { Datetime = rdr.GetDateTime(0), Value = rdr.GetDouble(1) };
                        yield return DataRead;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                _conn.Dispose();
                disposed = true;
            }
        }
    }
}
