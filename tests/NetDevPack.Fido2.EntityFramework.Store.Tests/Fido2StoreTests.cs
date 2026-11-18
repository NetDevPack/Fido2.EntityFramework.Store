using System.Text;
using AwesomeAssertions;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetDevPack.Fido2.EntityFramework.Store.Model;
using NetDevPack.Fido2.EntityFramework.Store.Store;
using Xunit;

namespace NetDevPack.Fido2.EntityFramework.Store.Tests;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IFido2Context
{
    public DbSet<StoredCredentialDetail> Fido2StoredCredential { get; set; } = null!;
}

/// <summary>
/// Runs <see cref="IFido2Store"/> against a real relational provider (SQLite in-memory),
/// so byte[] comparisons and column mappings are translated to SQL.
/// </summary>
public sealed class Fido2StoreTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private ServiceProvider _provider = null!;
    private AsyncServiceScope _scope;

    private IFido2Store Store => _scope.ServiceProvider.GetRequiredService<IFido2Store>();
    private TestDbContext Context => _scope.ServiceProvider.GetRequiredService<TestDbContext>();

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseSqlite(_connection));
        services.AddFido2Context<TestDbContext>();
        _provider = services.BuildServiceProvider();

        _scope = _provider.CreateAsyncScope();
        await Context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static Fido2User User(string name) => new()
    {
        Name = name,
        DisplayName = name,
        Id = Encoding.UTF8.GetBytes(name)
    };

    private static StoredCredential Credential(Fido2User user, byte[] credentialId, uint counter = 0, params AuthenticatorTransport[] transports) =>
        StoredCredential.FromRegisteredCredential(new RegisteredPublicKeyCredential
        {
            Type = PublicKeyCredentialType.PublicKey,
            Id = credentialId,
            PublicKey = [1, 2, 3, 4],
            Transports = transports,
            SignCount = counter,
            IsBackupEligible = true,
            IsBackedUp = false,
            AaGuid = Guid.Parse("08987058-cadc-4b81-b6e1-30de50dcbe96"),
            User = user,
            AttestationFormat = "none"
        }, new DateTime(2026, 11, 17, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task ShouldStoreAndGetCredentialByPublicKeyId()
    {
        var user = User("alice@netdevpack.net");
        byte[] credentialId = [10, 20, 30];

        Store.Store("YubiKey", user, Credential(user, credentialId, 5, AuthenticatorTransport.Usb, AuthenticatorTransport.Nfc));

        var stored = await Store.GetCredentialByPublicKeyIdAsync(credentialId);

        stored.Should().NotBeNull();
        stored!.Descriptor.Id.Should().Equal(credentialId);
        stored.Descriptor.Type.Should().Be(PublicKeyCredentialType.PublicKey);
        stored.Descriptor.Transports.Should().Equal(AuthenticatorTransport.Usb, AuthenticatorTransport.Nfc);
        stored.PublicKey.Should().Equal(1, 2, 3, 4);
        stored.UserId.Should().Equal(user.Id);
        stored.UserHandle.Should().Equal(user.Id);
        stored.SignatureCounter.Should().Be(5);
        stored.CredType.Should().Be("none");
        stored.AaGuid.Should().Be(Guid.Parse("08987058-cadc-4b81-b6e1-30de50dcbe96"));
        stored.IsBackupEligible.Should().BeTrue();
        stored.IsBackedUp.Should().BeFalse();

        (await Store.HasSecurityKey("YubiKey")).Should().BeTrue();
        (await Store.GetUsernameByIdAsync(user.Id)).Should().Be(user.Name);
    }

    [Fact]
    public async Task ShouldReturnNullWhenCredentialDoesNotExist()
    {
        (await Store.GetCredentialByPublicKeyIdAsync([9, 9, 9])).Should().BeNull();
    }

    [Fact]
    public async Task ShouldUpdateCounterAndBackupState()
    {
        var user = User("bob@netdevpack.net");
        byte[] credentialId = [1, 1, 1];
        Store.Store("Phone", user, Credential(user, credentialId, 1));

        await Store.UpdateCounter(credentialId, 42, isBackedUp: true);

        var stored = await Store.GetCredentialByPublicKeyIdAsync(credentialId);
        stored!.SignatureCounter.Should().Be(42);
        stored.IsBackedUp.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUpdateCounterWithoutChangingBackupStateWhenNotInformed()
    {
        var user = User("carol@netdevpack.net");
        byte[] credentialId = [2, 2, 2];
        var credential = Credential(user, credentialId, 1);
        credential.IsBackedUp = true;
        Store.Store("Laptop", user, credential);

        await Store.UpdateCounter(credentialId, 7);

        var stored = await Store.GetCredentialByPublicKeyIdAsync(credentialId);
        stored!.SignatureCounter.Should().Be(7);
        stored.IsBackedUp.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldListCredentialsByUser()
    {
        var alice = User("alice@netdevpack.net");
        var bob = User("bob@netdevpack.net");
        Store.Store("Key 1", alice, Credential(alice, [1]));
        Store.Store("Key 2", alice, Credential(alice, [2], 0, AuthenticatorTransport.Internal));
        Store.Store("Key 3", bob, Credential(bob, [3]));

        var credentials = (await Store.ListCredentialsByUser("alice@netdevpack.net")).ToList();
        credentials.Should().HaveCount(2);
        credentials.Select(c => c.Descriptor.Id[0]).Should().BeEquivalentTo(new byte[] { 1, 2 });

        var publicKeys = (await Store.ListPublicKeysByUser("alice@netdevpack.net")).ToList();
        publicKeys.Should().HaveCount(2);

        (await Store.ListCredentialDetailsByUser(alice.Id)).Should().HaveCount(2);
        (await Store.ListCredentialDetailsByUser("alice@netdevpack.net")).Select(d => d.SecurityKeyName)
            .Should().BeEquivalentTo("Key 1", "Key 2");
        (await Store.GetCredentialsByUserHandleAsync(bob.Id)).Should().ContainSingle();
        (await Store.ListCredentialsByPublicKeyIdAsync([3])).Should().ContainSingle();
    }

    [Fact]
    public async Task ShouldRemoveCredentials()
    {
        var user = User("dave@netdevpack.net");
        Store.Store("Key A", user, Credential(user, [5]));
        Store.Store("Key B", user, Credential(user, [6]));

        (await Store.RemoveByPublicKeyId([5])).Should().BeTrue();
        (await Store.RemoveBySecretName("Key B")).Should().BeTrue();
        (await Store.RemoveByPublicKeyId([5])).Should().BeFalse();

        (await Store.ListCredentialsByUser("dave@netdevpack.net")).Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldReadRowsWrittenByPreviousVersions()
    {
        // A row as written by 3.x (Fido2 3): transports stored as enum names, no backup flags
        Context.Fido2StoredCredential.Add(new StoredCredentialDetail
        {
            Username = "legacy@netdevpack.net",
            UserId = Encoding.UTF8.GetBytes("legacy"),
            SecurityKeyName = "Legacy",
            PublicKey = [7, 7],
            PublicKeyId = [7],
            UserHandle = Encoding.UTF8.GetBytes("legacy"),
            SignatureCounter = 3,
            CredType = "packed",
            RegDate = DateTime.UtcNow,
            AaGuid = Guid.Empty,
            Type = null,
            Transports = "Usb;Internal;unknown-transport"
        });
        await Context.SaveChangesAsync();

        var stored = await Store.GetCredentialByPublicKeyIdAsync([7]);

        stored.Should().NotBeNull();
        stored!.Descriptor.Type.Should().Be(PublicKeyCredentialType.PublicKey);
        stored.Descriptor.Transports.Should().Equal(AuthenticatorTransport.Usb, AuthenticatorTransport.Internal);
        stored.IsBackupEligible.Should().BeFalse();
        stored.IsBackedUp.Should().BeFalse();
        stored.SignatureCounter.Should().Be(3);
    }
}
