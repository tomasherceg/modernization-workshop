using System;
using System.Collections.Generic;

namespace ModernizationDemo.NewCore;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductSalePrice> ProductSalePrices { get; set; } = new List<ProductSalePrice>();
}
