# Fido2 EntityFramework.Store

A thin wrapper around [Fido2](https://github.com/passwordless-lib/fido2-net-lib) adding store capabilities to it.

## Purpouse

Provide a better integration between [Fido2](https://github.com/passwordless-lib/fido2-net-lib) library and ASP.NET Identity

## How to implement

1. Add at `Program.cs`
```csharp
builder.Services.AddFido2Context<ApplicationDbContext>();
```

2. Change `ApplicationDbContext`
```csharp
public class ApplicationDbContext : IdentityDbContext, IFido2Context
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredCredential> Fido2StoredCredential { get; set; }
}
```

3. Now you can inject `IFido2Store` to save and recover `StoredCredential`

## Demo

Check demo at Source Code.