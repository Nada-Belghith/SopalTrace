using System;

namespace SopalTrace.Application.Alertes;

public class NotifsSuperviseurContexte
{
    public Guid ExecControleOfId { get; set; }
    public string NumeroOf { get; set; } = null!;
    public string ArticleCode { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public int NbNotificationsManquees { get; set; }
    public TimeSpan DureeDepuisPremierRetard { get; set; }
}

public class NotifsManagerContexte
{
    public Guid ExecControleOfId { get; set; }
    public string NumeroOf { get; set; } = null!;
    public string ArticleCode { get; set; } = null!;
    public string OperationCode { get; set; } = null!;
    public int NbNotificationsManquees { get; set; }
    public TimeSpan DureeDepuisPremierRetard { get; set; }
}
