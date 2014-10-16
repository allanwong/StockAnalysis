﻿using System;
using System.Xml.Serialization;
using System.IO;

namespace TradingStrategyEvaluation
{
    [Serializable]
    public sealed class TradingSettings
    {
        public CommissionSettings BuyingCommission { get; set; }

        public CommissionSettings SellingCommission { get; set; }

        public int Spread { get; set; }

//        public TradingPriceOption BuyShortPriceOption { get; set; }
//        public TradingPriceOption CloseShortPriceOption { get; set; }

        public TradingPriceOption OpenLongPriceOption { get; set; }
        public TradingPriceOption CloseLongPriceOption { get; set; }

        public static TradingSettings LoadFromFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException();
            }

            TradingSettings settings;

            var serializer = new XmlSerializer(typeof(TradingSettings));

            using (var reader = new StreamReader(file))
            {
                settings = (TradingSettings)serializer.Deserialize(reader);
            }

            if (settings.BuyingCommission.Type != settings.SellingCommission.Type)
            {
                throw new InvalidDataException("Commission types of buying and selling are different");
            }

            return settings;
        }

        public void SaveToFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException();
            }

            var serializer = new XmlSerializer(typeof(TradingSettings));

            using (var writer = new StreamWriter(file))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static TradingSettings GenerateExampleSettings()
        {
            var settings = new TradingSettings();

            settings.BuyingCommission = new CommissionSettings();
            settings.BuyingCommission.Type = CommissionSettings.CommissionType.ByAmount;
            settings.BuyingCommission.Tariff = 0.0005;

            settings.SellingCommission = new CommissionSettings();
            settings.SellingCommission.Type = CommissionSettings.CommissionType.ByAmount;
            settings.SellingCommission.Tariff = 0.0005;

            settings.Spread = 0;

            settings.OpenLongPriceOption = TradingPriceOption.NextOpenPrice;
            settings.CloseLongPriceOption = TradingPriceOption.NextOpenPrice;

            return settings;
        }
    }
}