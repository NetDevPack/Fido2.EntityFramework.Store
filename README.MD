# Fido2 EntityFramework.Store

An Entity Framework Core store for [Fido2](https://github.com/passwordless-lib/fido2-net-lib) (WebAuthn / passkeys).

## Purpose

Provide a better integration between the [Fido2](https://github.com/passwordless-lib/fido2-net-lib) library and ASP.NET Core Identity.

## Compatibility

| NetDevPack.Fido2.EntityFramework.Store | Fido2 | Target framework |
| -------------------------------------- | ----- | ---------------- |
| 4.x                                    | 4.x   | .NET 10 (LTS)    |
| 3.x                                    | 3.x   | .NET 8, .NET 9, .NET 10 |

## How to implement

1. Add to `Program.cs`
```csharp
builder.Services.AddFido2(builder.Configuration.GetSection("Fido2"));
builder.Services.AddFido2Context<ApplicationDbContext>();
```

2. Change `ApplicationDbContext`
```csharp
using NetDevPack.Fido2.EntityFramework.Store.Model;
using NetDevPack.Fido2.EntityFramework.Store.Store;

public class ApplicationDbContext : IdentityDbContext, IFido2Context
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredCredentialDetail> Fido2StoredCredential { get; set; }
}
```

3. Inject `IFido2Store` to save and recover credentials:
```csharp
// Registration
var credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
{
    AttestationResponse = attestationResponse,
    OriginalOptions = options,
    IsCredentialIdUniqueToUserCallback = async (args, ct) =>
        !(await _fido2Store.ListCredentialsByPublicKeyIdAsync(args.CredentialId)).Any()
});
_fido2Store.Store("My security key", fidoUser, StoredCredential.FromRegisteredCredential(credential));

// Login
var stored = await _fido2Store.GetCredentialByPublicKeyIdAsync(assertionResponse.RawId);
var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
{
    AssertionResponse = assertionResponse,
    OriginalOptions = options,
    StoredPublicKey = stored.PublicKey,
    StoredSignatureCounter = stored.SignatureCounter,
    IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
        (await _fido2Store.GetCredentialsByUserHandleAsync(args.UserHandle)).Any(c => c.Descriptor.Id.SequenceEqual(args.CredentialId))
});
await _fido2Store.UpdateCounter(result.CredentialId, result.SignCount, result.IsBackedUp);
```

## Upgrading from 3.x to 4.x

4.x targets **.NET 10** and **Fido2 4**. Stay on 3.x for .NET 8/9 or Fido2 3.

### API changes
- `Fido2NetLib.Development.StoredCredential` does not exist in Fido2 4. The store now uses its own
  `NetDevPack.Fido2.EntityFramework.Store.Model.StoredCredential`. It keeps the 3.x property names (`UserId`, `Descriptor`, `PublicKey`,
  `UserHandle`, `SignatureCounter`, `CredType`, `RegDate`, `AaGuid`). Replace `using Fido2NetLib.Development;` with
  `using NetDevPack.Fido2.EntityFramework.Store.Model;`.
- New `StoredCredential.FromRegisteredCredential(RegisteredPublicKeyCredential)` builds a credential from the result of
  `IFido2.MakeNewCredentialAsync`, including transports and backup flags.
- `IFido2Store.GetCredentialByPublicKeyIdAsync` returns `StoredCredential?`.
- `IFido2Store.UpdateCounter(byte[] credentialId, uint counter, bool? isBackedUp = null)` also stores the Backup State flag.
- Fido2 4 itself changed its API: `MakeNewCredentialAsync`, `MakeAssertionAsync`, `RequestNewCredential` and `GetAssertionOptions` now take
  `*Params` objects, `MakeAssertionAsync` throws `Fido2VerificationException` instead of returning a status, and options no longer carry
  `Status`/`ErrorMessage`. Use `AuthenticatorAssertionRawResponse.RawId` (bytes) to look up credentials, since `Id` is now a string.
  See the [Fido2 releases](https://github.com/passwordless-lib/fido2-net-lib/releases).

### Database schema
Two columns were added to `StoredCredentialDetail`. Nothing was renamed or removed, and existing rows keep working:

| Column             | Type          | Default for existing rows |
| ------------------ | ------------- | ------------------------- |
| `IsBackupEligible` | `bool` (not null) | `false`               |
| `IsBackedUp`       | `bool` (not null) | `false`               |

Create a migration in your application:
```bash
dotnet ef migrations add Fido2StoreV4 -c ApplicationDbContext
dotnet ef database update -c ApplicationDbContext
```

The generated migration only adds the two columns with `defaultValue: false`. For SQL Server it is equivalent to:
```sql
ALTER TABLE [Fido2StoredCredential] ADD [IsBackupEligible] bit NOT NULL DEFAULT CAST(0 AS bit);
ALTER TABLE [Fido2StoredCredential] ADD [IsBackedUp] bit NOT NULL DEFAULT CAST(0 AS bit);
```

`IsBackedUp` is refreshed on the next successful login when you pass `result.IsBackedUp` to `UpdateCounter`. `IsBackupEligible` cannot be known for credentials registered with 3.x, so it stays `false` for them.

## Demo

See the demo in `demo/Fido2.Passwordless`. Docker build context is the repository root:
```bash
docker build -f demo/Fido2.Passwordless/Dockerfile .
```
