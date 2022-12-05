namespace NatsWriters
{
    internal interface INatsOutput
    {
        Task Info(string input);
    }
}
