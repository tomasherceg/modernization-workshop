using System;
using System.Collections.Generic;

namespace ModernizationDemo.NewCore;

public partial class ProductSalePrice
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public decimal Price { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public virtual Product Product { get; set; } = null!;
}
