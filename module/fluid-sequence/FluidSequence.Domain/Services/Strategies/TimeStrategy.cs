using System;
using System.Collections.Generic;
using FluidSequence.Domain.Entities;
using System.Globalization;

namespace FluidSequence.Domain.Services.Strategies
{
    public class TimeStrategy : IPlaceholderStrategy
    {
        public bool CanHandle(string placeholderKey)
        {
            return placeholderKey == "yyyy" || placeholderKey == "yy" || placeholderKey == "MM" || placeholderKey == "dd" 
                || placeholderKey == "HH" || placeholderKey == "mm" || placeholderKey == "ss" 
                || placeholderKey == "ww" || placeholderKey == "QQ" || placeholderKey == "FY";
        }

        public string Handle(string placeholderKey, SysSequenceRule rule, Dictionary<string, string> context)
        {
            var now = DateTime.Now;
            switch (placeholderKey)
            {
                case "yyyy": return now.ToString("yyyy", CultureInfo.InvariantCulture);
                case "yy": return now.ToString("yy", CultureInfo.InvariantCulture);
                case "MM": return now.ToString("MM", CultureInfo.InvariantCulture);
                case "dd": return now.ToString("dd", CultureInfo.InvariantCulture);
                case "HH": return now.ToString("HH", CultureInfo.InvariantCulture);
                case "mm": return now.ToString("mm", CultureInfo.InvariantCulture);
                case "ss": return now.ToString("ss", CultureInfo.InvariantCulture);
                case "ww": 
                    return ISOWeek.GetWeekOfYear(now).ToString("D2", CultureInfo.InvariantCulture);
                case "QQ":
                    int q = (now.Month - 1) / 3 + 1;
                    return $"Q{q}";
                case "FY":
                   int startMonth = 1;
                   if (rule.ExtensionProps != null && rule.ExtensionProps.TryGetValue("FiscalYearStartMonth", out var startMonthObj))
                   {
                       startMonth = Convert.ToInt32(startMonthObj, CultureInfo.InvariantCulture);
                   }
                   int fy = now.Year;
                   if (now.Month < startMonth) fy--;
                   return fy.ToString(CultureInfo.InvariantCulture);
            }
            return placeholderKey;
        }
    }
}
