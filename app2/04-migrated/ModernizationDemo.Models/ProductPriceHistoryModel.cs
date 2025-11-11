namespace ModernizationDemo.Models;

public class ProductPriceHistoryModel
{
	public int Id { get; set; }
	public DateTime ValidFrom { get; set; }
	public DateTime ValidTo { get; set; }
	public decimal Price { get; set; }
	public decimal TotalQuantity { get; set; }
}