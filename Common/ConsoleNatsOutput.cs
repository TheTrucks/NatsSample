namespace NatsWriters
{
    internal class ConsoleNatsOutput : INatsOutput
    {
        public Task Info(string input)
        {
            Console.WriteLine($"INFO\t{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss:ffff")} { input }");
            return Task.CompletedTask;
        }
    }
}
