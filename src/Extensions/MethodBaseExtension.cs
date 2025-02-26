﻿using System;
using Rapidity.Http.Configurations;
using System.Reflection;
using Rapidity.Http.Attributes;

namespace Rapidity.Http.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    internal static class MethodBaseExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static NamedHttpConfigureItem GetConfigureItem(this MethodBase method)
        {
            var httpAttr = method.GetCustomAttribute<HttpServiceAttribute>(false);
            var cacheAttr = method.GetCustomAttribute<CacheAttribute>(false);
            var retryAttr = method.GetCustomAttribute<RetryAttribute>(false);
            var namedOption = new NamedHttpConfigureItem
            {
                Option = new HttpConfigureItem
                {
                    CacheOption = cacheAttr?.GetCacheOption(),
                    RetryOption = retryAttr?.GetRetryOption()
                }
            };
            if (httpAttr != null)
            {
                namedOption.Service = httpAttr.Service;
                namedOption.Module = httpAttr.Module;
                namedOption.Option.Uri = httpAttr.Uri;
                namedOption.Option.Method = httpAttr.HttpMethod;
                namedOption.Option.Encoding = httpAttr.Encoding;
                namedOption.Option.ContentType = httpAttr.ContentType;
                namedOption.Option.RequestBuilderType = httpAttr.RequestBuilderType;
                namedOption.Option.ResponseResolverType = httpAttr.ResponseResolverType;
            }
            return namedOption;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="onMethodConfig"></param>
        /// <returns></returns>
        public static NamedHttpConfigureItem GetConfigureItem(this Type type, NamedHttpConfigureItem onMethodConfig)
        {
            var httpAttr = type.GetCustomAttribute<HttpServiceAttribute>(false);
            var cacheAttr = type.GetCustomAttribute<CacheAttribute>(false);
            var retryAttr = type.GetCustomAttribute<RetryAttribute>(false);
            var namedOption = new NamedHttpConfigureItem
            {
                Option = new HttpConfigureItem
                {
                    CacheOption = cacheAttr?.GetCacheOption(),
                    RetryOption = retryAttr?.GetRetryOption()
                }
            };
            if (httpAttr != null)
            {
                namedOption.Service = httpAttr.Service;
                namedOption.Module = httpAttr.Module;
                namedOption.Option.Uri = httpAttr.Uri;
                namedOption.Option.Method = httpAttr.HttpMethod;
                namedOption.Option.Encoding = httpAttr.Encoding;
                namedOption.Option.ContentType = httpAttr.ContentType;
                namedOption.Option.RequestBuilderType = httpAttr.RequestBuilderType;
                namedOption.Option.ResponseResolverType = httpAttr.ResponseResolverType;
            }

            if (onMethodConfig != null)
            {
                if (string.IsNullOrEmpty(onMethodConfig.Service))
                    onMethodConfig.Service = namedOption.Service;
                if (string.IsNullOrEmpty(onMethodConfig.Module))
                    onMethodConfig.Module = namedOption.Module;
                namedOption.Option = onMethodConfig.Option.Union(namedOption.Option);
            }
            return namedOption;
        }
    }

    internal class NamedHttpConfigureItem
    {
        public string Service { get; set; }

        public string Module { get; set; }

        public HttpConfigureItem Option { get; set; }
    }
}