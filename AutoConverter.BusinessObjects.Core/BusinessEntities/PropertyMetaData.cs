using System;
using System.Collections.Generic;
using System.Reflection;

namespace AutoConverter.BusinessObjects.Core.BusinessEntities
{
    public class PropertyMetaData
    {
        public List<PropertyInfo> PropertyInfoList { get; set; }
        public Type Type { get; set; }
    }
}
