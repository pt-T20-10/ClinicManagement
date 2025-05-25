using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public int? PatientId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public string? Status { get; set; }

    public bool IsPharmacySale { get; set; }

    public int? MedicalRecordId { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual MedicalRecord? MedicalRecord { get; set; }

    public virtual Patient? Patient { get; set; }
}
