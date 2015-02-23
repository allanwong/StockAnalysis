﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MetricsDefinition.Metrics;

namespace TradingStrategy.Strategy
{
    public sealed class MovingAverageTrendDetector
    {
        private int[] _periods;
        private RuntimeMetricProxy[] _movingAverages;
        private RuntimeMetricProxy[] _trendDetectors;

        public int PeriodsCount 
        {
            get { return _periods.Length; }
        }

        public int GetPeriod(int index)
        {
            return _periods[index];
        }

        public double GetMovingAverage(ITradingObject tradingObject, int index)
        {
            return _movingAverages[index].GetMetricValues(tradingObject)[0];
        }

        public MovingAverageTrendDetector(IRuntimeMetricManager manager, IEnumerable<int> periods)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (periods == null)
            {
                throw new ArgumentNullException("periods");
            }

            var dedupPeriods = periods.GroupBy(i => i);

            if (dedupPeriods.Count() != periods.Count())
            {
                throw new ArgumentException("Duplicated periods are found");
            }

            if (periods.Count() == 1)
            {
                throw new ArgumentException("Need at least 2 different MA periods for trend detection");
            }

            _periods = periods.OrderBy(i => i).ToArray();

            _movingAverages = _periods
                .Select(i => new RuntimeMetricProxy(manager, string.Format("MA[{0}]", i)))
                .ToArray();

            _trendDetectors = _periods
                .Select(i => new RuntimeMetricProxy(manager, string.Format("TD[5](MA[{0}])", i)))
                .ToArray();
        }

        public bool HasTrend(ITradingObject tradingObject)
        {
            bool hasTrend = true;

            foreach (var td in _trendDetectors)
            {
                if (td.GetMetricValues(tradingObject)[0] <= 0.0)
                {
                    hasTrend = false;
                    break;
                }
            }

            if (!hasTrend)
            {
                return false;
            }

            double lastValue = GetMovingAverage(tradingObject, 0);
            for (int i = 1; i < _movingAverages.Length; ++i)
            {
                double nextValue = GetMovingAverage(tradingObject, i);
                if (lastValue < nextValue)
                {
                    hasTrend = false;
                    break;
                }

                lastValue = nextValue;
            }

            return hasTrend;
        }
    }
}
