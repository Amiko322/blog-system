namespace BlogSystem.Configuration.Constants;

public static class RabbitMqTopology
{
    public const string DEAD_LETTER_EXCHANGE = "dead_letter_exchange";
    public const string DEAD_LETTER_QUEUE = "dead_letter_queue";
    public const string DEAD_LETTER_ROUTING_KEY = "dead.letter";

    public const string API_REQUEST_QUEUE = "api.requests";
    public const string API_RESPONSE_QUEUE = "api.responses";

    public const int MAX_RETRY_COUNT = 3;
    public const int BASE_DELAY = 1000; // мс
}
