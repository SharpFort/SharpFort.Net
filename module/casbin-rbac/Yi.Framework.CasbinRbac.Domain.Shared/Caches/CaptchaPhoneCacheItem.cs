using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yi.Framework.CasbinRbac.Domain.Shared.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Shared.Caches
{
    public class CaptchaPhoneCacheItem
    {
        public CaptchaPhoneCacheItem(string code) { Code = code; }
        public string Code { get; set; }
    }

    public class CaptchaPhoneCacheKey
    {
        public CaptchaPhoneCacheKey(PhoneValidationType validationPhoneType,string phone) { Phone = phone;
            PhoneValidationType = validationPhoneType;
        }
        public PhoneValidationType PhoneValidationType { get; set; }
        public string Phone { get; set; }

        public override string ToString()
        {
            return $"Phone:{PhoneValidationType.ToString()}:{Phone}";
        }
    }
}
