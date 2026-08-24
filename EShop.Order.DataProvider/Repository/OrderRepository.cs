using Eshop.Product.DataProvider.Repository;
using EShop.Infrastructure.Command.Product;
using EShop.Infrastructure.Event.Product;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EShop.Order.DataProvider.Repository
{
    using EShop.Infrastructure.Order;

    public class OrderRepository : IOrderRepository
    {
        private IMongoDatabase _database;
        private IMongoCollection<Order> _collection;
        public OrderRepository(IMongoDatabase database)
        {
            _database = database;
            _collection = database.GetCollection<Order>("order", null);
        }

        public async Task<bool> CreateOrder(Order order)
        {
            await _collection.InsertOneAsync(order);
            return true;
        }
public async Task<List<Order>> GetAllOrders(string userId)
{
    return await _collection
        .Find(order => order.UserId == userId)
        .ToListAsync();
}



public async Task<Order> GetOrder(string orderId)
{
    return await _collection
        .Find(order => order.OrderId == orderId)
        .FirstOrDefaultAsync();
}
    }
}
