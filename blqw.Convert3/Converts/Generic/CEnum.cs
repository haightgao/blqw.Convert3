﻿using System;

namespace blqw.Converts
{
    /// <summary>
    /// 枚举转换器
    /// </summary>
    /// <typeparam name="T"> </typeparam>
    public class CEnum<T> : SystemTypeConvertor<T>
        where T : struct
    {
        /// <summary>
        /// 转换器的输出类型
        /// </summary>
        public override Type OutputType => typeof(T);

        /// <summary>
        /// 返回指定类型的对象，其值等效于指定字符串对象。
        /// </summary>
        /// <param name="context"> </param>
        /// <param name="input"> 需要转换类型的字符串对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="success"> 是否成功 </param>
        protected override T ChangeType(ConvertContext context, string input, Type outputType, out bool success)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                success = false;
                return default(T);
            }
            T result;
            try
            {
                result = (T)Enum.Parse(outputType, input, true);
            }
            catch (Exception ex)
            {
                context.AddException(ex);
                success = false;
                return default(T);
            }

            if (result.Equals(default(T)))
            {
                success = true;
                return default(T);
            }
            if (Enum.IsDefined(outputType, result))
            {
                success = true;
                return result;
            }
            if (Attribute.IsDefined(outputType, typeof(FlagsAttribute)))
            {
                if (result.ToString().Contains(","))
                {
                    success = true;
                    return result;
                }
            }
            context.AddException($"{result} 不是有效的枚举值");
            success = false;
            return default(T);
        }

        /// <summary>
        /// 返回指定类型的对象，其值等效于指定对象。
        /// </summary>
        /// <param name="context"> </param>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="success"> 是否成功 </param>
        protected override T ChangeTypeImpl(ConvertContext context, object input, Type outputType, out bool success)
        {
            var conv = input as IConvertible;
            if (conv != null)
            {
                T result;
                switch (conv.GetTypeCode())
                {
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                    case TypeCode.DateTime:
                    case TypeCode.Boolean:
                        success = false;
                        return default(T);
                    case TypeCode.Decimal:
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        result = (T)Enum.ToObject(outputType, conv.ToInt64(null));
                        break;
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        result = (T)Enum.ToObject(outputType, conv.ToUInt64(null));
                        break;
                    case TypeCode.String:
                        return ChangeType(context, conv.ToString(null), outputType, out success);
                    case TypeCode.Object:
                    default:
                        success = false;
                        return default(T);
                }
                if (result.Equals(default(T)))
                {
                    success = true;
                    return result;
                }
                if (Enum.IsDefined(outputType, result))
                {
                    success = true;
                    return result;
                }
                if (Attribute.IsDefined(outputType, typeof(FlagsAttribute)))
                {
                    if (result.ToString().Contains(","))
                    {
                        success = true;
                        return result;
                    }
                }
                context.AddException($"{result} 不是有效的枚举值");
            }
            success = false;
            return default(T);
        }
    }
}