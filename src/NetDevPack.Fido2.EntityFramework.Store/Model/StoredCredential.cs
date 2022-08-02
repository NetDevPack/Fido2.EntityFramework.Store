using Fido2NetLib.Objects;

namespace NetDevPack.Fido2.EntityFramework.Store.Model;

public class StoredCredential
{
    public int Id { get; set; }
    public string Username { get; set; }
    public byte[] UserId { get; set; }
    public byte[] PublicKey { get; set; }
    public byte[] UserHandle { get; set; }
    public uint SignatureCounter { get; set; }
    public string CredType { get; set; }
    public DateTime RegDate { get; set; }
    public Guid AaGuid { get; set; }


    public byte[] PublicKeyId { get; set; }
    public PublicKeyCredentialType? Type { get; set; }
    public string Transports { get; set; }

    public StoredCredential SetUsername(string username)
    {
        Username = username;
        return this;
    }
}
