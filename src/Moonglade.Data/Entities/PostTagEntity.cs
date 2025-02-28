namespace MoongladePure.Data.Entities;

public class PostTagEntity
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public int TagId { get; set; }

    public virtual PostEntity Post { get; set; }
    public virtual TagEntity Tag { get; set; }
}
