﻿namespace TradingStrategy.Strategy
{
    public abstract class GeneralStopLossBase 
        : GeneralTradingStrategyComponentBase
        , IStopLossComponent
    {
        public abstract double EstimateStopLossGap(ITradingObject tradingObject, double assumedPrice, out string comments);
    }
}
