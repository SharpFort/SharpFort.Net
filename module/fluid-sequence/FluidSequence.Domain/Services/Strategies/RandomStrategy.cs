using System;
using System.Collections.Generic;
using Volo.Abp.DependencyInjection;
using FluidSequence.Domain.Entities;

namespace FluidSequence.Domain.Services.Strategies
{
    public class RandomStrategy : IPlaceholderStrategy, ISingletonDependency
    {
        private static readonly Random _random = new Random();
        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        // Safe chars excluding I, O, Z, 0, 1, 2
        private const string SafeChars = "ABCDEFGHJKLMNPQRSTUVWXY3456789"; 
        private const string MixChars = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";

        public bool CanHandle(string key)
        {
            return key.StartsWith("RAND:");
        }

        public string Handle(string key, SysSequenceRule rule, Dictionary<string, string> context)
        {
            // RAND:NUM:4
            var parts = key.Split(':');
            if (parts.Length < 3) return key;

            string type = parts[1];
            if (!int.TryParse(parts[2], out int len)) return key;

            char[] buffer = new char[len];
            string source = "";

            switch (type)
            {
                case "NUM": source = "0123456789"; break;
                case "CHAR": source = Chars; break;
                case "SAFE": source = SafeChars; break;
                case "MIX": source = MixChars; break;
                default: return key;
            }

            lock(_random)
            {
                for (int i = 0; i < len; i++)
                {
                    buffer[i] = source[_random.Next(source.Length)];
                }
            }
            return new string(buffer);
        }
    }
}
