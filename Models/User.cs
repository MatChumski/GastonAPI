using System.Text.Json.Serialization;
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

    [JsonIgnore]
    public virtual ICollection<Category> Categories { get; } = new List<Category>();
    [JsonIgnore]
    public virtual ICollection<Expense> Expenses { get; } = new List<Expense>();
}
