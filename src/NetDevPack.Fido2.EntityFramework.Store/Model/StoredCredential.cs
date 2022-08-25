using Fido2NetLib.Objects;
using System.ComponentModel.DataAnnotations;

namespace NetDevPack.Fido2.EntityFramework.Store.Model;

public class StoredCredential
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Username { get; set; }

    public byte[]? UserId { get; set; }
    
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

    public StoredCredential SetUsername(string username)
    {
        Username = username;
        return this;
    }
}
