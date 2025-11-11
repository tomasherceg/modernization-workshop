using System.ComponentModel.DataAnnotations;

namespace ModernizationDemo.Api.Models
{
	public class PagingInfo
	{
		[Range(0, int.MaxValue)]
		public int Skip { get; set; }

		[Range(10, 500)]
		public int Take { get; set; } = 10;
	}
}