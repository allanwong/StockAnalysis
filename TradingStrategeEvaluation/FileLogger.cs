﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using TradingStrategy;

namespace TradingStrategyEvaluation
{
    public sealed class FileLogger : ILogger, IDisposable
    {
        private StreamWriter _writer;
        private bool _flushForEachLog;

        public FileLogger(string file, bool flushForEachLog = false)
        {
            _writer = new StreamWriter(file, false, Encoding.UTF8);
            _flushForEachLog = flushForEachLog;
        }

        public void Log(string log)
        {
            _writer.WriteLine(log);

            if (_flushForEachLog)
            {
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Flush();

                _writer.Dispose();
                _writer = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
