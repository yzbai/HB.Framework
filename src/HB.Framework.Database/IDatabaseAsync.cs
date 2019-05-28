﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using HB.Framework.Database.Entity;
using HB.Framework.Database.SQL;
using System.Threading.Tasks;
using HB.Framework.Database.Engine;
using HB.Framework.Database.Transaction;

namespace HB.Framework.Database
{
    public interface IDatabaseAsync
    {
		Task<IList<TSelect>> RetrieveAsync<TSelect, TFrom, TWhere>(SelectExpression<TSelect> selectCondition, FromExpression<TFrom> fromCondition, WhereExpression<TWhere> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false)
			where TSelect : DatabaseEntity, new()
			where TFrom : DatabaseEntity, new()
			where TWhere : DatabaseEntity, new();

        Task<IList<T>> RetrieveAsync<T>(WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> RetrieveAsync<T>(Expression<Func<T, bool>> whereExpr, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> RetrieveAsync<T>(FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> RetrieveAsync<T>(SelectExpression<T> selectCondition, FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<Tuple<TSource, TTarget>>> RetrieveAsync<TSource, TTarget>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget : DatabaseEntity, new();

        Task<IList<Tuple<TSource, TTarget1, TTarget2>>> RetrieveAsync<TSource, TTarget1, TTarget2>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget1 : DatabaseEntity, new()
            where TTarget2 : DatabaseEntity, new();

        Task<IList<T>> RetrieveAllAsync<T>(DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<long> CountAsync<T>(DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<long> CountAsync<T>(WhereExpression<T> condition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<long> CountAsync<T>(Expression<Func<T, bool>> whereExpr, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<long> CountAsync<T>(FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<long> CountAsync<T>(SelectExpression<T> selectCondition, FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> PageAsync<T>(long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> PageAsync<T>(WhereExpression<T> whereCondition, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> PageAsync<T>(Expression<Func<T, bool>> whereExpr, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> PageAsync<T>(FromExpression<T> fromCondition, WhereExpression<T> whereCondition, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<T>> PageAsync<T>(SelectExpression<T> selectCondition, FromExpression<T> fromCondition, WhereExpression<T> whereCondition, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<IList<Tuple<TSource, TTarget>>> PageAsync<TSource, TTarget>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget : DatabaseEntity, new();
        Task<IList<Tuple<TSource, TTarget1, TTarget2>>> PageAsync<TSource, TTarget1, TTarget2>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, long pageNumber, long perPageCount, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget1 : DatabaseEntity, new()
            where TTarget2 : DatabaseEntity, new();
        Task<T> ScalarAsync<T>(WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<T> ScalarAsync<T>(long id, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<T> ScalarAsync<T>(Expression<Func<T, bool>> whereExpr, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<T> ScalarAsync<T>(FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<T> ScalarAsync<T>(SelectExpression<T> selectCondition, FromExpression<T> fromCondition, WhereExpression<T> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false) where T : DatabaseEntity, new();
        Task<Tuple<TSource, TTarget>> ScalarAsync<TSource, TTarget>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget : DatabaseEntity, new();
        Task<Tuple<TSource, TTarget1, TTarget2>> ScalarAsync<TSource, TTarget1, TTarget2>(FromExpression<TSource> fromCondition, WhereExpression<TSource> whereCondition, DatabaseTransactionContext transContext = null, bool useMaster = false)
            where TSource : DatabaseEntity, new()
            where TTarget1 : DatabaseEntity, new()
            where TTarget2 : DatabaseEntity, new();

        Task<DatabaseResult> AddAsync<T>(T item, DatabaseTransactionContext transContext = null) where T : DatabaseEntity, new();
        Task<DatabaseResult> DeleteAsync<T>(T item, DatabaseTransactionContext transContext = null) where T : DatabaseEntity, new();
        Task<DatabaseResult> UpdateAsync<T>(T item, DatabaseTransactionContext transContext = null) where T : DatabaseEntity, new();

        Task<DatabaseResult> BatchAddAsync<T>(IList<T> items, string lastUser, DatabaseTransactionContext transContext) where T : DatabaseEntity, new();
        Task<DatabaseResult> BatchDeleteAsync<T>(IList<T> items, string lastUser, DatabaseTransactionContext transContext) where T : DatabaseEntity, new();
        Task<DatabaseResult> BatchUpdateAsync<T>(IList<T> items, string lastUser, DatabaseTransactionContext transContext) where T : DatabaseEntity, new();
        
    }
}