using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class ExternalDocumentType
{
    public string Code { get; set; } = null!;

    public string Label { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public string UploadRole { get; set; } = null!;

    public string SharedFolder { get; set; } = null!;

    public bool RequiresControlReportNumber { get; set; }

    public bool RequiresArticleCode { get; set; }

    public bool RequiresDeliveryNoteNumber { get; set; }

    public bool RequiresManufacturingOrderNumber { get; set; }

    public bool RequiresSupplierOrderNumber { get; set; }

    public bool RequiresSalesOrderNumber { get; set; }

    public bool RequiresCustomerOrderReference { get; set; }

    public virtual ICollection<ExternalDocument> ExternalDocuments { get; set; } = new List<ExternalDocument>();

    public virtual AppRole UploadRoleNavigation { get; set; } = null!;
}
