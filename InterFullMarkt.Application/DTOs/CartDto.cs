namespace InterFullMarkt.Application.DTOs;

public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public int TotalQuantity => Items.Sum(i => i.Quantity);
    public string Currency { get; set; } = "EUR";
}

public class CartItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal TotalPrice => UnitPrice * Quantity;
}
