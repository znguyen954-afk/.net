namespace ECommerceAPI.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum ProductStatus
{
    Active = 0,
    Inactive = 1,
    OutOfStock = 2,
    Discontinued = 3
}

public enum UserRole
{
    Customer = 0,
    Admin = 1,
    Manager = 2
}
