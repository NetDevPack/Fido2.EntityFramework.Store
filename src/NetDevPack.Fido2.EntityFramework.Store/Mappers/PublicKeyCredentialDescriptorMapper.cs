using System;
using System.Linq;
using Fido2NetLib;
using Fido2NetLib.Objects;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Mappers;

internal static class PublicKeyCredentialDescriptorMapper
{
    public static Fido2NetLib.Objects.PublicKeyCredentialDescriptor ToDomain(StoredCredentialDetail model)
    {
        var transports = string.IsNullOrWhiteSpace(model.Transports)
            ? null
            : model.Transports.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToEnum<AuthenticatorTransport>())
                .ToArray();

        return new PublicKeyCredentialDescriptor
        {
            Id = model.PublicKeyId,
            Transports = transports,
            Type = model.Type ?? PublicKeyCredentialType.PublicKey
        };
    }
}
