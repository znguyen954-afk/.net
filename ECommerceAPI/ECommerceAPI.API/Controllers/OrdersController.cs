using ECommerceAPI.Application.DTOs;
using ECommerceAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceAPI.API.Controllers;

/// <summary>
/// Order management endpoints
/// </summary>
[Tags("Orders")]
[Authorize]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Get all orders with pagination and filtering (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAll([FromQuery] OrderQueryParams query)
    {
        var result = await _orderService.GetPagedAsync(query);
        return ApiOk(result);
    }

    /// <summary>
    /// Get current user's orders
    /// </summary>
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var result = await _orderService.GetMyOrdersAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>
    /// Get an order by ID
    /// </summary>
    /// <param name="id">Order GUID</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _orderService.GetByIdAsync(id, CurrentUserId, IsAdmin);
        if (!result.Success) return result.Message.Contains("not found") ? NotFound(result) : Forbid();
        return Ok(result);
    }

    /// <summary>
    /// Create a new order for the current user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateAsync(CurrentUserId, request);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Update order status (Admin/Manager only) - follows state machine
    /// </summary>
    /// <param name="id">Order GUID</param>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request);
        if (!result.Success) return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Cancel an order (Customer can cancel their own, Admin can cancel any)
    /// </summary>
    /// <param name="id">Order GUID</param>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _orderService.CancelOrderAsync(id, CurrentUserId, IsAdmin);
        if (!result.Success) return result.Message.Contains("not found") ? NotFound(result) : BadRequest(result);
        return Ok(result);
    }
}
