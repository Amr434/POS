namespace Application.DTOs;

public class InstallmentReportDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerPhone { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int PendingInstallments { get; set; }
    public int OverdueInstallments { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
}