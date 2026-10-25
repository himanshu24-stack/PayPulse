using System.Collections.Generic;

namespace PayPulse.Models;

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public class RiskAssessmentResult
{
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OutstandingAmount { get; set; }
    public int DaysOverdue { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public int RiskScore { get; set; } // 0 to 100
    public List<string> Factors { get; set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
}
