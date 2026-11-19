using System.Threading.Channels;
using MreHunter.Api.Services;

namespace MreHunter.Api.Infrastructure
{
    /// <summary>
    /// Cola en memoria de correos. Los endpoints encolan el envío y responden de inmediato,
    /// en lugar de esperar 1-5 s a que el servidor SMTP responda.
    /// </summary>
    public sealed class EmailQueue
    {
        private readonly Channel<Func<EmailService, Task>> _channel =
            Channel.CreateBounded<Func<EmailService, Task>>(new BoundedChannelOptions(500)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true
            });

        private readonly ILogger<EmailQueue> _logger;

        public EmailQueue(ILogger<EmailQueue> logger) => _logger = logger;

        public void Enqueue(Func<EmailService, Task> job)
        {
            if (!_channel.Writer.TryWrite(job))
                _logger.LogWarning("Cola de correos llena: se descartó un correo.");
        }

        public ChannelReader<Func<EmailService, Task>> Reader => _channel.Reader;
    }

    public sealed class EmailBackgroundService : BackgroundService
    {
        private readonly EmailQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(EmailQueue queue, IServiceScopeFactory scopes, ILogger<EmailBackgroundService> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var email = scope.ServiceProvider.GetRequiredService<EmailService>();
                    await job(email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando correo en segundo plano.");
                }
            }
        }
    }
}
