﻿namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class CommentEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string IPAddress { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string CommentContent { get; set; }
    public Guid PostId { get; set; }
    public bool IsApproved { get; set; }

    public virtual PostEntity Post { get; set; }
    public virtual ICollection<CommentReplyEntity> Replies { get; set; } = new HashSet<CommentReplyEntity>();
}
