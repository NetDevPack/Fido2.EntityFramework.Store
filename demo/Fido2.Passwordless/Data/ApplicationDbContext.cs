using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetDevPack.Fido2.EntityFramework.Store.Model;
using NetDevPack.Fido2.EntityFramework.Store.Store;

namespace Fido2.Passwordless.Data
{
    public class ApplicationDbContext : IdentityDbContext, IFido2Context
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<StoredCredential> Fido2StoredCredential { get; set; }
    }
}