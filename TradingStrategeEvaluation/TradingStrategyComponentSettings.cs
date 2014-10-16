﻿using System;
using System.Linq;
using TradingStrategy;
using System.Xml.Serialization;

namespace TradingStrategyEvaluation
{
    [Serializable]
    public sealed class TradingStrategyComponentSettings
    {
        [XmlAttribute]
        public bool Enabled { get; set; }
        public string ClassType { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImplementedInterfaces { get; set; }
        public ParameterSettings[] ComponentParameterSettings { get; set; }

        public static TradingStrategyComponentSettings GenerateExampleSettings(
            ITradingStrategyComponent component)
        {
            if (component == null)
            {
                throw new ArgumentNullException();
            }

            var settings = new TradingStrategyComponentSettings();

            settings.Enabled = false;
            settings.ClassType = component.GetType().AssemblyQualifiedName;
            settings.Name = component.Name;
            settings.Description = component.Description;

            var interfaces = component.GetType().GetInterfaces()
                .Where(i => typeof(ITradingStrategyComponent).IsAssignableFrom(i))
                .Select(i => i.Name);

            settings.ImplementedInterfaces = string.Join(";", interfaces);

            settings.ComponentParameterSettings = ParameterHelper.GetParameterAttributes(component)
                .Select(p => ParameterSettings.GenerateExampleSettings(p))
                .ToArray();

            return settings;
        }
    }
}
