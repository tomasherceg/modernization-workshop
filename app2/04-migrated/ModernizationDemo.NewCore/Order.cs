using System;
using System.Collections.Generic;

namespace ModernizationDemo.NewCore;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Completed { get; set; }

    public DateTime? Canceled { get; set; }

    public decimal TotalPrice { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;
}
