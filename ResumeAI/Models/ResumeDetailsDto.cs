namespace ResumeAI.Models;

public class ResumeDetailsDto
{
    public Guid Id { get; set; }
    public string CandidateName { get; set; }
    public string Profession { get; set; }
    public int YearsOfCommercialExperience { get; set; }
    public List<string> Skills { get; set; }
    public string Summary { get; set; }
}