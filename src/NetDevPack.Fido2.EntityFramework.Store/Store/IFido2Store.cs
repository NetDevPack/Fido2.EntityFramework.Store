using Fido2NetLib;
using Fido2NetLib.Objects;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Store;

public interface IFido2Store
{
    Task<IEnumerable<StoredCredential>> ListCredentialsByUser(string username);
    Task<IEnumerable<PublicKeyCredentialDescriptor>> ListPublicKeysByUser(string username);
    Task<IEnumerable<StoredCredential>> ListCredentialsByPublicKeyIdAsync(byte[] credentialId);
    Task<StoredCredential?> GetCredentialByPublicKeyIdAsync(byte[] credentialId);
    void Store(string securityKeyAlias, Fido2User user, StoredCredential storedCredential);
    //void Store(Model.FidoInfo info, StoredCredential storedCredential);
    Task<IEnumerable<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle);
    /// <summary>
    /// Updates the signature counter after a successful assertion and, when provided, the Backup State (BS) flag
    /// returned by Fido2 4 (<c>VerifyAssertionResult.IsBackedUp</c>).
    /// </summary>
    Task UpdateCounter(byte[] credentialId, uint counter, bool? isBackedUp = null);
    Task<string?> GetUsernameByIdAsync(byte[] userId);
    Task<List<StoredCredentialDetail>> ListCredentialDetailsByUser(byte[] userId);
    Task<List<StoredCredentialDetail>> ListCredentialDetailsByUser(string username);
    Task<bool> RemoveByPublicKeyId(byte[] publicKeyId);
    Task<bool> RemoveBySecretName(string name);
    Task<bool> HasSecurityKey(string name);
}
