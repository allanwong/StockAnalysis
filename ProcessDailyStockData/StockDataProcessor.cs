﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using StockAnalysis.Share;

namespace ProcessDailyStockData
{
    sealed class StockDataProcessor : IDataProcessor
    {
        public void ConvertToCsvFile(TradingObjectName name, string inputFile, string outputFile, DateTime startDate, DateTime endDate)
        {
            if (string.IsNullOrWhiteSpace(inputFile) || string.IsNullOrWhiteSpace(outputFile))
            {
                throw new ArgumentNullException();
            }

            var lines = File.ReadAllLines(inputFile, Encoding.GetEncoding("GB2312"));

            using (var outputter = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                const string header = "code,date,open,highest,lowest,close,volume,amount";
                const int indexOfVolume = 6;

                outputter.WriteLine(header);

                var fields = header.Split(new[] { ',' });
                var expectedFieldCount = fields.Length - 1; // remove the first column 'code' which does not exists in input file

                for (var i = 2; i < lines.Length - 1; ++i)
                {
                    lines[i] = lines[i].Trim();
                    fields = lines[i].Split(new[] { ',' });
                    if (fields.Length == expectedFieldCount)
                    {
                        // the first field is date
                        DateTime date;

                        if (!DateTime.TryParse(fields[0], out date))
                        {
                            continue;
                        }

                        if (date < startDate || date > endDate)
                        {
                            continue;
                        }

                        int volume;

                        if (int.TryParse(fields[indexOfVolume], out volume))
                        {
                            if (volume == 0)
                            {
                                continue;
                            }
                        }

                        outputter.WriteLine("{0},{1}", name.CanonicalCode, lines[i]);
                    }
                }
            }
        }

        public int GetColumnIndexOfDateInCsvFile()
        {
            return 1;
        }

        public TradingObjectName GetName(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException();
            }

            var lines = File.ReadAllLines(file, Encoding.GetEncoding("GB2312"));

            // in general the file contains at least 3 lines, 2 lines of header and at least 1 line of data.
            if (lines.Length <= 2)
            {
                Console.WriteLine("Input {0} contains less than 3 lines, ignore it", file);

                return null;
            }

            // first line contains the stock code, name(can include ' '), '日线', '前复权'
            var fields = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4)
            {
                Console.WriteLine("Invalid first line in file {0}", file);

                return null;
            }

            var codeFromFileName = ExtractCodeFromFileName(file);

            var code = fields[0];
            var name = string.Concat(fields.Skip(1).Take(fields.Length - 3));

            if (codeFromFileName.Contains(code))
            {
                code = codeFromFileName;
            }

            var stockName = new StockName(code, name);

            return stockName;
        }

        static string ExtractCodeFromFileName(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException();
            }

            var fileNameStub = Path.GetFileNameWithoutExtension(file);

            return fileNameStub;
        }
    }
}