using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SELENE_STUDIO.Data;
using SELENE_STUDIO.Models;

public class OrderCancellationService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OrderCancellationService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    // Define a method to check and cancel orders
    public async Task CheckAndCancelOrders()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LogAppDbContext>();

        var currentTime = DateTime.UtcNow;

        // Orders in "Accepted" state with pending payment and without proof of payment
        var ordersToCancelAccepted = await dbContext.Orders
            .Where(o => o.OrderStatus == OrderStatus.Accepted &&
                        o.PaymentStatus == PaymentStatus.Pending && // Payment status is pending
                        o.Date.AddHours(24) < currentTime && // Check if 24 hours have passed
                        string.IsNullOrEmpty(o.UploadedImagePath)) // Check if proof of payment is not uploaded
            .ToListAsync();

        // Orders in "Processing" state that haven't progressed to "Accepted" in 3 days
        var ordersToCancelProcessing = await dbContext.Orders
            .Where(o => o.OrderStatus == OrderStatus.Pending &&
                        o.Date.AddDays(7) < currentTime) // Check if 3 days have passed
            .ToListAsync();

        foreach (var order in ordersToCancelAccepted)
        {
            if (order.OrderStatus != OrderStatus.Shipped && order.OrderStatus != OrderStatus.Completed)
            {
                order.OrderStatus = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Unpaid;
                dbContext.Orders.Update(order);
            }
        }

        foreach (var order in ordersToCancelProcessing)
        {
            order.OrderStatus = OrderStatus.Cancelled;
            dbContext.Orders.Update(order);
        }

        await dbContext.SaveChangesAsync();
    }


    // Add a scheduled task to execute the method periodically
    public void ExecuteScheduledTask()
    {
        var timer = new Timer(async _ =>
        {
            await CheckAndCancelOrders();
        }, null, TimeSpan.Zero, TimeSpan.FromHours(1)); // Run every hour
    }
}
