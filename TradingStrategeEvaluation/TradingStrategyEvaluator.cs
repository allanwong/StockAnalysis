﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StockAnalysis.Share;
using TradingStrategy;

namespace TradingStrategyEvaluation
{
    public sealed class TradingStrategyEvaluator
    {
        private ITradingStrategy _strategy;
        private IDictionary<ParameterAttribute, object> _strategyParameterValues;
        private ITradingDataProvider _provider;
        private EquityManager _equityManager;
        private StandardEvaluationContext _context;
        private TradingSettings _settings;
        private TradingTracker _tradingTracker = null;
        private ITradingObject[] _allTradingObjects = null;
        private List<Instruction> _pendingInstructions = new List<Instruction>();

        private bool _evaluatable = true;

        public TradingTracker Tracker
        {
            get { return _tradingTracker; }
        }

        public IEnumerable<Position> ClosedPositions
        {
            get { return _equityManager.ClosedPositions; }
        }

        public EventHandler<EvaluationProgressEventArgs> OnEvaluationProgress = null;

        public TradingStrategyEvaluator(
            double initalCapital, 
            ITradingStrategy strategy, 
            IDictionary<ParameterAttribute, object> strategyParameters, 
            ITradingDataProvider provider, 
            TradingSettings settings,
            ILogger logger)
        {
            if (strategy == null || provider == null || settings == null)
            {
                throw new ArgumentNullException();
            }

            _strategy = strategy;
            _strategyParameterValues = strategyParameters;

            _provider = provider;
           
            _settings = settings;

            _equityManager = new EquityManager(initalCapital);
            _context = new StandardEvaluationContext(_provider, _equityManager, logger);
            _tradingTracker = new TradingTracker(initalCapital);
        }

        public void Evaluate()
        {
            if (!_evaluatable)
            {
                throw new InvalidOperationException("Evalute() can be called only once");
            }

            // initialize context
            _strategy.Initialize(_context, _strategyParameterValues);

            // Get all trading objects
            _allTradingObjects = _provider.GetAllTradingObjects();
            Action<ITradingObject> warmupAction = 
                (ITradingObject obj) =>
                {
                    var warmupData = _provider.GetWarmUpData(obj.Index);
                    if (warmupData != null)
                    {
                        foreach (var bar in warmupData)
                        {
                            _strategy.WarmUp(obj, bar);
                        }
                    }
                };

            // warm up
            foreach (var tradingObject in _allTradingObjects)
            {
                warmupAction(tradingObject);
            }

            // evaluating
            DateTime lastPeriodTime = DateTime.MinValue;
            DateTime[] periods = _provider.GetAllPeriodsOrdered();
            for (int periodIndex = 0; periodIndex < periods.Length; ++periodIndex)
            {
                DateTime thisPeriodTime = periods[periodIndex];
                Bar[] thisPeriodData = _provider.GetDataOfPeriod(thisPeriodTime);

                if (thisPeriodData.Length != _allTradingObjects.Length)
                {
                    throw new InvalidOperationException("the number of data returned does not match the number of trading object");
                }
                
                // start a new period
                _strategy.StartPeriod(thisPeriodTime);
                
                // run pending instructions left over from previous period
                RunPendingInstructions(thisPeriodData, thisPeriodTime, false);

                // check data
                for (int i = 0; i < thisPeriodData.Length; ++i)
                {
                    Bar bar = thisPeriodData[i];

                    if (bar.Time != Bar.InvalidTime && bar.Time != thisPeriodTime)
                    {
                        throw new InvalidOperationException("Time in bar data is different with the time returned by data provider");
                    }
                }

                // evaluate bar data
                _strategy.Evaluate(_allTradingObjects, thisPeriodData);


                // get instructions and add them to pending instruction list
                var instructions = _strategy.RetrieveInstructions();

                if (instructions != null && instructions.Count() > 0)
                {
                    _pendingInstructions.AddRange(instructions);

                    // run instructions for current period
                    RunPendingInstructions(thisPeriodData, thisPeriodTime, true);
                }

                // end period
                _strategy.EndPeriod();

                // update last period time
                lastPeriodTime = thisPeriodTime;

                // update progress event
                if (OnEvaluationProgress != null)
                {
                    OnEvaluationProgress(
                        this, 
                        new EvaluationProgressEventArgs(
                            thisPeriodTime, 
                            (double)(periodIndex + 1) / periods.Length));
                }
            }

            // finish evaluation
            _strategy.Finish();

            // Sell all equities forcibly.
            ClearEquityForcibly(lastPeriodTime);

            // clear all pending instructions.
            _pendingInstructions.Clear();

            // update 'evaluatable' flag to avoid this function be called twice
            _evaluatable = false;
        }

        private void ClearEquityForcibly(DateTime lastPeriodTime)
        {
            var codes = _equityManager.GetAllPositionCodes();
            foreach (var code in codes)
            {
                var equities = _equityManager.GetPositionDetails(code);
                int totalVolume = equities.Sum(e => e.Volume);

                if (totalVolume <= 0)
                {
                    throw new InvalidOperationException("total volume should be greater than zero, logic error");
                }

                Bar bar;
                int index = _provider.GetIndexOfTradingObject(code);

                if (!_provider.GetLastEffectiveBar(index, lastPeriodTime, out bar))
                {
                    throw new InvalidOperationException(
                        string.Format("failed to get last data for code {0}, logic error", code));
                }

                Transaction transaction = new Transaction()
                {
                    Action = TradingAction.CloseLong,
                    Commission = 0.0,
                    ExecutionTime = lastPeriodTime,
                    InstructionId = long.MaxValue,
                    Code = code,
                    Price = bar.ClosePrice,
                    SellingType = SellingType.ByVolume,
                    Succeeded = false,
                    SubmissionTime = lastPeriodTime,
                    Volume = totalVolume,
                    Comments = "clear forcibly"
                };

                UpdateTransactionCommission(transaction, _allTradingObjects[index]);

                if (!ExecuteTransaction(transaction, false))
                {
                    throw new InvalidOperationException(
                        string.Format("failed to execute transaction, logic error", code));
                }
            }
        }

        private void RunPendingInstructions(
            Bar[] tradingData, 
            DateTime time, 
            bool forCurrentPeriod)
        {
            Dictionary<int, Transaction> pendingTansactions = new Dictionary<int, Transaction>();

            for (int i = 0; i < _pendingInstructions.Count; ++i)
            {
                var instruction = _pendingInstructions[i];

                TradingPriceOption option;
                if (instruction.Action == TradingAction.OpenLong)
                {
                    option = _settings.OpenLongPriceOption;
                }
                else if (instruction.Action == TradingAction.CloseLong)
                {
                    option = _settings.CloseLongPriceOption;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format("unsupported action {0}", instruction.Action));
                }

                if (forCurrentPeriod)
                {
                    if (!option.HasFlag(TradingPriceOption.CurrentPeriod))
                    {
                        continue;
                    }
                }
                else
                {
                    if (option.HasFlag(TradingPriceOption.CurrentPeriod))
                    {
                        throw new InvalidProgramException("Logic error, all transaction expect to be executed in today should have been fully executed");
                    }
                }

                int tradingObjectIndex = instruction.TradingObject.Index;
                if (tradingData[tradingObjectIndex].Time == Bar.InvalidTime)
                {
                    if (forCurrentPeriod)
                    {
                        throw new InvalidOperationException(
                            string.Format("the data for trading object {0} is invalid", instruction.TradingObject.Code));
                    }
                    else
                    {
                        continue;
                    }
                }

                Transaction transaction = BuildTransactionFromInstruction(
                    instruction,
                    time,
                    tradingData[tradingObjectIndex]);

                pendingTansactions.Add(i, transaction);
            }

            var orderedTransactions = pendingTansactions.Values
                .OrderBy(t => t, new Transaction.DefaultComparer());

            // execute the close long transaction firstly
            foreach (var transaction in orderedTransactions)
            {
                // execute transaction
                ExecuteTransaction(transaction, true);
            }

            foreach (var index in pendingTansactions.Keys)
            {
                // remove instruction that has been executed
                _pendingInstructions[index] = null;
            }

            // compact pending instruction list
            _pendingInstructions = _pendingInstructions.Where(i => i != null).ToList();
        }

        private bool ExecuteTransaction(Transaction transaction, bool notifyTransactionStatus)
        {
            string error;

            CompletedTransaction completedTransaction = null;
            bool succeeded = _equityManager.ExecuteTransaction(
                transaction,
                true,
                out completedTransaction,
                out error);

            transaction.Succeeded = succeeded;
            transaction.Error = error;

            if (notifyTransactionStatus)
            {
                // notify transaction status
                _strategy.NotifyTransactionStatus(transaction);
            }

            // add to history
            _tradingTracker.AddTransaction(transaction);
            if (completedTransaction != null)
            {
                _tradingTracker.AddCompletedTransaction(completedTransaction);
            }

            // log transaction
            _context.Log(transaction.Print());

            return succeeded;
        }

        private Transaction BuildTransactionFromInstruction(Instruction instruction, DateTime time, Bar bar)
        {
            if (time != bar.Time)
            {
                throw new ArgumentException("Inconsistent time in bar and time parameters");
            }

            if ((instruction.Action == TradingAction.OpenLong 
                    && instruction.Volume % instruction.TradingObject.VolumePerBuyingUnit != 0)
                || (instruction.Action == TradingAction.CloseLong
                    && instruction.Volume % instruction.TradingObject.VolumePerSellingUnit != 0))
            {
                throw new InvalidOperationException("The volume of transaction does not meet trading object's requirement");
            }

            Transaction transaction = new Transaction()
            {
                Action = instruction.Action,
                Commission = 0.0,
                ExecutionTime = time,
                InstructionId = instruction.ID,
                Code = instruction.TradingObject.Code,
                Price = CalculateTransactionPrice(bar, instruction),
                Succeeded = false,
                SubmissionTime = instruction.SubmissionTime,
                Volume = instruction.Volume,
                SellingType = instruction.SellingType,
                StopLossPriceForSell = instruction.StopLossPriceForSell,
                PositionIdForSell = instruction.PositionIdForSell,
                Comments = instruction.Comments,
            };

            // update commission
            UpdateTransactionCommission(transaction, instruction.TradingObject);

            return transaction;
        }

        private double CalculateTransactionPrice(Bar bar, Instruction instruction)
        {
            double price;

            TradingPriceOption option;
            if (instruction.Action == TradingAction.OpenLong)
            {
                option = _settings.OpenLongPriceOption;
            }
            else if (instruction.Action == TradingAction.CloseLong)
            {
                option = _settings.CloseLongPriceOption;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("unsupported action {0}", instruction.Action));
            }

            if (option.HasFlag(TradingPriceOption.OpenPrice))
            {
                price = bar.OpenPrice;
            }
            else if (option.HasFlag(TradingPriceOption.ClosePrice))
            {
                price = bar.ClosePrice;
            }
            else if (option.HasFlag(TradingPriceOption.MiddlePrice))
            {
                price = (bar.OpenPrice + bar.ClosePrice + bar.HighestPrice + bar.LowestPrice ) / 4;
            }
            else if (option.HasFlag(TradingPriceOption.HighestPrice))
            {
                price = bar.HighestPrice;
            }
            else if (option.HasFlag(TradingPriceOption.LowestPrice))
            {
                price = bar.LowestPrice;
            }
            else
            {
                throw new InvalidProgramException("Logic error");
            }

            // count the spread now
            if (instruction.Action == TradingAction.OpenLong)
            {
                price += _settings.Spread * instruction.TradingObject.MinPriceUnit;
            }
            else if (instruction.Action == TradingAction.CloseLong)
            {
                price -= _settings.Spread * instruction.TradingObject.MinPriceUnit;

                if (price < instruction.TradingObject.MinPriceUnit)
                {
                    price = instruction.TradingObject.MinPriceUnit;
                }
            }

            return price;
        }

        private void UpdateTransactionCommission(Transaction transaction, ITradingObject tradingObject)
        {
            if (tradingObject.Code != transaction.Code)
            {
                throw new ArgumentException("Code in transaction and trading object are different");
            }

            CommissionSettings commission;

            if (transaction.Action == TradingAction.OpenLong)
            {
                commission = _settings.BuyingCommission;
            }
            else if (transaction.Action == TradingAction.CloseLong)
            {
                commission = _settings.SellingCommission;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("unsupported action {0}", transaction.Action));
            }

            if (commission.Type == CommissionSettings.CommissionType.ByAmount)
            {
                transaction.Commission = transaction.Price * transaction.Volume * commission.Tariff;
            }
            else if (commission.Type == CommissionSettings.CommissionType.ByVolume)
            {
                double hands = Math.Ceiling((double)transaction.Volume / tradingObject.VolumePerHand);

                transaction.Commission = commission.Tariff * hands;
            }

            transaction.Commission = (double)((long)(transaction.Commission * 10000.0) / 10000L);
        }
    }
}
