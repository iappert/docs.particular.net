﻿using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using NServiceBus;
using NServiceBus.Logging;

#region UseHeader
public class ShipOrderHandler :
    IHandleMessages<ShipOrder>
{
    public Task Handle(ShipOrder message, IMessageHandlerContext context)
    {
        Log.Info($"Order Shipped. OrderId {message.OrderId}");

        var orderShippingInformation = StoreOrderShippingInformation(message, context);

        var options = new PublishOptions();
        options.SetHeader("Sample.CosmosDB.Transaction.OrderId", message.OrderId.ToString());

        return context.Publish(new OrderShipped { OrderId = orderShippingInformation.OrderId, ShippingDate = orderShippingInformation.ShippedAt }, options);
    }

    private static OrderShippingInformation StoreOrderShippingInformation(ShipOrder message, IMessageHandlerContext context)
    {
        var transactionalBatch = context.SynchronizedStorageSession.GetSharedTransactionalBatch();
        var requestOptions = new TransactionalBatchItemRequestOptions
        {
            EnableContentResponseOnWrite = false,
        };

        var orderShippingInformation = new OrderShippingInformation
        {
            Id = Guid.NewGuid(),
            OrderId = message.OrderId,
            ShippedAt = DateTimeOffset.UtcNow
        };
        transactionalBatch.CreateItem(orderShippingInformation, requestOptions);
        return orderShippingInformation;
    }

    static ILog Log = LogManager.GetLogger<ShipOrderHandler>();
}

#endregion