using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExternalDocument
{
    public Guid Id { get; set; }

    public string TypeCode { get; set; } = null!;

    public string? ControlReportNumber { get; set; }

    public string? ArticleCode { get; set; }

    public string? DeliveryNoteNumber { get; set; }

    public string? ManufacturingOrderNumber { get; set; }

    public string? SupplierOrderNumber { get; set; }

    public string? SalesOrderNumber { get; set; }

    public string? CustomerOrderReference { get; set; }

    public string? FileName { get; set; }

    public string? DocumentKey { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public byte[] Content { get; set; } = null!;

    public string? ContentSha256 { get; set; }

    public string UploadedBy { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public Guid? ReplacesDocumentId { get; set; }

    public string? ReplacementReason { get; set; }

    public DateTime? SupersededAt { get; set; }

    public virtual Article? ArticleCodeNavigation { get; set; }

    public virtual Sdelivery? DeliveryNoteNumberNavigation { get; set; }

    public virtual ICollection<ExternalDocument> InverseReplacesDocument { get; set; } = new List<ExternalDocument>();

    public virtual MfgheadOrdreFabrication? ManufacturingOrderNumberNavigation { get; set; }

    public virtual ExternalDocument? ReplacesDocument { get; set; }

    public virtual Sorder? SalesOrderNumberNavigation { get; set; }

    public virtual Porder? SupplierOrderNumberNavigation { get; set; }

    public virtual ExternalDocumentType TypeCodeNavigation { get; set; } = null!;

    public virtual Autili UploadedByNavigation { get; set; } = null!;
}
