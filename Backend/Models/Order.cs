using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderApi.Enum;

namespace OrderApi.Model
{
    public class Order
    {
        
    public int Id { get; set; }

    public string Cliente { get; set; } = string.Empty;

    public string Produto { get; set; } = string.Empty;

    public decimal Valor { get; set; }
 
    public OrderStatus Status { get; set; } = OrderStatus.Pendente;

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    }
}