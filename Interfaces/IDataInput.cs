namespace NatsWriters
{
    public interface IAsyncNatsDataInput<TResult> : IDisposable
    {
        public IAsyncEnumerable<TResult> NextValue();
    }

    public interface INatsDataInput<TResult> : IDisposable
    {
        public IEnumerable<TResult> NextValue();
    }
}
