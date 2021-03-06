﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTrading.Utility
{
    public sealed class DispatchedOrder
    {
        public int OrderNo { get; set; }

        public int SucceededVolume { get; set; }

        public OrderStatus LastStatus { get; set; }

        public OrderRequest Request { get; set; }
    }
}
