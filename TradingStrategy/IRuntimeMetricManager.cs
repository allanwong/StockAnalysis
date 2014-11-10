﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StockAnalysis.Share;

namespace TradingStrategy
{
    public interface IRuntimeMetricManager
    {
        /// <summary>
        /// Register a metric
        /// </summary>
        /// <param name="metricName">metric string which can be parsed as MetricExpression, such as "ATR[20]".</param>
        /// <returns>index of metric</returns>
        int RegisterMetric(string metricName);

        /// <summary>
        /// Register a metric with creator
        /// </summary>
        /// <param name="metricName">name of metric</param>
        /// <param name="creator">creator that can create metric from metric name</param>
        /// <returns>index of metric</returns>
        int RegisterMetric(string metricName, Func<string, IRuntimeMetric> creator);

        /// <summary>
        /// Update metric for a given trading object
        /// </summary>
        /// <param name="tradingObject">trading object whose metrics need to be updated</param>
        /// <param name="bar">Bar data of trading object</param>
        void UpdateMetric(ITradingObject tradingObject, Bar bar);

        /// <summary>
        /// Get specific metric values for given trading object
        /// </summary>
        /// <param name="tradingObject">trading object whose metric values should be fetched</param>
        /// <param name="metrciIndex">index of metric returned by RegisterMetric()</param>
        /// <returns>array of double which contains current values of specific values</returns>
        double[] GetMetricValues(ITradingObject tradingObject, int metricIndex);

        /// <summary>
        /// Get specific metric for given trading object
        /// </summary>
        /// <param name="tradingObject">trading object whose metric should be fetched</param>
        /// <param name="metrciIndex">index of metric returned by RegisterMetric()</param>
        /// <returns>underlying IRuntimeMetric object</returns>
        IRuntimeMetric GetMetric(ITradingObject tradingObject, int metricIndex);

        /// <summary>
        /// Get metrics for all trading objects
        /// </summary>
        /// <param name="metricIndex">index of metric returned by RegisterMetric()</param>
        /// <returns>array of underlying IRuntimeMetric objects. It is indexed by the index of trading objects</returns>
        IRuntimeMetric[] GetMetrics(int metricIndex);
    }
}