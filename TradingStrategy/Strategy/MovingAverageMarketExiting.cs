﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MetricsDefinition;
using StockAnalysis.Share;

namespace TradingStrategy.Strategy
{
    public sealed class MovingAverageMarketExiting : MovingAverageBase, IMarketExitingComponent
    {
        public override string Name
        {
            get { return "移动平均出市"; }
        }

        public override string Description
        {
            get { return "当短期平均向下交叉长期平均时出市"; }
        }

        public bool ShouldExit(ITradingObject tradingObject, out string comments)
        {
            comments = string.Empty;
            var runtimeMetric = base.MetricManager.GetOrCreateRuntimeMetric(tradingObject);

            if (runtimeMetric.ShortMA < runtimeMetric.LongMA
                && runtimeMetric.PreviousShortMA > runtimeMetric.PreviousLongMA)
            {
                comments = string.Format(
                    "prevShort:{0:0.000}; prevLong:{1:0.000}; curShort:{2:0.000}; curLong:{3:0.000}",
                    runtimeMetric.PreviousShortMA,
                    runtimeMetric.PreviousLongMA,
                    runtimeMetric.ShortMA,
                    runtimeMetric.LongMA);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
