using ModifiedCB.Settings;

namespace ModifiedCB
{
    public class CBCommunication : ICommunicationStrategy
    {
        private readonly ICommunicationStrategy _httpStrategy;
        private readonly ICommunicationStrategy _rabbitMqStrategy;
        private readonly CircuitBreakerState _state;
        private readonly TimeSpan _retryDelay;

        public CBCommunication(ICommunicationStrategy httpStrategy,
                            ICommunicationStrategy rabbitMqStrategy,
                            TimeSpan retryDelay,
                            int failureThreshold,
                            TimeSpan resetTimeout)
        {
            _httpStrategy = httpStrategy;
            _rabbitMqStrategy = rabbitMqStrategy;
            _retryDelay = retryDelay;
            _state = new CircuitBreakerState(failureThreshold, resetTimeout); // Параметри Circuit Breaker передаються сюди
        }

        public async Task SendMessage(CommunicationSettings settings)
        {
            if (_state.IsOpen)
            {
                // Використовуємо RabbitMQ при відкритому стані Circuit Breaker
                await _rabbitMqStrategy.SendMessage(settings);
            }
            else
            {
                int attempt = 0;
                bool success = false;
                while (attempt < 2 && !success)
                {
                    try
                    {
                        attempt++;
                        await _httpStrategy.SendMessage(settings);
                        success = true;  // Якщо успішно — залишаємо CB закритим або скидаємо стан
                    }
                    catch
                    {
                        if (attempt < 2)  // Якщо це не остання спроба — чекаємо перед повторенням
                        {
                            Console.WriteLine($"HTTP request failed. Retrying in {_retryDelay.TotalSeconds} seconds...");
                            await Task.Delay(_retryDelay);
                        }
                        else
                        {
                            Console.WriteLine("HTTP request failed twice. Switching to RabbitMQ.");
                            _state.IsOpen = true;  // Перемикаємось на RabbitMQ після двох невдалих спроб
                        }
                    }
                }

                if (!success)
                {
                    await _rabbitMqStrategy.SendMessage(settings);
                }
            }
        }
    }
}
