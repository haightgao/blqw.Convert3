﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace blqw
{
    /// <summary> 超级牛逼的类型转换器v3版
    /// </summary>
    public static partial class Convert3
    {
        #region private

        private static TypeCache _cache = InitCache();

        private static TypeCache InitCache()
        {
            _cache = new TypeCache();
            AddCache(typeof(string), typeof(CString));
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var m in ass.GetModules())
                {
                    try
                    {
                        foreach (var conv in m.GetTypes())
                        {
                            if (conv.IsAbstract)
                            {
                                continue;
                            }
                            foreach (var iface in conv.GetInterfaces())
                            {
                                if (iface.IsGenericTypeDefinition
                                    || iface.IsGenericType == false
                                    || iface.GetGenericTypeDefinition() != typeof(IConvertor<>))
                                {
                                    continue;
                                }

                                var type = iface.GenericTypeArguments[0];
                                AddCache(type, conv);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString(), "Convert3初始化出现错误");
                    }
                }
            }
            _cache.Initialize();
            return _cache;
        }


        /// <summary> 添加缓存
        /// </summary>
        /// <param name="outputType"> 输出类型 </param>
        /// <param name="convType"> 转换器类型 </param>
        private static void AddCache(Type outputType, Type convType)
        {
            if (convType == null)
            {
                throw new ArgumentNullException("convType");
            }

            //泛型定义的处理
            //如果转换器是一个泛型定义类型,则从转换器的接口中提取当前T的类型,必须也是泛型,且泛型参数个数相同
            if (convType.IsGenericTypeDefinition)
            {
                var iconvT = CType.GetInterface(convType.GetInterfaces(), typeof(IConvertor<>));
                var resultT = iconvT.GetGenericArguments()[0];
                if (resultT.IsGenericType == false)
                {
                    return;
                }

                var args = resultT.GetGenericArguments();
                if (outputType.IsGenericType == false)
                {
                    return;
                }

                if (args.Any(it => it.IsGenericParameter) == false
                    && resultT.IsGenericTypeDefinition == false)
                {
                    return;
                }

                if (outputType.GetGenericArguments().Length != args.Length)
                {
                    return;
                }
                outputType = outputType.GetGenericTypeDefinition();
                _cache.Set(outputType,
                              TypeCacheItem.New(outputType, new GenericConvertorFactory(convType)));
                return;
            }

            var conv = (IConvertor)Activator.CreateInstance(convType);

            var cache = _cache.Get(outputType);

            if (cache == null || cache.Convertor.Priority <= conv.Priority)
            {
                _cache.Set(outputType, TypeCacheItem.New(outputType, conv));
            }
        }

        /// <summary> 抛出异常
        /// </summary>
        /// <param name="input">待转换的值</param>
        /// <param name="type">输出类型</param>
        internal static void ThrowError(object input, Type type)
        {
            throw ErrorContext.Error 
                ?? new InvalidCastException(CType.GetDisplayName(type) + " 类型转换失败");
        }

        #endregion

        internal static TypeCacheItem GetCache(Type type)
        {
            var cache = _cache.Get(type);
            if (cache != null)
            {
                return cache ?? _cache.Get<object>();
            }

            //泛型的处理
            if (type.IsGenericType
                && type.IsGenericTypeDefinition == false)
            {
                cache = _cache.Get(type.GetGenericTypeDefinition());
                if (cache != null)
                {
                    var factory = cache.Convertor as GenericConvertorFactory;
                    if (factory != null)
                    {
                        var convType = factory.Create(type);
                        AddCache(type, convType);
                        return _cache.Get(type);
                    }
                }
            }

            //如果是接口就不用继续了,接口没有基类,接口不用递归接口
            if (type.IsInterface || type.IsGenericTypeDefinition)
            {
                return null;
            }

            //基类型
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                var baseConv = GetConvertor(baseType);
                if (baseConv != null)
                {
                    if (baseConv is IIgnoreInherit == false)
                    {
                        var convType = typeof(LiskovConvertor<,>).MakeGenericType(baseType, type);
                        AddCache(type, convType);
                        return _cache.Get(type);
                    }
                }
                baseType = baseType.BaseType;
            }

            //接口类型
            foreach (var item in type.GetInterfaces())
            {
                var baseConv = GetConvertor(item);
                if (baseConv != null)
                {
                    if (baseConv is IIgnoreInherit == false)
                    {
                        var convType = typeof(LiskovConvertor<,>).MakeGenericType(item, type);
                        AddCache(type, convType);
                        return _cache.Get(type);
                    }
                }
            }

            {
                var convType = typeof(LiskovConvertor<,>).MakeGenericType(typeof(object), type);
                AddCache(type, convType);
                return _cache.Get(type);
            }
        }

        internal static TypeCacheItem GetCache<T>()
        {
            var cache = _cache.Get<T>();
            if (cache != null)
            {
                return cache;
            }
            return GetCache(typeof(T));
        }

        /// <summary> 获取转换器,失败返回null
        /// </summary>
        /// <param name="outputType">输出类型</param>
        /// <returns></returns>
        public static IConvertor GetConvertor(Type outputType)
        {
            var cache = GetCache(outputType);
            if (cache == null)
            {
                return null;
            }
            return cache.Convertor;
        }

        /// <summary> 获取转换器,失败返回null
        /// </summary>
        /// <typeparam name="T">输出类型泛型</typeparam>
        /// <returns></returns>
        public static IConvertor<T> GetConvertor<T>()
        {
            var cache = GetCache<T>();
            if (cache == null)
            {
                return null;
            }
            return (IConvertor<T>)cache.Convertor;
        }

        /// <summary> 返回一个指定类型的对象，该对象的值等效于指定的对象。转换失败抛出异常
        /// </summary>
        /// <param name="input">需要转换类型的对象</param>
        /// <param name="outputType">要返回的对象的类型</param>
        public static object ChangeType(this object input, Type outputType)
        {
            using (ErrorContext.Callin())
            {
                object result;
                if (TryChangedType(input, outputType, out result) == false)
                {
                    ThrowError(input, outputType);
                }
                return result;
            }
        }

        /// <summary> 返回一个指定类型的对象，该对象的值等效于指定的对象。转换失败返回默认值
        /// </summary>
        /// <param name="input">需要转换类型的对象</param>
        /// <param name="outputType">要返回的对象的类型</param>
        /// <param name="defaultValue">转换失败时返回的默认值</param>
        public static object ChangeType(this object input, Type outputType, object defaultValue)
        {
            object result;
            if (TryChangedType(input, outputType, out result) == false)
            {
                return defaultValue;
            }
            return result;
        }

        /// <summary> 返回一个指定类型的对象，该对象的值等效于指定的对象。转换失败抛出异常
        /// </summary>
        /// <typeparam name="T">要返回的对象类型的泛型</typeparam>
        /// <param name="input">需要转换类型的对象</param>
        public static T To<T>(this object input)
        {
            using (ErrorContext.Callin())
            {
                T result;
                if (TryTo<T>(input, out result) == false)
                {
                    ThrowError(input, typeof(T));
                }
                return result;
            }
        }

        /// <summary> 返回一个指定类型的对象，该对象的值等效于指定的对象。转换失败返回默认值
        /// </summary>
        /// <typeparam name="T">要返回的对象类型的泛型</typeparam>
        /// <param name="input">需要转换类型的对象</param>
        /// <param name="defaultValue">转换失败时返回的默认值</param>
        public static T To<T>(this object input, T defaultValue)
        {
            T result;
            if (TryTo<T>(input, out result) == false)
            {
                return defaultValue;
            }
            return result;
        }

        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <param name="input">需要转换类型的对象</param>
        /// <param name="outputType">要返回的对象的类型</param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为null</param>
        public static bool TryChangedType(this object input, Type outputType, out object result)
        {
            var conv = GetConvertor(outputType);
            return conv.Try(input, outputType, out result);
        }

        /// <summary> 尝试将指定对象转换为指定类型的值。返回是否转换成功
        /// </summary>
        /// <typeparam name="T">要返回的对象类型的泛型</typeparam>
        /// <param name="input">需要转换类型的对象</param>
        /// <param name="result">如果转换成功,则包含转换后的对象,否则为default(T)</param>
        public static bool TryTo<T>(this object input, out T result)
        {
            var conv = GetConvertor<T>();
            if (conv == null)
            {
                return CObject.TryTo<T>(input, typeof(T), out result);
            }
            return conv.Try(input, typeof(T), out result);
        }


        /// <summary> 转为动态类型
        /// </summary>
        public static dynamic ToDynamic(this object obj)
        {
            if (obj == null)
            {
                return new Dynamic.DynamicSystemObject(null);
            }
            if (obj is System.Dynamic.IDynamicMetaObjectProvider)
            {
                return obj;
            }

            var str = obj as string;
            if (str != null)
            {
                return new Dynamic.DynamicSystemObject(str);
            }
            var row = obj as DataRow;
            if (row != null)
            {
                return new Dynamic.DynamicDataRow(row);
            }
            var view = obj as DataRowView;
            if (view != null)
            {
                return new Dynamic.DynamicDataRow(view);
            }
            var nv = obj as NameValueCollection;
            if (nv != null)
            {
                return new Dynamic.DynamicNameValueCollection(nv);
            }
            var reader = obj as IDataReader;
            if (reader != null)
            {
                return new Dynamic.DynamicDictionary(reader.To<IDictionary>());
            }

            var dict = obj as IDictionary;
            if (dict != null)
            {
                return new Dynamic.DynamicDictionary(dict);
            }
            var list = obj as System.Collections.IList;
            if (list != null)
            {
                return new Dynamic.DynamicList(list);
            }
            if ("System".Equals(obj.GetType().Namespace, StringComparison.Ordinal))
            {
                return new Dynamic.DynamicSystemObject(obj);
            }
            return new Dynamic.DynamicEntity(obj);
        }

        #region 半角全角转换
        /// <summary> 半角转全角
        /// </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>全角字符串</returns>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static string ToSBC(string input)
        {
            //半角转全角：
            char[] arr = input.ToCharArray();
            var length = arr.Length;
            unsafe
            {
                fixed (char* p = arr)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var c = p[i];
                        if (TryToSBC(ref c))
                        {
                            p[i] = c;
                        }
                    }
                    return new string(p, 0, length);
                }
            }
        }

        /// <summary> 半角转全角
        /// </summary>
        /// <param name="c">字符</param>
        ///<remarks>
        ///全角空格为12288，半角空格为32
        ///其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static char ToSBC(char c)
        {
            TryToSBC(ref c);
            return c;
        }

        public static bool TryToSBC(ref char c)
        {
            if (c < 127)
            {
                if (c > 32)
                {
                    c = (char)(c + 65248);
                    return true;
                }
                else if (c == 32)
                {
                    c = (char)12288;
                    return true;
                }
            }
            return false;
        }

        /// <summary> 全角转半角(DBC case) </summary>
        /// <param name="input">任意字符串</param>
        /// <returns>半角字符串</returns>
        ///<remarks>
        /// 全角空格为12288，半角空格为32
        /// 其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static string ToDBC(string input)
        {
            //半角转全角：
            char[] arr = input.ToCharArray();
            var length = arr.Length;
            unsafe
            {
                fixed (char* p = arr)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var c = p[i];
                        if (TryToDBC(ref c))
                        {
                            p[i] = c;
                        }
                    }
                    return new string(p, 0, length);
                }
            }
        }


        /// <summary> 全角转半角
        /// </summary>
        /// <param name="c">字符</param>
        ///<remarks>
        /// 全角空格为12288，半角空格为32
        /// 其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248
        ///</remarks>
        public static char ToDBC(char c)
        {
            TryToDBC(ref c);
            return c;
        }

        public static bool TryToDBC(ref char c)
        {
            if (c == 12288)
            {
                c = (char)32;
                return true;
            }
            else if (c >= 65281 && c <= 65374)
            {
                c = (char)(c - 65248);
                return true;
            }
            return false;
        }
        #endregion

        #region MD5

        /// <summary> 使用MD5加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        /// <remarks>周子鉴 2015.08.26</remarks>
        public static Guid ToMD5_Fast(string input)
        {
            using (var md5Provider = new MD5CryptoServiceProvider())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5Provider.ComputeHash(bytes);
                var count = hash.Length;
                hash[0] = (byte)(hash[3] + (hash[3] = hash[0]) * 0); //交换0,3的值
                hash[1] = (byte)(hash[2] + (hash[2] = hash[1]) * 0); //交换1,2的值
                hash[5] = (byte)(hash[4] + (hash[4] = hash[5]) * 0); //交换4,5的值
                hash[7] = (byte)(hash[6] + (hash[6] = hash[7]) * 0); //交换6,7的值
                return new Guid(hash);
            }
        }

        /// <summary> 产生一个包含随机'盐'的的MD5
        /// </summary>
        /// <param name="input">输入内容</param>
        /// <returns></returns>
        /// <remarks>周子鉴 2015.10.03</remarks>
        public static Guid ToRandomMD5(string input)
        {
            using (var md5Provider = new MD5CryptoServiceProvider())
            {
                //获取一个随机数,用于充当 "盐"
                var salt = new object().GetHashCode();
                input += salt;
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5Provider.ComputeHash(bytes);
                var saltBytes = BitConverter.GetBytes(salt);
                var index = hash[0] % 12 + 1;
                hash[index] = saltBytes[0];
                hash[index + 1] = saltBytes[1];
                hash[index + 2] = saltBytes[2];
                hash[index + 3] = saltBytes[3];
                return new Guid(hash);
            }
        }

        /// <summary> 对比使用 ToRandomMD5 产生的MD5和原信息是否匹配
        /// </summary>
        /// <param name="input">原信息</param>
        /// <param name="rmd5">随机盐MD5</param>
        /// <returns></returns>
        /// <remarks>周子鉴 2015.10.03</remarks>
        public static bool EqualsRandomMD5(string input, Guid rmd5)
        {
            var arr = rmd5.ToByteArray();
            var index = arr[0] % 12 + 1;
            //将盐取出来
            var salt = BitConverter.ToInt32(arr, index);
            using (var md5Provider = new MD5CryptoServiceProvider())
            {
                input += salt;
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5Provider.ComputeHash(bytes);
                for (int i = 0; i < 16; i++)
                {
                    if (i == index) //跳过盐的部分
                    {
                        i += 4; continue;
                    }
                    if (hash[i] != arr[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        #endregion

        #region Decode

        /// <summary> 使用16位MD5加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        /// <param name="count">加密次数</param>
        public static string ToMD5x16(string input, int count = 1)
        {
            if (count <= 0)
            {
                return input;
            }
            for (int i = 0; i < count; i++)
            {
                input = ToMD5x16(input);
            }
            return input;
        }
        /// <summary> 使用16位MD5加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        public static string ToMD5x16(string input)
        {
            return ToMD5x16(Encoding.UTF8.GetBytes(input));
        }
        /// <summary> 使用MD5加密
        /// </summary>
        /// <param name="input">需要加密的字节</param>
        public static string ToMD5x16(byte[] input)
        {
            var md5 = new MD5CryptoServiceProvider();
            var data = Hash(md5, input);
            return ByteToString(data, 4, 8);
        }
        /// <summary> 使用MD5加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        /// <param name="count">加密次数</param>
        public static string ToMD5(string input, int count = 1)
        {
            if (count <= 0)
            {
                return input;
            }
            for (int i = 0; i < count; i++)
            {
                input = ToMD5(input);
            }
            return input;
        }
        /// <summary> 使用MD5加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        public static string ToMD5(string input)
        {
            return ToMD5(Encoding.UTF8.GetBytes(input));
        }
        /// <summary> 使用MD5加密
        /// </summary>
        /// <param name="input">需要加密的字节</param>
        public static string ToMD5(byte[] input)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var data = Hash(md5, input);
                return ByteToString(data);
            }
        }

        /// <summary> 使用SHA1加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        /// <param name="count">加密次数</param>
        public static string ToSHA1(string input, int count = 1)
        {
            if (count <= 0)
            {
                return input;
            }
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var data = Encoding.UTF8.GetBytes(input);
                for (int i = 0; i < count; i++)
                {
                    data = Hash(sha1, data);
                }
                return ByteToString(data);
            }
        }
        /// <summary> 使用SHA1加密
        /// </summary>
        /// <param name="input">加密字符串</param>
        public static string ToSHA1(string input)
        {
            return ToSHA1(Encoding.UTF8.GetBytes(input));
        }
        /// <summary> 使用SHA1加密
        /// </summary>
        /// <param name="input">需要加密的字节</param>
        public static string ToSHA1(byte[] input)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var data = Hash(sha1, input);
                return ByteToString(data);
            }
        }

        private static string ByteToString(byte[] data)
        {
            return ByteToString(data, 0, data.Length);
        }

        private static string ByteToString(byte[] data, int offset, int count)
        {
            if (data == null)
            {
                return null;
            }
            char[] chArray = new char[count * 2];
            var end = offset + count;
            for (int i = offset, j = 0; i < end; i++)
            {
                byte num2 = data[i];
                chArray[j++] = NibbleToHex((byte)(num2 >> 4));
                chArray[j++] = NibbleToHex((byte)(num2 & 15));
            }
            return new string(chArray);
        }
        private static char NibbleToHex(byte nibble)
        {
            return ((nibble < 10) ? ((char)(nibble + 0x30)) : ((char)((nibble - 10) + 'a')));
        }

        private static byte[] Hash(HashAlgorithm algorithm, byte[] input)
        {
            return algorithm.ComputeHash(input);
        }

        #endregion

        #region DbType

        public static DbType TypeToDbType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return DbType.Boolean;
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.Char:
                    return DbType.Boolean;
                case TypeCode.DBNull:
                    return DbType.Object;
                case TypeCode.DateTime:
                    return DbType.DateTime;
                case TypeCode.Decimal:
                    return DbType.Decimal;
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Empty:
                    return DbType.Object;
                case TypeCode.Int16:
                    return DbType.Int16;
                case TypeCode.Int32:
                    return DbType.Int32;
                case TypeCode.Int64:
                    return DbType.Int64;
                case TypeCode.SByte:
                    return DbType.SByte;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.String:
                    return DbType.String;
                case TypeCode.UInt16:
                    return DbType.UInt16;
                case TypeCode.UInt32:
                    return DbType.UInt32;
                case TypeCode.UInt64:
                    return DbType.UInt64;
                case TypeCode.Object:
                default:
                    break;
            }
            if (type == typeof(Guid))
            {
                return DbType.Guid;
            }
            else if (type == typeof(byte[]))
            {
                return DbType.Binary;
            }
            else if (type == typeof(System.Xml.XmlDocument))
            {
                return DbType.Xml;
            }
            return DbType.Object;
        }

        public static Type DbTypeToType(DbType dbtype)
        {
            switch (dbtype)
            {
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return typeof(String);
                case DbType.Binary:
                    return typeof(Byte[]);
                case DbType.Boolean:
                    return typeof(Boolean);
                case DbType.Byte:
                    return typeof(Byte);
                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                case DbType.Time:
                    return typeof(DateTime);
                case DbType.Decimal:
                case DbType.VarNumeric:
                case DbType.Currency:
                    return typeof(Decimal);
                case DbType.Double:
                    return typeof(Double);
                case DbType.Guid:
                    return typeof(Guid);
                case DbType.Int16:
                    return typeof(Int16);
                case DbType.Int32:
                    return typeof(Int32);
                case DbType.Int64:
                    return typeof(Int64);
                case DbType.Object:
                    return typeof(Object);
                case DbType.SByte:
                    return typeof(SByte);
                case DbType.Single:
                    return typeof(Single);
                case DbType.UInt16:
                    return typeof(UInt16);
                case DbType.UInt32:
                    return typeof(UInt32);
                case DbType.UInt64:
                    return typeof(UInt64);
                case DbType.Xml:
                    return typeof(System.Xml.XmlDocument);
                default:
                    throw new InvalidCastException("无效的DbType值:" + dbtype.ToString());
            }
        }

        #endregion

        /// <summary> 获取一个类型的默认值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [Export("GetDefaultValue")]
        [ExportMetadata("Priority", 100)]
        public static object GetDefaultValue(this Type type)
        {
            if (type == null
                || type.IsValueType == false
                || type.IsGenericTypeDefinition
                || Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }
            return Activator.CreateInstance(type);
        }
    }
}