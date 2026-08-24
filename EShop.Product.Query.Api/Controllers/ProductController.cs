using System.Threading.Tasks;
using Eshop.Product.DataProvider.Service;
using EShop.Infrastructure.Event.Product;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace EShop.Product.Query.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult<ProductCreated>> GetProduct(string productId)
        {
            if (!ObjectId.TryParse(productId, out _))
                return BadRequest(new { message = "Product ID must be a 24-character hexadecimal MongoDB ObjectId." });

            var product = await _productService.GetProduct(productId);

            if (product is null)
                return NotFound(new { message = "Product was not found." });
            return Ok(product);
        }
    }
}