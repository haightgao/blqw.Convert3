﻿using System;
using System.Globalization;

namespace blqw.Converts
{
    internal sealed class CSingle : SystemTypeConvertor<float>
    {
        protected override float ChangeType(ConvertContext context, string input, Type outputType, out bool success)
        {
            float result;
            if (float.TryParse(input, out result))
            {
                success = true;
                return result;
            }
            if (CString.IsHexString(ref input))
            {
                success = float.TryParse(input, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out result);
                return result;
            }
            success = false;
            return 0;
        }

        protected override float ChangeTypeImpl(ConvertContext context, object input, Type outputType, out bool success)
        {
            var conv = input as IConvertible;
            if (conv != null)
            {
                success = true;
                switch (conv.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        return conv.ToBoolean(null) ? 1 : 0;

                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                    case TypeCode.DateTime:
                        success = false;
                        return default(float);
                    case TypeCode.Byte:
                        return conv.ToByte(null);
                    case TypeCode.Char:
                        return conv.ToChar(null);
                    case TypeCode.Int16:
                        return conv.ToInt16(null);
                    case TypeCode.Int32:
                        return conv.ToInt32(null);
                    case TypeCode.Int64:
                        return conv.ToInt64(null);
                    case TypeCode.SByte:
                        return conv.ToSByte(null);
                    case TypeCode.Double:
                    {
                        var a = conv.ToDouble(null);
                        if ((a < float.MinValue) || (a > float.MaxValue))
                        {
                            success = false;
                            return default(float);
                        }
                        return (float) a;
                    }
                    case TypeCode.Single:
                        return conv.ToSingle(null);
                    case TypeCode.UInt16:
                        return conv.ToUInt16(null);
                    case TypeCode.UInt32:
                        return conv.ToUInt32(null);
                    case TypeCode.UInt64:
                        return conv.ToUInt64(null);
                    case TypeCode.Decimal:
                        return (float) conv.ToDecimal(null);
                    case TypeCode.Object:
                        break;
                    case TypeCode.String:
                        return ChangeType(context, conv.ToString(null), outputType, out success);
                    default:
                        break;
                }
            }
            else
            {
                var bs = input as byte[];
                if (bs?.Length == 4)
                {
                    success = true;
                    return BitConverter.ToSingle(bs, 0);
                }
            }
            success = false;
            return default(float);
        }
    }
}