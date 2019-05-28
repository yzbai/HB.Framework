﻿using HB.Component.Authorization.Entity;
using HB.Framework.Database;
using HB.Framework.Database.Transaction;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace HB.Component.Authorization.Abstractions
{
    public interface ISignInTokenBiz
    {
        Task<SignInToken> CreateAsync(string userGuid, string clientId, string clientType, string clientVersion, string clientAddress, string ipAddress, TimeSpan expireTimeSpan, TransactionContext transContext = null);
        Task<AuthorizationServerResult> DeleteByUserGuidAsync(string userGuid, TransactionContext transContext = null);
        Task<AuthorizationServerResult> DeleteAppClientTokenByUserGuidAsync(string userGuid, TransactionContext transContext = null);
        Task<AuthorizationServerResult> DeleteAsync(string signInTokenGuid, TransactionContext transContext = null);
        Task<SignInToken> GetAsync(string signInTokenGuid, string refreshToken, string clientId, string userGuid, TransactionContext transContext = null);
        Task<AuthorizationServerResult> UpdateAsync(SignInToken signInToken, TransactionContext transContext = null);
    }
}
