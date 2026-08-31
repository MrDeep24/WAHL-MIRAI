using System;

namespace WahlMirai.Web.Models;

public partial class CensusWhitelist
{
    public uint Id { get; set; }
    public string DocumentHash { get; set; } = null!;
    public string EncryptedDocument { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public byte GradeId { get; set; }
    public bool ExcluirDePromocion { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public uint? ClaimedByUserId { get; set; }
    public uint UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual Grade Grade { get; set; } = null!;
    public virtual Voter? ClaimedByVoter { get; set; }
    public virtual Voter UploadedByVoter { get; set; } = null!;
}
