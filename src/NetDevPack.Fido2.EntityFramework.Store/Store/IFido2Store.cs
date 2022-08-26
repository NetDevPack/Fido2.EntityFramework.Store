using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Store;

public interface IFido2Store
{
    Task<IEnumerable<StoredCredential>> ListCredentialsByUser(string username);
    Task<IEnumerable<PublicKeyCredentialDescriptor>> ListPublicKeysByUser(string username);
    Task<IEnumerable<StoredCredential>> ListCredentialsByPublicKeyIdAsync(byte[] credentialId);
    Task<StoredCredential> GetCredentialByPublicKeyIdAsync(byte[] credentialId);
    void Store(string securityKeyAlias, Fido2User user, StoredCredential storedCredential);
    //void Store(Model.FidoInfo info, StoredCredential storedCredential);
    Task<IEnumerable<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle);
    Task UpdateCounter(byte[] credentialId, uint counter);
    Task<string?> GetUsernameByIdAsync(byte[] userId);
    Task<List<StoredCredentialDetail>> ListCredentialDetailsByUser(byte[] userId);
    Task<List<StoredCredentialDetail>> ListCredentialDetailsByUser(string username);
}
