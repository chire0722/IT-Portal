using System.Text.Json;
using IT_Form.Models;

namespace IT_Form.Infrastructure.Notifications;

internal static class EmailOutboxMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static EmailOutboxMessage Serialize(IEmailBackgroundMessage message)
        => message switch
        {
            WelcomeEmailMessage welcome => Create(nameof(WelcomeEmailMessage), welcome),
            OtpEmailMessage otp => Create(nameof(OtpEmailMessage), otp),
            ItLockoutAlertMessage lockout => Create(nameof(ItLockoutAlertMessage), lockout),
            RenderedEmailMessage rendered => Create(nameof(RenderedEmailMessage), rendered),
            _ => throw new InvalidOperationException($"Unsupported email background message type: {message.GetType().FullName}")
        };

    public static IEmailBackgroundMessage Deserialize(string messageType, string payloadJson)
        => messageType switch
        {
            nameof(WelcomeEmailMessage) => Deserialize<WelcomeEmailMessage>(payloadJson),
            nameof(OtpEmailMessage) => Deserialize<OtpEmailMessage>(payloadJson),
            nameof(ItLockoutAlertMessage) => Deserialize<ItLockoutAlertMessage>(payloadJson),
            nameof(RenderedEmailMessage) => Deserialize<RenderedEmailMessage>(payloadJson),
            _ => throw new InvalidOperationException($"Unsupported email outbox message type: {messageType}")
        };

    private static EmailOutboxMessage Create<TMessage>(string messageType, TMessage message)
        where TMessage : IEmailBackgroundMessage
        => new()
        {
            MessageType = messageType,
            PayloadJson = JsonSerializer.Serialize(message, SerializerOptions),
            AvailableAfterUtc = DateTime.UtcNow
        };

    private static TMessage Deserialize<TMessage>(string payloadJson)
        => JsonSerializer.Deserialize<TMessage>(payloadJson, SerializerOptions)
           ?? throw new InvalidOperationException($"Unable to deserialize email outbox payload to {typeof(TMessage).Name}.");
}
