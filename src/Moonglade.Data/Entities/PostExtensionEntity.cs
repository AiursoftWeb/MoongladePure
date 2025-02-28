using System.ComponentModel.DataAnnotations;

namespace MoongladePure.Data.Entities;

public class PostExtensionEntity
{
    [Key]
    public Guid PostId { get; set; }
    public int Hits { get; set; }
    public int Likes { get; set; }

    public virtual PostEntity Post { get; set; }
}
