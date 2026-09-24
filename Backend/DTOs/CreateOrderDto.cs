using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderApi.DTOs
{
    public class CreateOrderDto
    {
        public string Cliente { get; set; } = string.Empty;

        public string Produto { get; set; } = string.Empty;

        public decimal Valor { get; set; }
    }
}