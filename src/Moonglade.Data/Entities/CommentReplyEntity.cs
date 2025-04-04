﻿namespace MoongladePure.Data.Entities;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

public class CommentReplyEntity
{
    public Guid Id { get; set; }
    public string ReplyContent { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public Guid? CommentId { get; set; }

    public virtual CommentEntity Comment { get; set; }
}
