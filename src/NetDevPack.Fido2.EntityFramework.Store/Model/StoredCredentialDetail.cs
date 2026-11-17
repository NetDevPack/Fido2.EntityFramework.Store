using Fido2NetLib;
using Fido2NetLib.Objects;
using System.ComponentModel.DataAnnotations;

namespace NetDevPack.Fido2.EntityFramework.Store.Model;

public class StoredCredentialDetail
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public byte[]? UserId { get; set; }

    /// <summary>
    /// Friendly name for security key
    /// </summary>
    public string? SecurityKeyName { get; set; }


    [Required]
    public byte[] PublicKey { get; set; }

    [Required]
    public byte[] PublicKeyId { get; set; }

    [Required]
    public byte[] UserHandle { get; set; }

    public uint SignatureCounter { get; set; }
    public string? CredType { get; set; }
    public DateTime RegDate { get; set; }
    public Guid AaGuid { get; set; }


    public PublicKeyCredentialType? Type { get; set; }
    public string? Transports { get; set; }

    /// <summary>
    /// Backup Eligibility (BE) flag. Added in 4.0 (Fido2 4). Rows created by older versions default to <c>false</c>.
    /// </summary>
    public bool IsBackupEligible { get; set; }

    /// <summary>
    /// Backup State (BS) flag. Added in 4.0 (Fido2 4). Updated on every successful assertion.
    /// </summary>
    public bool IsBackedUp { get; set; }

    public StoredCredentialDetail UpdateUserDetails(Fido2User user)
    {
        Username = user.Name;
        UserId = user.Id;
        return this;
    }

    public StoredCredentialDetail SetSecurityKeyName(string securityKeyAlias)
    {
        securityKeyAlias ??= Username;
        SecurityKeyName = securityKeyAlias;
        return this;
    }
}
