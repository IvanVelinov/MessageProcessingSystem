﻿namespace Shared.Config
{
    public class RabbitMqSettings
    {
        public required string Host { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string QueueName { get; set; }
    }
}
