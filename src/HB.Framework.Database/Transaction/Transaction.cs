﻿using HB.Framework.Database.Engine;
using HB.Framework.Database.Entity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace HB.Framework.Database.Transaction
{
    internal partial class Transaction : ITransaction
    {
        private IDatabaseEntityDefFactory entityDefFactory;
        private IDatabaseEngine databaseEngine;

        public Transaction(IDatabaseEntityDefFactory entityDefFactory, IDatabaseEngine databaseEngine)
        {
            this.entityDefFactory = entityDefFactory;
            this.databaseEngine = databaseEngine;
        }

        public TransactionContext BeginTransaction<T>(IsolationLevel isolationLevel) where T : DatabaseEntity
        {
            DatabaseEntityDef entityDef = entityDefFactory.GetDef<T>();

            IDbTransaction dbTransaction = databaseEngine.BeginTransaction(entityDef.DatabaseName, isolationLevel);

            return new TransactionContext() {
                Transaction = dbTransaction,
                Status = TransactionStatus.InTransaction
            };
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit(TransactionContext context)
        {
            if (context == null || context.Transaction == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Status != TransactionStatus.InTransaction)
            {
                throw new DatabaseException("use a already finished transactioncontenxt");
            }

            try
            {
                IDbConnection conn = context.Transaction.Connection;
                databaseEngine.Commit(context.Transaction);
                //context.Transaction.Commit();
                context.Transaction.Dispose();

                if (conn != null && conn.State != ConnectionState.Closed)
                {
                    conn.Dispose();
                }

                context.Status = TransactionStatus.Commited;
            }
            catch
            {
                context.Status = TransactionStatus.Failed;
                throw;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback(TransactionContext context)
        {
            if (context == null || context.Transaction == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Status != TransactionStatus.InTransaction)
            {
                throw new DatabaseException("use a already finished transactioncontenxt");
            }

            try
            {
                IDbConnection conn = context.Transaction.Connection;

                databaseEngine.Rollback(context.Transaction);
                
                //context.Transaction.Rollback();
                context.Transaction.Dispose();

                if (conn != null && conn.State != ConnectionState.Closed)
                {
                    conn.Dispose();
                }

                context.Status = TransactionStatus.Rollbacked;
            }
            catch
            {
                context.Status = TransactionStatus.Failed;
                throw;
            }
        }

    }
}
