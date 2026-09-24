using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderApi.Services
{
    public interface IServiceBusService 
    {
        Task PublicarPedidoAsync(int orderId);
    }
}