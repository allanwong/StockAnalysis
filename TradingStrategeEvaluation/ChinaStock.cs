﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TradingStrategy;

namespace TradingStrategyEvaluation
{
    public sealed class ChinaStock : ITradingObject
    {
        public int Index { get; private set; }

        public string Code { get; private set; }

        public string Name { get; private set; }

        public int VolumePerHand { get; private set; }

        public int VolumePerBuyingUnit { get; private set; }

        public int VolumePerSellingUnit { get; private set; }

        public double MinPriceUnit { get; private set; }

        public ChinaStock(int index, string code, string name)
            : this(index, code, name, 100, 100, 1, 0.01)
        {
        }

        public ChinaStock(
            int index,
            string code, 
            string name, 
            int volumePerHand,
            int volumePerBuyingUnit,
            int volumePerSellingUnit, 
            double minPriceUnit)
        {
            Index = index;
            Code = code;
            Name = name;
            VolumePerHand = volumePerHand;
            VolumePerBuyingUnit = volumePerBuyingUnit;
            VolumePerSellingUnit = volumePerSellingUnit;
            MinPriceUnit = minPriceUnit;
        }
    }
}
