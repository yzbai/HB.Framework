﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace ClassLibrary1
{

    public class DateTimeOffsetTypeHandler : ITypeHandler
    {
        public void SetValue(IDbDataParameter parameter, object value)
        {

        }

        public object Parse(Type destinationType, object value)
        {
            return ValueConverterUtil.DbValueToTypeValue(value, destinationType);
        }
    }


    public static class TypeHandlerHelper
    {

        static TypeHandlerHelper()
        {
            AddTypeHandlerImpl(typeof(DateTimeOffset), new DateTimeOffsetTypeHandler(), false);
        }

        internal static bool HasTypeHandler(Type type) => TypeHandlers.ContainsKey(type);

        public static Dictionary<Type, ITypeHandler> TypeHandlers = new Dictionary<Type, ITypeHandler>();

        public static void AddTypeHandlerImpl(Type type, ITypeHandler handler, bool clone)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            Type secondary = null;
            if (type.IsValueType)
            {
                var underlying = Nullable.GetUnderlyingType(type);
                if (underlying == null)
                {
                    secondary = typeof(Nullable<>).MakeGenericType(type); // the Nullable<T>
                    // type is already the T
                }
                else
                {
                    secondary = type; // the Nullable<T>
                    type = underlying; // the T
                }
            }

            var snapshot = TypeHandlers;
            if (snapshot.TryGetValue(type, out ITypeHandler oldValue) && handler == oldValue) return; // nothing to do

            var newCopy = clone ? new Dictionary<Type, ITypeHandler>(snapshot) : snapshot;

#pragma warning disable 618
            typeof(TypeHandlerCache<>).MakeGenericType(type).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            if (secondary != null)
            {
                typeof(TypeHandlerCache<>).MakeGenericType(secondary).GetMethod(nameof(TypeHandlerCache<int>.SetHandler), BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { handler });
            }
#pragma warning restore 618
            if (handler == null)
            {
                newCopy.Remove(type);
                if (secondary != null) newCopy.Remove(secondary);
            }
            else
            {
                newCopy[type] = handler;
                if (secondary != null) newCopy[secondary] = handler;
            }
            TypeHandlers = newCopy;
        }

    }
}
#nullable restore