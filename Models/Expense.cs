
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GastonAPI.Models;

public partial class Expense
{
    public int Id { get; set; }

    public int? FkUser { get; set; }

    public int? FkCategory { get; set; }

    public double? Amount { get; set; }

    public string? Description { get; set; }

    public string? Type { get; set; }

    public DateTime? Date { get; set; }

    public DateTime? CreationDate { get; set; }

    [JsonIgnore]
    public virtual Category? FkCategoryNavigation { get; set; }

    [JsonIgnore]
    public virtual User? FkUserNavigation { get; set; }
}
