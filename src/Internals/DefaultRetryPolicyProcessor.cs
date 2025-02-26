﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rapidity.Http.Configurations;

namespace Rapidity.Http
{
    /// <summary>
    /// 重试/熔断降级Processor
    /// </summary>
    internal class DefaultRetryPolicyProcessor : IRetryPolicyProcessor
    {
        private readonly ILogger<DefaultRetryPolicyProcessor> _logger;

        public DefaultRetryPolicyProcessor(ILogger<DefaultRetryPolicyProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sending"></param>
        /// <returns></returns>
        public async Task<ResponseWrapperResult> ProcessAsync(RetryPolicyContext context, Func<HttpRequest, Task<HttpResponse>> sending)
        {
            var records = new List<RequestRecord>();
            var option = context.Option;
            await HandleException(context.Request, async () =>
             {
                 var timeoutToken = (option?.TotalTimeout ?? 0) > 0
                     ? new CancellationTokenSource(TimeSpan.FromMilliseconds(option.TotalTimeout))
                     : new CancellationTokenSource();
                 return await SendWithRetryAsync(option, context.Request, sending, records, timeoutToken.Token);
             }, record =>
             {
                 //只有执行超时才会抛ExecutionHttpException，同时意味着重试结束
                 if (record.Exception is ExecutionHttpException executionEx)
                 {
                     record.Request = executionEx.Request;
                     record.Exception = executionEx.InnerException;
                     record.Duration = GetDuration(record.Request.TimeStamp);
                     records.Add(record);
                 }
                 return record;
             });
            var result = new ResponseWrapperResult
            {
                Request = context.Request,
                Records = records,
                Duration = GetDuration(context.Request.TimeStamp)
            };
            var recordsCount = result.Records.Count();
            result.RetryCount = recordsCount > 1 ? recordsCount - 1 : 0;
            //获取有效请求记录
            var validRecord = result.Records.LastOrDefault(x => x.Response != null) ?? result.Records.Last();
            result.Response = validRecord.Response;
            if (result.Response != null)
                result.RawResponse = await result.Response.Content.ReadAsStringAsync();
            result.Exception = validRecord.Exception;
            _logger.LogInformation($"请求{context.Request.RequestUri}执行完毕，用时:{result.Duration}ms");
            return result;
        }

        /// <summary>
        /// 重试逻辑
        /// </summary>
        /// <param name="option"></param>
        /// <param name="request"></param>
        /// <param name="sending"></param>
        /// <param name="records"></param>
        /// <param name="timeoutToken"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        private async Task<RequestRecord> SendWithRetryAsync(RetryOption option, HttpRequest request,
            Func<HttpRequest, Task<HttpResponse>> sending,
            List<RequestRecord> records, CancellationToken timeoutToken, int retryCount = 0)
        {
            if (timeoutToken.IsCancellationRequested)
                throw new ExecutionHttpException(request, new TimeoutException($"请求超时，在{option.TotalTimeout}ms内未获取到结果"));
            records = records ?? new List<RequestRecord>();
            var record = await HandleException(request, async () =>
             {
                 var response = await sending(request);
                 var requestRecord = new RequestRecord
                 {
                     Request = request,
                     Response = response,
                     Duration = response.Duration
                 };
                 return requestRecord;
             }, requestRecord =>
             {
                 records.Add(requestRecord);
                 return requestRecord;
             });

            if (!CanRetry(option, record, retryCount)) return record;
            request = request.Clone();
            var waitTime = option.WaitIntervals[retryCount];
            await Waiting(request, waitTime, option.TotalTimeout, timeoutToken);
            return await SendWithRetryAsync(option, request, sending, records, timeoutToken, ++retryCount);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="waitMilliseconds"></param>
        /// <param name="totalTimeout"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task Waiting(HttpRequest request, int waitMilliseconds, int totalTimeout, CancellationToken token)
        {
            if (waitMilliseconds <= 0)
                await Task.FromResult(0);
            try
            {
                await Task.Delay(waitMilliseconds, token);
            }
            catch //将OperationCanceledException异常转换为timeoutException
            {
                throw new ExecutionHttpException(request, new TimeoutException($"请求超时，在{totalTimeout}ms内未获取到结果"));
            }
        }

        /// <summary>
        /// 检查是否满足重试条件
        /// </summary>
        /// <param name="option"></param>
        /// <param name="record"></param>
        /// <param name="retryCount"></param>
        /// <returns></returns>
        private bool CanRetry(RetryOption option, RequestRecord record, int retryCount)
        {
            if (option == null)
                return false;

            if (retryCount >= option.RetryCount)
                return false;

            if ((option.TransientErrorRetry ?? false) && record.Exception != null)
            {
                if (record.Exception is TimeoutException
                    || record.Exception is OperationCanceledException
                    || record.Exception is HttpRequestException)
                    return true;
                return false;
            }

            var statusCode = (int)record.Response.StatusCode;
            var method = record.Response.RequestMessage.Method.ToString();
            if (option.RetryCount > 0
                && (option.RetryStatusCodes?.Contains(statusCode) ?? false)
                && (option.RetryMethods?.Contains(method, StringComparer.CurrentCultureIgnoreCase) ?? false))
                return true;
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="running"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private async Task<RequestRecord> HandleException(HttpRequest request, Func<Task<RequestRecord>> running, Func<RequestRecord, RequestRecord> callback)
        {
            RequestRecord record = null;
            try
            {
                record = await running();
            }
            catch (Exception ex)
            {
                record = new RequestRecord
                {
                    Request = request,
                    Exception = ex,
                    Duration = GetDuration(request.TimeStamp)
                };
            }
            return callback(record);
        }

        private long GetDuration(long beginTicks)
        {
            return (long)TimeSpan.FromTicks(DateTime.Now.Ticks - beginTicks).TotalMilliseconds;
        }
    }
}