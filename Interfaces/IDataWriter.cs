namespace NatsWriters
{
    internal interface IDataWriter<TInput> : IDisposable
    {
        Task StartWorkAsync();
        Task SendData(TInput input);
    }
}
