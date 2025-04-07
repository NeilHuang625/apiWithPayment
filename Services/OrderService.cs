using cakeshop_api.Hubs;
using cakeshop_api.Models;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

namespace cakeshop_api.Services
{
    public class OrderService
    {
        private readonly IMongoCollection<Order> _orderCollection;
        private readonly IHubContext<OrderHub> _hubContext;

        public OrderService(IMongoDatabase database, IHubContext<OrderHub> hubContext)
        {
            _orderCollection = database.GetCollection<Order>("Orders");
            _hubContext = hubContext;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            await _orderCollection.InsertOneAsync(order);

            // Notify all clients about the new order
            await _hubContext.Clients.All.SendAsync("NewOrder", order);

            return order;
        }

        public async Task<List<Order>> GetOrdersByUserAsync(string userId)
        {
            return await _orderCollection.Find(o => o.UserId == userId).ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orderCollection.Find(o => true).ToListAsync();
        }

        public async Task<bool> DeleteOrderAsync(string id, string userRole)
        {
            var order = await _orderCollection.Find(o => o.Id == id).FirstOrDefaultAsync();
            // Only admin can delete confirmed or completed orders
            if (order.OrderStatus == "Confirmed" || order.OrderStatus == "Completed")
            {
                if (userRole != "admin")
                {
                    return false;
                }
            }

            var result = await _orderCollection.DeleteOneAsync(o => o.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            var result = await _orderCollection.ReplaceOneAsync(o => o.Id == order.Id, order);
            return result.ModifiedCount > 0;
        }
    }
}