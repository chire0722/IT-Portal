// Project comment: EmailOutboxMessageSerializer.cs is part of the IT Form application source code.

using System.Text.Json;
using IT_Form.Models;
using IT_Form.Services;

namespace IT_Form.Infrastructure.Notifications;

/// <summary>
/// Defines the EmailOutboxMessageSerializer class used by the IT Form application.
/// </summary>
internal static class EmailOutboxMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Runs the Serialize operation.
    /// </summary>
    public static EmailOutboxMessage Serialize(
        IEmailBackgroundMessage message,
        ILicenseEncryptionService encryptionService)
        => message switch
        {
            WelcomeEmailMessage welcome => Create(nameof(WelcomeEmailMessage), welcome, encryptionService),
            OtpEmailMessage otp => Create(nameof(OtpEmailMessage), otp, encryptionService),
            ItLockoutAlertMessage lockout => Create(nameof(ItLockoutAlertMessage), lockout, encryptionService),
            RenderedEmailMessage rendered => Create(nameof(RenderedEmailMessage), rendered, encryptionService),
            _ => throw new InvalidOperationException($"Unsupported email background message type: {message.GetType().FullName}")
        };

    /// <summary>
    /// Runs the Deserialize operation.
    /// </summary>
    public static IEmailBackgroundMessage Deserialize(
        string messageType,
        string payloadJson,
        ILicenseEncryptionService encryptionService)
        => messageType switch
        {
            nameof(WelcomeEmailMessage) => Deserialize<WelcomeEmailMessage>(encryptionService.DecryptKey(payloadJson)),
            nameof(OtpEmailMessage) => Deserialize<OtpEmailMessage>(encryptionService.DecryptKey(payloadJson)),
            nameof(ItLockoutAlertMessage) => Deserialize<ItLockoutAlertMessage>(encryptionService.DecryptKey(payloadJson)),
            nameof(RenderedEmailMessage) => Deserialize<RenderedEmailMessage>(encryptionService.DecryptKey(payloadJson)),
            _ => throw new InvalidOperationException($"Unsupported email outbox message type: {messageType}")
        };

    private static EmailOutboxMessage Create<TMessage>(
        string messageType,
        TMessage message,
        ILicenseEncryptionService encryptionService)
        where TMessage : IEmailBackgroundMessage
        => new()
        {
            MessageType = messageType,
            PayloadJson = encryptionService.EncryptKey(JsonSerializer.Serialize(message, SerializerOptions)),
            AvailableAfterUtc = DateTime.UtcNow
        };

    private static TMessage Deserialize<TMessage>(string payloadJson)
        => JsonSerializer.Deserialize<TMessage>(payloadJson, SerializerOptions)
           ?? throw new InvalidOperationException($"Unable to deserialize email outbox payload to {typeof(TMessage).Name}.");
}

