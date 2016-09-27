﻿using System;
using System.Collections.Generic;

namespace blqw.Converts
{
    /// <summary>
    /// 自定定义类型
    /// </summary>
    internal sealed class CIDictionaryFactory : GenericConvertor
    {
        public override Type OutputType => typeof(IDictionary<,>);

        /// <summary>
        /// 根据返回类型的泛型参数类型返回新的转换器
        /// </summary>
        /// <param name="outputType"> </param>
        /// <param name="genericTypes"> </param>
        /// <returns> </returns>
        protected override IConvertor GetConvertor(Type outputType, Type[] genericTypes)
        {
            var type = typeof(CIDictionary<,>).MakeGenericType(genericTypes);
            return (IConvertor) Activator.CreateInstance(type);
        }
    }
}