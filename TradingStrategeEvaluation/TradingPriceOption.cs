﻿using System;

namespace TradingStrategyEvaluation
{
    [Serializable]
    [Flags]
    public enum TradingPriceOption
    {
        CurrentPeriod = 0x1,
        NextPeriod = 0x2,

        OpenPrice = 0x4,
        MiddlePrice = 0x8,
        ClosePrice = 0x10,
        HighestPrice = 0x20,
        LowestPrice = 0x40,
        
        CurrentMiddlePrice = CurrentPeriod | MiddlePrice,
        CurrentHighestPrice = CurrentPeriod | HighestPrice,
        CurrentOpenPrice = CurrentPeriod | OpenPrice,
        CurrentClosePrice = CurrentPeriod | ClosePrice,
        CurrentLowestPrice = CurrentPeriod | LowestPrice,

        NextMiddlePrice = NextPeriod | MiddlePrice,
        NextHighestPrice = NextPeriod | HighestPrice,
        NextOpenPrice = NextPeriod | OpenPrice,
        NextClosePrice = NextPeriod | ClosePrice,
        NextLowestPrice = NextPeriod | LowestPrice,
    }
}
