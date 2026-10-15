namespace InterFullMarkt.Domain;

/// <summary>
/// Audit bilgilerini takip eden varlıklar için arayüz.
/// Kim tarafından ne zaman oluşturulduğu ve güncellendiğini kaydeder.
/// </summary>
public interface IAuditEntity
{
    /// <summary>
    /// Varlığı oluşturan kullanıcı ID'si
    /// </summary>
    string CreatedByUserId { get; set; }

    /// <summary>
    /// Varlığın oluşturulma tarihi
    /// </summary>
    DateTime CreatedDate { get; set; }

    /// <summary>
    /// Varlığı son güncelleyen kullanıcı ID'si
    /// </summary>
    string? UpdatedByUserId { get; set; }

    /// <summary>
    /// Varlığın güncellenme tarihi
    /// </summary>
    DateTime UpdatedDate { get; set; }
}
