﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HB.FullStack.Client.Api;
using HB.FullStack.Client.IdBarriers;
using HB.FullStack.Common.Api;
using HB.FullStack.Common.IdGen;
using HB.FullStack.Common.Resources;
using HB.FullStack.Database;

namespace MyColorfulTime.IdBarriers
{

    internal class IdBarrierService : IIdBarrierService
    {
        enum ChangeDirection
        {
            ToServer,
            FromServer
        }
        private readonly IIdBarrierRepo _idBarrierRepo;
        private readonly IApiClient _apiClient;
        private readonly ITransaction _transaction;
        private readonly Dictionary<string, List<long>> _addRequestClientIdDict = new Dictionary<string, List<long>>();

        public IdBarrierService(IIdBarrierRepo idBarrierRepo, IApiClient apiClient, ITransaction transaction)
        {
            _idBarrierRepo = idBarrierRepo;
            _apiClient = apiClient;
            _transaction = transaction;
        }

        public void Initialize()
        {
            _apiClient.Requesting += ApiClient_RequestingAsync;
            _apiClient.Responsed += ApiClient_ResponsedAsync;
        }

        //考虑手工硬编码
        private async Task ApiClient_RequestingAsync(ApiRequest request, ApiEventArgs args)
        {
            if (args.RequestType == ApiRequestType.Add)
            {
                _addRequestClientIdDict[request.GetRequestId()] = new List<long>();
            }

            await ChangeIdAsync(request, args.RequestId, ChangeDirection.ToServer, args.RequestType).ConfigureAwait(false);
        }

        private async Task ApiClient_ResponsedAsync(object? sender, ApiEventArgs args)
        {
            switch (args.RequestType)
            {
                case ApiRequestType.Add:

                    if (sender is IEnumerable<long> servierIds)
                    {
                        List<long> clientIds = _addRequestClientIdDict[args.RequestId];

                        await AddServerIdToClientIdAsync(servierIds, clientIds).ConfigureAwait(false);

                        _addRequestClientIdDict.Remove(args.RequestId);
                    }

                    break;
                case ApiRequestType.Update:
                    break;
                case ApiRequestType.Delete:
                    break;
                case ApiRequestType.Get:
                    if (sender is IEnumerable enumerable)
                    {
                        foreach (object obj in enumerable)
                        {
                            await ChangeIdAsync(obj, args.RequestId, ChangeDirection.FromServer, args.RequestType).ConfigureAwait(false);
                        }
                    }
                    break;
                case ApiRequestType.GetSingle:
                    await ChangeIdAsync(sender, args.RequestId, ChangeDirection.FromServer, args.RequestType).ConfigureAwait(false);
                    break;
                default:
                    break;
            }
        }



        private async Task ChangeIdAsync(object? obj, string requestId, ChangeDirection direction, ApiRequestType requestType)
        {
            if (obj == null) { return; }

            //替换ID
            foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(IdBarrierAttribute))))
            {
                object? propertyValue = propertyInfo.GetValue(obj);

                if (propertyValue == null)
                {
                    continue;
                }

                if (propertyValue is long id)
                {
                    await ConvertLongIdAsync(obj, id, propertyInfo, requestType, direction, requestId).ConfigureAwait(false);
                }
                else if (propertyValue is IEnumerable<long> longIds)
                {
                    foreach (long iditem in longIds)
                    {
                        await ConvertLongIdAsync(obj, iditem, propertyInfo, requestType, direction, requestId).ConfigureAwait(false);
                    }
                }
                else if (propertyValue is IEnumerable enumerable)
                {
                    foreach (object subObj in enumerable)
                    {
                        await ChangeIdAsync(subObj, requestId, direction, requestType).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new ClientException($"Id Barrier碰到无法解析的类型");
                }
            }
        }

        private async Task ConvertLongIdAsync(object obj, long id, PropertyInfo propertyInfo, ApiRequestType requestType, ChangeDirection direction, string requestId)
        {
            if (id < 0)
            {
                return;
            }

            if (propertyInfo.Name == nameof(Resource.Id) && requestType == ApiRequestType.Add && direction == ChangeDirection.ToServer)
            {
                _addRequestClientIdDict[requestId].Add(id);

                propertyInfo.SetValue(obj, -1);

                return;
            }

            long changedId = direction switch
            {
                ChangeDirection.ToServer => await _idBarrierRepo.GetServerIdAsync(id).ConfigureAwait(false),
                ChangeDirection.FromServer => await _idBarrierRepo.GetClientIdAsync(id).ConfigureAwait(false),
                _ => -1,
            };

            if (changedId < 0 &&
                //propertyInfo.Name == nameof(Resource.Id) &&
                (requestType == ApiRequestType.Get || requestType == ApiRequestType.GetSingle) &&
                direction == ChangeDirection.FromServer)
            {
                changedId = IDistributedIdGen.IdGen.GetId();
                await AddServerIdToClientIdAsync(id, changedId).ConfigureAwait(false);
            }

            propertyInfo.SetValue(obj, changedId);
        }

        private Task AddServerIdToClientIdAsync(long serverId, long clientId)
        {
            if (serverId <= 0)
            {
                return Task.CompletedTask;
            }

            return _idBarrierRepo.AddIdBarrierAsync(clientId: clientId, serverId: serverId);
        }

        private async Task AddServerIdToClientIdAsync(IEnumerable<long> serverIds, List<long> clientIds)
        {
            List<long> serverAdds = new List<long>();
            List<long> clientAdds = new List<long>();

            int num = 0;

            foreach (long serverId in serverIds)
            {
                if (serverId <= 0)
                {
                    continue;
                }

                serverAdds.Add(serverId);
                clientAdds.Add(clientIds[num++]);
            }

            TransactionContext trans = await _transaction.BeginTransactionAsync<IdBarrier>().ConfigureAwait(false);
            try
            {
                await _idBarrierRepo.AddIdBarrierAsync(clientIds: clientAdds, servierIds: serverAdds, trans).ConfigureAwait(false);

                await trans.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await trans.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
