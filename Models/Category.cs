using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;


namespace GastonAPI.Models;

public partial class Category
{
    public int Id { get; set; }

    public int? FkUser { get; set; }

    public string? Name { get; set; }

    public DateTime? CreationDate { get; set; }
    
    public virtual ICollection<Expense> Expenses { get; } = new List<Expense>();

    [JsonIgnore]
    public virtual User? FkUserNavigation { get; set; }
}
