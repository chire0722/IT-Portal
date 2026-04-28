// Project comment: ISoftDeletable.cs is part of the IT Form application source code.

using System;

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the contract implemented by ISoftDeletable services.
    /// </summary>
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedByUserId { get; set; }
    }
}
