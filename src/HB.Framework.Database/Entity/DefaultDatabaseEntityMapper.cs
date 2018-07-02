﻿using HB.Framework.Common;
using System;
using System.Collections.Generic;
using System.Data;

namespace HB.Framework.Database.Entity
{
    /// <summary>
    /// 單例
    /// </summary>
    public class DefaultDatabaseEntityMapper : IDatabaseEntityMapper
    {
        private IDatabaseEntityDefFactory _modelDefFactory;

        public DefaultDatabaseEntityMapper(IDatabaseEntityDefFactory modelDefFactory)
        {
            _modelDefFactory = modelDefFactory;
        }

        #region 表行与实体间映射

        public IList<T> To<T>(IDataReader reader)
            where T : DatabaseEntity, new()
        {
            IList<T> lst = new List<T>();

            if (reader == null)
            {
                return lst;
            }

            int len = reader.FieldCount;
            string[] propertyNames = new string[len];
            

            DatabaseEntityDef definition = _modelDefFactory.Get<T>();

            for (int i = 0; i < len; ++i)
            {
                propertyNames[i] = reader.GetName(i);
            }

            while (reader.Read())
            {
                T item = new T();

                for (int i = 0; i < len; ++i)
                {
                    DatabaseEntityPropertyDef property = definition.GetProperty(propertyNames[i]);

                    object value = DataConverter.To(property.PropertyType, reader[i]);

                    property.SetValue(item, value);
                }

                lst.Add(item);

            }

            return lst;
        }

        public IList<Tuple<TSource, TTarget>> To<TSource, TTarget>(IDataReader reader)
            where TSource : DatabaseEntity, new()
            where TTarget : DatabaseEntity, new()
        {
            IList<Tuple<TSource, TTarget>> lst = new List<Tuple<TSource, TTarget>>();

            if (reader == null)
            {
                return lst;
            }

            DatabaseEntityDef definition1 = _modelDefFactory.Get<TSource>();
            DatabaseEntityDef definition2 = _modelDefFactory.Get<TTarget>();

            string[] propertyNames1 = new string[definition1.FieldCount];
            string[] propertyNames2 = new string[definition2.FieldCount];

            int j = 0;

            for (int i = 0; i < definition1.FieldCount; ++j, ++i)
            {
                propertyNames1[i] = reader.GetName(j);
            }

            for (int i = 0; i < definition2.FieldCount; ++j, ++i)
            {
                propertyNames2[i] = reader.GetName(j);
            }

            while (reader.Read())
            {
                TSource t1 = new TSource();
                TTarget t2 = new TTarget();

                j = 0;

                for (int i = 0; i < definition1.FieldCount; ++i, ++j)
                {
                    DatabaseEntityPropertyDef pDef = definition1.GetProperty(propertyNames1[i]);

                    if (pDef != null)
                    {
                        pDef.SetValue(t1, DataConverter.To(pDef.PropertyType, reader[j]));
                    }
                }

                for (int i = 0; i < definition2.FieldCount; ++i, ++j)
                {
                    DatabaseEntityPropertyDef pDef = definition2.GetProperty(propertyNames2[i]);

                    if (pDef != null)
                    {
                        pDef.SetValue(t2, DataConverter.To(pDef.PropertyType, reader[j]));
                    }
                }

                lst.Add(new Tuple<TSource, TTarget>(t1, t2));
            }

            return lst;
        }

        public IList<Tuple<TSource, TTarget2, TTarget3>> To<TSource, TTarget2, TTarget3>(IDataReader reader)
            where TSource : DatabaseEntity, new()
            where TTarget2 : DatabaseEntity, new()
            where TTarget3 : DatabaseEntity, new()
        {
            IList<Tuple<TSource, TTarget2, TTarget3>> lst = new List<Tuple<TSource, TTarget2, TTarget3>>();

            if (reader == null)
            {
                return lst;
            }

            DatabaseEntityDef definition1 = _modelDefFactory.Get<TSource>();
            DatabaseEntityDef definition2 = _modelDefFactory.Get<TTarget2>();
            DatabaseEntityDef definition3 = _modelDefFactory.Get<TTarget3>();

            string[] propertyNames1 = new string[definition1.FieldCount];
            string[] propertyNames2 = new string[definition2.FieldCount];
            string[] propertyNames3 = new string[definition3.FieldCount];

            int j = 0;

            for (int i = 0; i < definition1.FieldCount; ++i, ++j)
            {
                propertyNames1[i] = reader.GetName(j);
            }

            for (int i = 0; i < definition2.FieldCount; ++i, ++j)
            {
                propertyNames2[i] = reader.GetName(j);
            }

            for (int i = 0; i < definition3.FieldCount; ++i, ++j)
            {
                propertyNames3[i] = reader.GetName(j);
            }

            while (reader.Read())
            {
                TSource t1 = new TSource();
                TTarget2 t2 = new TTarget2();
                TTarget3 t3 = new TTarget3();

                j = 0;

                for (int i = 0; i < definition1.FieldCount; ++i, ++j)
                {
                    DatabaseEntityPropertyDef pDef = definition1.GetProperty(propertyNames1[i]);

                    if (pDef != null)
                    {
                        pDef.SetValue(t1, DataConverter.To(pDef.PropertyType, reader[j]));
                    }
                }

                for (int i = 0; i < definition2.FieldCount; ++i, ++j)
                {
                    DatabaseEntityPropertyDef pDef = definition2.GetProperty(propertyNames2[i]);

                    if (pDef != null)
                    {
                        pDef.SetValue(t2, DataConverter.To(pDef.PropertyType, reader[j]));
                    }
                }

                for (int i = 0; i < definition3.FieldCount; ++i, ++j)
                {
                    DatabaseEntityPropertyDef pDef = definition3.GetProperty(propertyNames3[i]);

                    if (pDef != null)
                    {
                        pDef.SetValue(t3, DataConverter.To(pDef.PropertyType, reader[j]));
                    }
                }

                lst.Add(new Tuple<TSource, TTarget2, TTarget3>(t1, t2, t3));
            }

            return lst;
        }

        public void To<T>(IDataReader reader, T item) where T : DatabaseEntity, new()
        {
            if (reader == null)
            {
                return;
            }

            int len = reader.FieldCount;
            string[] propertyNames = new string[len];

            DatabaseEntityDef definition = _modelDefFactory.Get<T>();

            for (int i = 0; i < len; ++i)
            {
                propertyNames[i] = reader.GetName(i);
            }

            if (reader.Read())
            {
                for (int i = 0; i < len; ++i)
                {
                    DatabaseEntityPropertyDef property = definition.GetProperty(propertyNames[i]);
                    property.SetValue(item, DataConverter.To(property.PropertyType, reader[i]));
                }
            }
        }

        #endregion        
    }
}
