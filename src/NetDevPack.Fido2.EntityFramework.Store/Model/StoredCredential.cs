using Fido2NetLib.Objects;

namespace NetDevPack.Fido2.EntityFramework.Store.Model;

/// <summary>
/// A registered WebAuthn credential.
/// Replaces <c>Fido2NetLib.Development.StoredCredential</c>, which Fido2 4 no longer ships.
/// Property names match the Fido2 3 type to make the upgrade easier.
/// </summary>
public class StoredCredential
{
    /// <summary>
    /// The user id (<see cref="Fido2NetLib.Fido2User.Id"/>) that owns the credential.
    /// </summary>
    public byte[] UserId { get; set; } = [];

    /// <summary>
    /// Credential id, type and transports.
    /// </summary>
    public PublicKeyCredentialDescriptor Descriptor { get; set; } = null!;

    /// <summary>
    /// The credential public key (COSE encoded).
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// The user handle returned by the authenticator.
    /// </summary>
    public byte[] UserHandle { get; set; } = [];

    /// <summary>
    /// Last known signature counter.
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>
    /// Attestation statement format (e.g. <c>none</c>, <c>packed</c>).
    /// </summary>
    public string? CredType { get; set; }

    /// <summary>
    /// Registration date.
    /// </summary>
    public DateTime RegDate { get; set; }

    /// <summary>
    /// Authenticator AAGUID.
    /// </summary>
    public Guid AaGuid { get; set; }

    /// <summary>
    /// Backup Eligibility (BE) flag: the credential can be synced (e.g. a passkey).
    /// </summary>
    public bool IsBackupEligible { get; set; }

    /// <summary>
    /// Backup State (BS) flag: the credential is currently backed up/synced.
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// Creates a <see cref="StoredCredential"/> from the result of <c>IFido2.MakeNewCredentialAsync</c>.
    /// </summary>
    /// <param name="credential">The verified credential.</param>
    /// <param name="regDate">Registration date. Defaults to <see cref="DateTime.UtcNow"/>.</param>
    public static StoredCredential FromRegisteredCredential(RegisteredPublicKeyCredential credential, DateTime? regDate = null)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return new StoredCredential
        {
            UserId = credential.User.Id,
            Descriptor = new PublicKeyCredentialDescriptor(credential.Type, credential.Id, credential.Transports),
            PublicKey = credential.PublicKey,
            UserHandle = credential.User.Id,
            SignatureCounter = credential.SignCount,
            CredType = credential.AttestationFormat,
            RegDate = regDate ?? DateTime.UtcNow,
            AaGuid = credential.AaGuid,
            IsBackupEligible = credential.IsBackupEligible,
            IsBackedUp = credential.IsBackedUp
        };
    }
}
