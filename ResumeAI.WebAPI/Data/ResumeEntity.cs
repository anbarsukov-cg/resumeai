using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ResumeAI.WebAPI.Data;

[Table("resume")]
public class ResumeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    [Column("candidate_name")]
    public string CandidateName { get; set; }
    [Column("profession")]
    public string Profession { get; set; }
    [Column("years_of_exp")]
    public int YearsOfCommercialExperience { get; set; }
    [Column("skills")]
    public List<string> Skills { get; set; }
    [Column("summary")]
    public string Summary { get; set; }
}