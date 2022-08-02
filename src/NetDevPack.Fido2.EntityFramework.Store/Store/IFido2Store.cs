using Fido2NetLib.Development;
using Fido2NetLib.Objects;

namespace NetDevPack.Fido2.EntityFramework.Store.Store;

public interface IFido2Store
{
    Task<IEnumerable<StoredCredential>> ListCredentialsByUser(string username);
    Task<IEnumerable<PublicKeyCredentialDescriptor>> ListPublicKeysByUser(string username);
    Task<IEnumerable<StoredCredential>> ListCredentialsByPublicKeyIdAsync(byte[] credentialId);
    Task<StoredCredential> GetCredentialByPublicKeyIdAsync(byte[] credentialId);
    void Store(string userName, StoredCredential storedCredential);
    Task<IEnumerable<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle);
    Task UpdateCounter(byte[] credentialId, uint counter);
    Task<string> GetUsernameByIdAsync(byte[] userId);
}
