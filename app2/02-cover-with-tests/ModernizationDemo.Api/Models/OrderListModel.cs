using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModernizationDemo.Api.Models
{
	public class OrderListModel
	{
		public int Id { get; set; }
		public DateTime Created { get; set; }
		public DateTime? LastChange { get; set; }

		public OrderStatus Status { get; set; }

		public decimal TotalPrice { get; set; }
	}
}