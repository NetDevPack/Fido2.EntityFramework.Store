using Microsoft.EntityFrameworkCore;
using NetDevPack.Fido2.EntityFramework.Store.Model;

namespace NetDevPack.Fido2.EntityFramework.Store.Store;

public interface IFido2Context
{
    DbSet<StoredCredentialDetail> Fido2StoredCredential { get; set; }
}
