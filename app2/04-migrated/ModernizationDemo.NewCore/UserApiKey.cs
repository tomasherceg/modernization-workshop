using System;
using System.Collections.Generic;

namespace ModernizationDemo.NewCore;

public partial class UserApiKey
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ApiKey { get; set; } = null!;

    public DateTime ValidFrom { get; set; }

    public DateTime ValidTo { get; set; }

    public virtual User User { get; set; } = null!;
}
