using System.Text.Json.Serialization;

public class Pension 
{
    public string? Identifier { get; set; }
    public string? Custodian { get; set; }
    public double Income { get; set; }
    public bool OneTime { get; set; }
    public int BeginningAge { get; set; }
    public bool HasCola { get; set; }
    public double SurvivorsBenefit { get; set; }

    public string FullName 
    {
        get {
            return (Identifier != null ? Identifier+ " " : "") + "Pension" + (Custodian != null ? " at " + Custodian : "");
        }
    }
}