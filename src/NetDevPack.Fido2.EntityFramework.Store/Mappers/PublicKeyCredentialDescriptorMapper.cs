using Fido2NetLib;
using Fido2NetLib.Objects;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Mappers;

internal static class PublicKeyCredentialDescriptorMapper
{
    public static Fido2NetLib.Objects.PublicKeyCredentialDescriptor ToDomain(StoredCredentialDetail model)
    {

        return new Fido2NetLib.Objects.PublicKeyCredentialDescriptor
        {
            Id = model.PublicKeyId,
            Transports = model.Transports?.Split(';').Select(s => s.ToEnum<AuthenticatorTransport>()).ToArray(),
            Type = model.Type
        };
    }
}
