using Fido2NetLib;
using Fido2NetLib.Objects;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Mappers;

internal static class PublicKeyCredentialDescriptorMapper
{
    public static PublicKeyCredentialDescriptor ToDomain(StoredCredentialDetail model)
    {
        return new PublicKeyCredentialDescriptor(
            model.Type ?? PublicKeyCredentialType.PublicKey,
            model.PublicKeyId,
            ParseTransports(model.Transports));
    }

    /// <summary>
    /// Transports are stored as a ';' separated list of enum names (e.g. "Usb;Internal").
    /// Accepts enum names (any case) and WebAuthn values (e.g. "usb", "smart-card"); unknown values are ignored.
    /// </summary>
    internal static AuthenticatorTransport[]? ParseTransports(string? transports)
    {
        if (string.IsNullOrWhiteSpace(transports))
            return null;

        var result = new List<AuthenticatorTransport>();
        foreach (var value in transports.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<AuthenticatorTransport>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            {
                result.Add(parsed);
                continue;
            }

            try
            {
                result.Add(value.ToEnum<AuthenticatorTransport>());
            }
            catch (ArgumentException)
            {
                // Unknown transport (e.g. added by a newer authenticator): ignore it, it is only a hint for the client.
            }
        }

        return result.ToArray();
    }
}
