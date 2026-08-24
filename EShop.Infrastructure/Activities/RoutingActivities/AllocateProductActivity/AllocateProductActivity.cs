using EShop.Infrastructure.Command.Inventory;
using MassTransit;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EShop.Infrastructure.Activities.RoutingActivities.AllocateProductActivity
{
    public class AllocateProductActivity : IActivity<AllocateProduct, OrderLog>
    {
        public async Task<CompensationResult> Compensate(CompensateContext<OrderLog> context)
        {
            try
            {
                var endpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/allocate_product"));
                var allocateProduct = JsonSerializer.Deserialize<AllocateProduct>(context.Message.Variables["PlacedOrder"].ToString());
                await endpoint.Send(allocateProduct);

                return context.Compensated();
            }
            catch (Exception)
            {
                return context.Failed();
            }
        }

        public async Task<ExecutionResult> Execute(ExecuteContext<AllocateProduct> context)
        {
            try
            {
            var endpoint = await context.GetSendEndpoint(new Uri("rabbitmq://localhost/release_product"));
            var order = JsonSerializer.Deserialize<ReleaseProduct>(context.Message.Variables["PlacedOrder"].ToString());

            await endpoint.Send(order);

            return context.CompletedWithVariables<ReleaseProduct>(order, new { });
            }
            catch (Exception)
            {
                return context.Faulted();
            }
        }
    }

    public class OrderLog
    {
        public Order.Order Order { get; set; }
        public string Message { get; set; }
    }
}
