using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace EShop.Cart.DataProvider.Repository
{
    public class CartRepository : ICartRepository
    {
        private IDistributedCache _distributedCache;
        public CartRepository(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }
        public async Task<bool> AddCart(Infrastructure.Cart.Cart cart)
        {
            try
            {
                await _distributedCache.SetStringAsync(cart.UserId, JsonSerializer.Serialize(cart));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Infrastructure.Cart.Cart> GetCart(string UserId)
        {
            var existingCart = await _distributedCache.GetStringAsync(UserId);

            if (string.IsNullOrEmpty(existingCart))
                return new Infrastructure.Cart.Cart();

            return JsonSerializer.Deserialize<Infrastructure.Cart.Cart>(existingCart);
        }

        public async Task<bool> RemoveCart(string UserId)
        {
            await _distributedCache.RemoveAsync(UserId);
            return true;
        }
    }
}
