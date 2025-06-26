using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public class Invoice
{
    public int InvoiceId { get; set; }

    public int? PatientId { get; set; }

    public int? StaffPrescriberId { get; set; }    // Người kê đơn (Dược sĩ)

    public int? StaffCashierId { get; set; }       // ✅ Người xác nhận thanh toán (Thu ngân)

    public decimal TotalAmount { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public string? Status { get; set; }

    public string InvoiceType { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public int? MedicalRecordId { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public virtual Staff? StaffPrescriber { get; set; }   // Dược sĩ

    public virtual Staff? StaffCashier { get; set; }      // ✅ Thu ngân

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual MedicalRecord? MedicalRecord { get; set; }

    public virtual Patient? Patient { get; set; }
}

