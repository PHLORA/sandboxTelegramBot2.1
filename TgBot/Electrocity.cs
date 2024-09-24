using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TgBot;

public class Electrocity
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public float Indicate { get; set; }
    
    public float? Difference { get; set; }
}