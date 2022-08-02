using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetDevPack.Fido2.EntityFramework.Store.Mappers;

namespace NetDevPack.Fido2.EntityFramework.Store.Store
{
    internal class Fido2Store<TContext> : IFido2Store
        where TContext : DbContext, IFido2Context
    {
        private readonly TContext _context;
        private readonly ILogger<Fido2Store<TContext>> _logger;

        public Fido2Store(TContext context, ILogger<Fido2Store<TContext>> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IEnumerable<StoredCredential>> ListCredentialsByUser(string username)
        {
            var credentials = await _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().Where(w => w.Username == username.ToLower().Trim()).ToListAsync();
            return credentials.Select(StoredCredentialMapper.ToDomain);
        }

        public async Task<IEnumerable<PublicKeyCredentialDescriptor>> ListPublicKeysByUser(string username)
        {
            var pks = await _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().Where(w => w.Username == username.ToLower().Trim()).ToListAsync();
            return pks.Select(PublicKeyCredentialDescriptorMapper.ToDomain);
        }

        public async Task<IEnumerable<StoredCredential>> ListCredentialsByPublicKeyIdAsync(byte[] credentialId)
        {
            var users = await _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().Where(w => w.PublicKeyId == credentialId).ToListAsync();
            return users.Select(StoredCredentialMapper.ToDomain);
        }

        public async Task<StoredCredential> GetCredentialByPublicKeyIdAsync(byte[] credentialId)
        {
            var users = await _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().FirstAsync(w => w.PublicKeyId == credentialId);
            if (users == null)
                return null;

            return StoredCredentialMapper.ToDomain(users);
        }

        public void Store(string userName, StoredCredential storedCredential)
        {
            var model = StoredCredentialMapper.ToModel(storedCredential).SetUsername(userName);
            _context.Fido2StoredCredential.Add(model);
            _context.SaveChanges();
        }

        public async Task<IEnumerable<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle)
        {
            var users = await _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().Where(w => w.UserHandle == userHandle).ToListAsync();
            return users.Select(StoredCredentialMapper.ToDomain);
        }


        public async Task UpdateCounter(byte[] credentialId, uint counter)
        {
            var cred = await _context.Fido2StoredCredential.FirstOrDefaultAsync(f => f.PublicKeyId == credentialId);
            cred.SignatureCounter = counter;
            _context.SaveChanges();
        }

        public Task<string> GetUsernameByIdAsync(byte[] userId)
        {
            return _context.Fido2StoredCredential.AsNoTrackingWithIdentityResolution().Where(w => w.UserId == userId).Select(s => s.Username).FirstOrDefaultAsync();
        }
    }
}
