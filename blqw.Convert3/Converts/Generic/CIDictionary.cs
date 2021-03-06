﻿using blqw.IOC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;

namespace blqw.Converts
{
    /// <summary>
    /// 泛型 <see cref="IDictionary{TKey,TValue}" /> 转换器
    /// </summary>
    /// <typeparam name="K"> 键类型 </typeparam>
    /// <typeparam name="V"> 值类型 </typeparam>
    public class CIDictionary<K, V> : BaseTypeConvertor<IDictionary<K, V>>
    {
        /// <summary>
        /// 返回指定类型的对象，其值等效于指定对象。
        /// </summary>
        /// <param name="context"> </param>
        /// <param name="input"> 需要转换类型的对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="success"> 是否成功 </param>
        /// <returns> </returns>
        protected override IDictionary<K, V> ChangeTypeImpl(ConvertContext context, object input, Type outputType,
            out bool success)
        {
            if (input.IsNull())
            {
                success = true;
                return null;
            }

            var keyConvertor = context.Get<K>();
            var valueConvertor = context.Get<V>();


            var builder = new DictionaryBuilder(context, outputType, keyConvertor, valueConvertor);
            if (builder.TryCreateInstance() == false)
            {
                success = false;
                return null;
            }
            var mapper = new Mapper(input);

            if (mapper.Error != null)
            {
                context.AddException(mapper.Error);
                success = false;
                return null;
            }

            while (mapper.MoveNext())
            {
                if (builder.Add(mapper.Key, mapper.Value) == false)
                {
                    success = false;
                    return null;
                }
            }

            success = true;
            return builder.Instance;
        }

        /// <summary>
        /// 返回指定类型的对象，其值等效于指定字符串对象。
        /// </summary>
        /// <param name="context"> </param>
        /// <param name="input"> 需要转换类型的字符串对象 </param>
        /// <param name="outputType"> 换转后的类型 </param>
        /// <param name="success"> 是否成功 </param>
        protected override IDictionary<K, V> ChangeType(ConvertContext context, string input, Type outputType,
            out bool success)
        {
            input = input?.Trim() ?? "";
            if (input.Length == 0)
            {
                var keyConvertor = context.Get<K>();
                var valueConvertor = context.Get<V>();

                var builder = new DictionaryBuilder(context, outputType, keyConvertor, valueConvertor);
                // ReSharper disable once AssignmentInConditionalExpression
                return (success = builder.TryCreateInstance()) ? builder.Instance : null;
            }
            if (input?.Length > 2)
            {
                if ((input[0] == '{') && (input[input.Length - 1] == '}'))
                {
                    try
                    {
                        var result = ComponentServices.ToJsonObject(outputType, input);
                        success = true;
                        return (IDictionary<K, V>)result;
                    }
                    catch (Exception ex)
                    {
                        context.AddException(ex);
                    }
                }
            }
            success = false;
            return null;
        }

        /// <summary>
        /// <see cref="IDictionary{TKey,TValue}" /> 构造器
        /// </summary>
        private struct DictionaryBuilder : IBuilder<IDictionary<K, V>, DictionaryEntry>
        {
            /// <summary>
            /// 键转换器
            /// </summary>
            private readonly IConvertor<K> _keyConvertor;

            /// <summary>
            /// 值转换器
            /// </summary>
            private readonly IConvertor<V> _valueConvertor;

            /// <summary>
            /// 需要构造的实例类型
            /// </summary>
            private readonly Type _type;

            /// <summary>
            /// 转换上下文
            /// </summary>
            private readonly ConvertContext _context;

            /// <summary>
            /// 初始化构造器
            /// </summary>
            /// <param name="context"> 转换上下文 </param>
            /// <param name="type"> 待构造的对象实际类型 </param>
            /// <param name="keyConvertor"> 键转换器 </param>
            /// <param name="valueConvertor"> 值转换器 </param>
            public DictionaryBuilder(ConvertContext context, Type type, IConvertor<K> keyConvertor,
                IConvertor<V> valueConvertor)
            {
                _context = context;
                _type = type;
                Instance = null;
                _keyConvertor = keyConvertor;
                _valueConvertor = valueConvertor;
            }

            /// <summary>
            /// 被构造的实例
            /// </summary>
            public IDictionary<K, V> Instance { get; private set; }

            /// <summary>
            /// 设置对象值
            /// </summary>
            /// <param name="obj">待设置的值</param>
            /// <returns></returns>
            public bool Set(DictionaryEntry obj) => Add(obj.Key, obj.Value);

            /// <summary>
            /// 尝试构造实例,返回是否成功
            /// </summary>
            /// <returns></returns>
            public bool TryCreateInstance()
            {
                if (_type.IsInterface)
                {
                    Instance = new Dictionary<K, V>();
                    return true;
                }
                try
                {
                    Instance = (IDictionary<K, V>)Activator.CreateInstance(_type);
                    return true;
                }
                catch (Exception ex)
                {
                    _context.AddException(ex);
                    return false;
                }
            }

            /// <summary>
            /// 向字典添加值
            /// </summary>
            public bool Add(object key, object value)
            {
                bool b;
                var k = _keyConvertor.ChangeType(_context, key, _keyConvertor.OutputType, out b);
                if (b == false)
                {
                    _context.AddException($"添加到字典{CType.GetFriendlyName(_type)}失败");
                    return false;
                }

                var v = _valueConvertor.ChangeType(_context, value, _valueConvertor.OutputType, out b);
                if (b == false)
                {
                    _context.AddException($"向字典{CType.GetFriendlyName(_type)}中添加元素 {key} 失败");
                    return false;
                }
                try
                {
                    Instance.Add(k, v);
                    return true;
                }
                catch (Exception ex)
                {
                    _context.AddException($"向字典{CType.GetFriendlyName(_type)}中添加元素 {key} 失败,原因:{ex.Message}", ex);
                    return false;
                }
            }
        }
    }
}