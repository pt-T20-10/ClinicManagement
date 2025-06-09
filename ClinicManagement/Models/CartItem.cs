using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{
    public class CartItem : BaseViewModel
    {
        private int _quantity;
        private decimal _unitPrice;

        /// <summary>
        /// The medicine product
        /// </summary>
        public Medicine Medicine { get; }

        /// <summary>
        /// Quantity of the medicine in the cart
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        /// <summary>
        /// Unit price of the medicine (used from the medicine's current sell price)
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged(nameof(UnitPrice));
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        /// <summary>
        /// Total price for this line item (quantity * unit price)
        /// </summary>
        public decimal LineTotal => Quantity * UnitPrice;

        /// <summary>
        /// Creates a new cart item from a medicine and quantity
        /// </summary>
        /// <param name="medicine">The medicine to add to cart</param>
        /// <param name="quantity">Initial quantity</param>
        public CartItem(Medicine medicine, int quantity = 1)
        {
            Medicine = medicine;
            Quantity = quantity;
            UnitPrice = medicine.CurrentSellPrice;
        }

        /// <summary>
        /// Creates a cart item from an existing invoice detail
        /// </summary>
        /// <param name="detail">Invoice detail to convert to cart item</param>
        public CartItem(InvoiceDetail detail)
        {
            Medicine = detail.Medicine;
            Quantity = detail.Quantity ?? 1;
            UnitPrice = detail.SalePrice ?? detail.Medicine.CurrentSellPrice;
        }

        /// <summary>
        /// Converts this cart item to an invoice detail
        /// </summary>
        /// <param name="invoiceId">The ID of the invoice this detail belongs to</param>
        /// <returns>A new invoice detail ready to be saved</returns>
        public InvoiceDetail ToInvoiceDetail(int invoiceId)
        {
            return new InvoiceDetail
            {
                InvoiceId = invoiceId,
                MedicineId = Medicine.MedicineId,
                Quantity = Quantity,
                SalePrice = UnitPrice
            };
        }
    }
}
