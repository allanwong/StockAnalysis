﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockAnalysis.Share;

namespace TradingStrategy
{
    public sealed class Transaction
    {
        public long InstructionId { get; set; }

        public bool Succeeded { get; set; }

        public DateTime SubmissionTime { get; set; }

        public DateTime ExecutionTime { get; set; }

        public string Code { get; set; }

        public TradingAction Action { get; set; }

        public int Volume { get; set; }

        public double Price { get; set; }

        /// <summary>
        /// the stop loss price for sell, all positions that has stop loss price higher than this should be sold.
        /// this field is used only when Action is CloseLong.
        /// </summary>
        public double StopLossPriceForSell { get; set; }

        public double Commission { get; set; }

        public string Error { get; set; }

        public string Comments { get; set; }

        public class DefaultComparer : IComparer<Transaction>
        {
                 
            public int Compare(Transaction x, Transaction y)
            {
 	            if (x.ExecutionTime != y.ExecutionTime)
                {
                    return x.ExecutionTime.CompareTo(y.ExecutionTime);
                }

                if (x.SubmissionTime != y.SubmissionTime)
                {
                    return x.SubmissionTime.CompareTo(y.SubmissionTime);
                }

                if (x.Code != y.Code)
                {
                    return x.Code.CompareTo(y.Code);
                }

                if (x.InstructionId != y.InstructionId)
                {
                    return x.InstructionId.CompareTo(y.InstructionId);
                }

                return 0;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "{0},{1:yyyy-MM-dd HH:mm:ss},{2:yyyy-MM-dd HH:mm:ss}, {3}, {4}, {5:0.00}, {6:0.00}, {7:0.00}, {8}, {9},{10}",
                InstructionId,
                SubmissionTime,
                ExecutionTime,
                (int)Action,
                Code,
                Price,
                Volume,
                Commission,
                Succeeded,
                Error.Escape(),
                Comments.Escape());
        }

        public static Transaction Parse(string s)
        {
            string[] fields = s.Split(new char[] { ',' });
            if (fields.Length != 11)
            {
                // there should be 11 fields
                throw new FormatException(
                    string.Format("Data format error: there is no enough fields in \"{0}\"", s));

            }


            Transaction transaction = new Transaction()
            {
                InstructionId = long.Parse(fields[0]),
                SubmissionTime = DateTime.Parse(fields[1]),
                ExecutionTime = DateTime.Parse(fields[2]),
                Action = (TradingAction)int.Parse(fields[3]),
                Code = fields[4],
                Price = double.Parse(fields[5]),
                Volume = int.Parse(fields[6]),
                Commission = double.Parse(fields[7]),
                Succeeded = bool.Parse(fields[8]),
                Error = fields[9].Unescape(),
                Comments = fields[10].Unescape()
            };

            return transaction;
        }
    }
}
