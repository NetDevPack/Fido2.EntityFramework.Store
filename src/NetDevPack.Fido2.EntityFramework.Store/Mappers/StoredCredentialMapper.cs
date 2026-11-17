using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Mappers;

internal static class StoredCredentialMapper
{
    public static StoredCredentialDetail ToModel(StoredCredential domain)
    {
        string? transports = null;
        if (domain.Descriptor.Transports is { Length: > 0 })
            transports = string.Join(";", domain.Descriptor.Transports);
        return new StoredCredentialDetail
        {
            UserId = domain.UserId,
            PublicKey = domain.PublicKey,
            UserHandle = domain.UserHandle,
            SignatureCounter = domain.SignatureCounter,
            CredType = domain.CredType,
            RegDate = domain.RegDate,
            AaGuid = domain.AaGuid,
            IsBackupEligible = domain.IsBackupEligible,
            IsBackedUp = domain.IsBackedUp,

            PublicKeyId = domain.Descriptor.Id,
            Type = domain.Descriptor.Type,
            Transports = transports
        };
    }

    public static StoredCredential ToDomain(StoredCredentialDetail model)
    {
        return new StoredCredential
        {
            UserId = model.UserId ?? [],
            PublicKey = model.PublicKey,
            UserHandle = model.UserHandle,
            SignatureCounter = model.SignatureCounter,
            CredType = model.CredType,
            RegDate = model.RegDate,
            AaGuid = model.AaGuid,
            IsBackupEligible = model.IsBackupEligible,
            IsBackedUp = model.IsBackedUp,
            Descriptor = PublicKeyCredentialDescriptorMapper.ToDomain(model)
        };
    }
}
