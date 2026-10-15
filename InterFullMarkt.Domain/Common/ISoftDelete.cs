namespace InterFullMarkt.Domain;

/// <summary>
/// Mantıksal silme (Soft Delete) arayüzü.
/// Silinen kayıtları veritabanından tamamen silmek yerine pasife çeker.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Varlık silinmiş mi?
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Silinme tarihi
    /// </summary>
    DateTime? DeletedDate { get; set; }

    /// <summary>
    /// Silen kullanıcı ID'si
    /// </summary>
    string? DeletedByUserId { get; set; }
}
