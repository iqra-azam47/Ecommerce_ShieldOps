using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShieldOps.Models
{
    // Keeping both enums cleanly visible within the same namespace cluster
    public enum OrderStatus
    {
        Ordered,
        Packing,
        SentToWarehouse,
        Shipped,
        Delivered
    }

    public enum ReturnStatus
    {
        None,
        Pending,
        Approved,
        Rejected
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Ordered;

        // Shipping Info (Required conditionally if any physical item is purchased)
        [StringLength(200)]
        public string? ShippingAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // FIXED: The Return Lifecycle Pipeline parameters are now correctly nested inside the class body block
        public ReturnStatus ReturnState { get; set; } = ReturnStatus.None;
        public string? ReturnReason { get; set; }
        public string? AdminReturnNotes { get; set; }
        public DateTime? ReturnRequestedAt { get; set; }
    } // <-- Class closes cleanly here now!
}