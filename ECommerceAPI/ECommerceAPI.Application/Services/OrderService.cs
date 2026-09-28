using ECommerceAPI.Application.Common;
using ECommerceAPI.Application.DTOs;
using ECommerceAPI.Application.Interfaces;
using ECommerceAPI.Domain.Entities;
using ECommerceAPI.Domain.Enums;
using ECommerceAPI.Domain.Interfaces;

namespace ECommerceAPI.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<OrderDto>> GetByIdAsync(Guid id, Guid? currentUserId = null, bool isAdmin = false)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        if (order == null || order.IsDeleted)
            return ApiResponse<OrderDto>.Fail($"Order with id '{id}' not found.");

        // Customers can only see their own orders
        if (!isAdmin && currentUserId.HasValue && order.UserId != currentUserId.Value)
            return ApiResponse<OrderDto>.Fail("You are not authorized to view this order.");

        return ApiResponse<OrderDto>.Ok(MapToDto(order));
    }

    public async Task<ApiResponse<PagedResult<OrderDto>>> GetPagedAsync(OrderQueryParams query)
    {
        var (items, total) = await _unitOfWork.Orders.GetPagedAsync(
            query.Page, query.PageSize,
            query.UserId, query.Status,
            query.FromDate, query.ToDate,
            query.SortBy, query.SortDesc);

        return ApiResponse<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    public async Task<ApiResponse<IEnumerable<OrderDto>>> GetMyOrdersAsync(Guid userId)
    {
        var orders = await _unitOfWork.Orders.GetByUserAsync(userId);
        return ApiResponse<IEnumerable<OrderDto>>.Ok(orders.Select(MapToDto));
    }

    public async Task<ApiResponse<OrderDto>> CreateAsync(Guid userId, CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return ApiResponse<OrderDto>.Fail("Shipping address is required.");
        if (!request.Items.Any())
            return ApiResponse<OrderDto>.Fail("Order must contain at least one item.");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            return ApiResponse<OrderDto>.Fail("User not found.");

        // Validate all products and build order items
        var orderItems = new List<OrderItem>();
        decimal total = 0;

        foreach (var itemReq in request.Items)
        {
            if (itemReq.Quantity <= 0)
                return ApiResponse<OrderDto>.Fail("Item quantity must be greater than 0.");

            var product = await _unitOfWork.Products.GetByIdAsync(itemReq.ProductId);
            if (product == null || product.IsDeleted)
                return ApiResponse<OrderDto>.Fail($"Product '{itemReq.ProductId}' not found.");
            if (product.Status != ProductStatus.Active)
                return ApiResponse<OrderDto>.Fail($"Product '{product.Name}' is not available for purchase.");
            if (product.StockQuantity < itemReq.Quantity)
                return ApiResponse<OrderDto>.Fail($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}.");

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = itemReq.Quantity,
                UnitPrice = product.Price
            });

            // Deduct stock
            product.StockQuantity -= itemReq.Quantity;
            if (product.StockQuantity == 0)
                product.Status = ProductStatus.OutOfStock;
            await _unitOfWork.Products.UpdateAsync(product);

            total += product.Price * itemReq.Quantity;
        }

        var orderNumber = await _unitOfWork.Orders.GenerateOrderNumberAsync();
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Status = OrderStatus.Pending,
            TotalAmount = total,
            ShippingAddress = request.ShippingAddress.Trim(),
            Notes = request.Notes,
            OrderItems = orderItems
        };

        await _unitOfWork.Orders.AddAsync(order);
        await _unitOfWork.SaveChangesAsync();

        var created = await _unitOfWork.Orders.GetWithDetailsAsync(order.Id);
        return ApiResponse<OrderDto>.Ok(MapToDto(created!), "Order created successfully.");
    }

    public async Task<ApiResponse<OrderDto>> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        if (order == null || order.IsDeleted)
            return ApiResponse<OrderDto>.Fail($"Order with id '{id}' not found.");

        // Validate state machine transitions
        var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
        {
            { OrderStatus.Pending, new() { OrderStatus.Confirmed, OrderStatus.Cancelled } },
            { OrderStatus.Confirmed, new() { OrderStatus.Processing, OrderStatus.Cancelled } },
            { OrderStatus.Processing, new() { OrderStatus.Shipped } },
            { OrderStatus.Shipped, new() { OrderStatus.Delivered } },
            { OrderStatus.Delivered, new() { OrderStatus.Refunded } },
        };

        if (!validTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(request.Status))
            return ApiResponse<OrderDto>.Fail($"Cannot transition from '{order.Status}' to '{request.Status}'.");

        order.Status = request.Status;
        if (request.Status == OrderStatus.Shipped) order.ShippedAt = DateTime.UtcNow;
        if (request.Status == OrderStatus.Delivered) order.DeliveredAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<OrderDto>.Ok(MapToDto(order), $"Order status updated to '{request.Status}'.");
    }

    public async Task<ApiResponse<bool>> CancelOrderAsync(Guid id, Guid userId, bool isAdmin = false)
    {
        var order = await _unitOfWork.Orders.GetWithDetailsAsync(id);
        if (order == null || order.IsDeleted)
            return ApiResponse<bool>.Fail($"Order with id '{id}' not found.");

        if (!isAdmin && order.UserId != userId)
            return ApiResponse<bool>.Fail("You are not authorized to cancel this order.");

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
            return ApiResponse<bool>.Fail($"Cannot cancel order in '{order.Status}' status.");

        // Restore stock
        foreach (var item in order.OrderItems)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
                if (product.Status == ProductStatus.OutOfStock && product.StockQuantity > 0)
                    product.Status = ProductStatus.Active;
                await _unitOfWork.Products.UpdateAsync(product);
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Orders.UpdateAsync(order);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Order cancelled successfully. Stock has been restored.");
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        UserId = o.UserId,
        CustomerName = o.User?.FullName ?? string.Empty,
        CustomerEmail = o.User?.Email ?? string.Empty,
        Status = o.Status,
        TotalAmount = o.TotalAmount,
        ShippingAddress = o.ShippingAddress,
        Notes = o.Notes,
        ShippedAt = o.ShippedAt,
        DeliveredAt = o.DeliveredAt,
        CreatedAt = o.CreatedAt,
        Items = o.OrderItems.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            ProductId = oi.ProductId,
            ProductName = oi.Product?.Name ?? string.Empty,
            ProductSKU = oi.Product?.SKU ?? string.Empty,
            ProductImageUrl = oi.Product?.ImageUrl,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            TotalPrice = oi.TotalPrice
        }).ToList()
    };
}
