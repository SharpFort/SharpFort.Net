using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SharpFort.CodeGen.Domain.Shared.Enums; 

public enum FieldType
{
    [Display(Name = "string", Description = "String")]
    String =1,

    [Display(Name = "int", Description = "Int32")]
    Int =2,

    [Display(Name = "long", Description = "Int64")]
    Long =3,

    [Display(Name = "bool", Description = "Boolean")]
    Bool =4,

    [Display(Name = "decimal", Description = "Decimal")]
    Decimal =5,

    [Display(Name = "DateTime", Description = "DateTime")]
    DateTime =6,

    [Display(Name = "Guid", Description = "Guid")]
    Guid =7
}


