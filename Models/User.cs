using System;
using System.Collections.Generic;

namespace GastonAPI.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public DateTime? CreationDate { get; set; }

    public virtual ICollection<Category> Categories { get; } = new List<Category>();

    public virtual ICollection<Expense> Expenses { get; } = new List<Expense>();
}
