using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpFort.CodeGen.Domain.Handlers
{
    public class HandledTemplate
    {
        public required string TemplateStr { get; set; }

        public required string BuildPath { get; set; }
    }
}
