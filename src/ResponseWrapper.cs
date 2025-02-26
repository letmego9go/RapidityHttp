﻿using System;
using System.Collections.Generic;

namespace Rapidity.Http
{
    /// <summary>
    /// 
    /// </summary>
    public class ResponseWrapper
    {
        /// <summary>
        /// 响应结果
        /// </summary>
        public HttpResponse Response { get; set; }
        /// <summary>
        /// 响应文本数据
        /// </summary>
        public string RawResponse { get; set; }
        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get; set; }
        /// <summary>
        /// 是否进行了重试
        /// </summary>
        public bool HasRetry => RetryCount > 0;
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }
        /// <summary>
        /// 是否命中缓存
        /// </summary>
        public bool HasHitCache { get; set; }
        /// <summary>
        /// 执行时间 ms
        /// </summary>
        public long Duration { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class ResponseWrapper<TData> : ResponseWrapper
    {
        public TData Data { get; set; }

        public ResponseWrapper() { }

        public ResponseWrapper(ResponseWrapper wrapper)
        {
            this.Response = wrapper.Response;
            this.RawResponse = wrapper.RawResponse;
            this.Exception = wrapper.Exception;
            this.HasHitCache = wrapper.HasHitCache;
            this.RetryCount = wrapper.RetryCount;
            this.Duration = wrapper.Duration;
        }
    }


    /// <summary>
    /// fulled response data
    /// </summary>
    public class ResponseWrapperResult : ResponseWrapper
    {
        /// <summary>
        /// 初始请求
        /// </summary>
        public HttpRequest Request { get; set; }

        /// <summary>
        /// 请求过程记录
        /// </summary>
        public IEnumerable<RequestRecord> Records { get; set; }
    }
}