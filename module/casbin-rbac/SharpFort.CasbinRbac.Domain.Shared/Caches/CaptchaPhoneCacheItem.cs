using SharpFort.CasbinRbac.Domain.Shared.Enums;

namespace SharpFort.CasbinRbac.Domain.Shared.Caches
{
    public class CaptchaPhoneCacheItem(string code)
    {
        public string Code { get; set; } = code;
    }

    public class CaptchaPhoneCacheKey(PhoneValidationType validationPhoneType, string phone)
    {
        public PhoneValidationType PhoneValidationType { get; set; } = validationPhoneType;
        public string Phone { get; set; } = phone;

        public override string ToString()
        {
            return $"Phone:{PhoneValidationType.ToString()}:{Phone}";
        }
    }
}
