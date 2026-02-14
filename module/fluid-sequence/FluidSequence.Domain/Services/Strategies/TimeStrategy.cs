using System;
using System.Collections.Generic;
using FluidSequence.Domain.Entities;
using System.Globalization;

namespace FluidSequence.Domain.Services.Strategies
{
    public class TimeStrategy : IPlaceholderStrategy
    {
        public bool CanHandle(string key)
        {
            return key == "yyyy" || key == "yy" || key == "MM" || key == "dd" 
                || key == "HH" || key == "mm" || key == "ss" 
                || key == "ww" || key == "QQ" || key == "FY";
        }

        public string Handle(string key, SysSequenceRule rule, Dictionary<string, string> context)
        {
            var now = DateTime.Now;
            switch (key)
            {
                case "yyyy": return now.ToString("yyyy");
                case "yy": return now.ToString("yy");
                case "MM": return now.ToString("MM");
                case "dd": return now.ToString("dd");
                case "HH": return now.ToString("HH");
                case "mm": return now.ToString("mm");
                case "ss": return now.ToString("ss");
                case "ww": 
                    return ISOWeek.GetWeekOfYear(now).ToString("D2");
                case "QQ":
                    int q = (now.Month - 1) / 3 + 1;
                    return $"Q{q}";
                case "FY":
                   int startMonth = 1;
                   if (rule.ExtensionProps != null && rule.ExtensionProps.ContainsKey("FiscalYearStartMonth"))
                   {
                       startMonth = Convert.ToInt32(rule.ExtensionProps["FiscalYearStartMonth"]);
                   }
                   int fy = now.Year;
                   if (now.Month < startMonth) fy--;
                   return fy.ToString();
            }
            return key;
        }
    }
}
