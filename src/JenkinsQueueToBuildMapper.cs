using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;

using log4net;

namespace JenkinsPlug
{
    internal class JenkinsQueueToBuildMapper
    {
        internal JenkinsQueueToBuildMapper(HttpClient httpClient, string mapStorageFile)
        {
            mHttpClient = httpClient;
            mMapStorageFile = mapStorageFile;
        }

        internal void Start()
        {
            if (mbIsInitialized)
                return;

            IdMappingsStorage.Load(mMapStorageFile, mResolvedIdsCache);
            ThreadPool.QueueUserWorkItem(StartResolvingPendingMappings);
            mbIsInitialized = true;
        }

        internal void Stop()
        {
            mbStopRequested = true;
        }

        internal void SetAsPendingToResolve(string queueItemId)
        {
            if (!mbIsInitialized)
                throw BuildNotInitializedException();

            lock (mSyncLock)
            {
                if (mResolvedIdsCache.ContainsKey(queueItemId))
                    return;

                mPendingIds.Add(queueItemId);
            }
        }

        internal bool IsPendingToResolve(string queuedItemId)
        {
            if (!mbIsInitialized)
                throw BuildNotInitializedException();

            lock (mSyncLock)
            {
                return mPendingIds.Contains(queuedItemId);
            }
        }

        internal string GetBuildUrl(string queueItemId)
        {
            if (!mbIsInitialized)
                throw BuildNotInitializedException();

            lock (mSyncLock)
            {
                if (!mResolvedIdsCache.ContainsKey(queueItemId))
                    return null;

                return mResolvedIdsCache[queueItemId];
            }
        }

        internal void Clear(string queueItemId)
        {
            lock (mSyncLock)
            {
                if (!mResolvedIdsCache.ContainsKey(queueItemId))
                    return;

                mResolvedIdsCache.Remove(queueItemId);
            }
        }

        void StartResolvingPendingMappings(object state)
        {
            while (true)
            {
                if (mbStopRequested)
                    return;

                Thread.Sleep(QUERY_ALL_PENDINGS_INTERVAL_MILLIS);

                try
                {
                    ResolvePendingMappings(mPendingIds, mResolvedIdsCache, mMapStorageFile);
                }
                catch(Exception e)
                {
                    mLog.ErrorFormat("Error resolving pending mappings: {0}", e.Message);
                    ExceptionLogger.Log(e);
                }
            }
        }

        void ResolvePendingMappings(
            List<string> pendingIds,
            Dictionary<string, string> resolvedIdsCache,
            string mapStorageFile)
        {
            lock (mSyncLock)
            {
                if (pendingIds == null || pendingIds.Count == 0)
                    return;

                bool bNeedsToSaveToStorage = false;
                for (int i = pendingIds.Count - 1; i >= 0; i--)
                {
                    if (mbStopRequested)
                        return;

                    string buildIdUrl;
                    if (TryResolveQueuedId(pendingIds[i], mHttpClient, out buildIdUrl))
                    {
                        resolvedIdsCache[pendingIds[i]] = buildIdUrl;
                        pendingIds.RemoveAt(i);
                        bNeedsToSaveToStorage = true;
                    }

                    Thread.Sleep(QUERY_AMONG_PENDINGS_INTERVAL_MILLIS);
                }

                if (bNeedsToSaveToStorage)
                    IdMappingsStorage.Save(mapStorageFile, resolvedIdsCache);
            }
        }
        bool TryResolveQueuedId(string queuedItemId, HttpClient httpClient, out string buildIdUrl)
        {
            buildIdUrl = null;
            string queuedItemIdEndpoint = string.Format(GET_QUEUED_ITEM_URI_FORMAT, queuedItemId);
            HttpResponseMessage response = httpClient.GetAsync(queuedItemIdEndpoint).Result;

            if (!response.IsSuccessStatusCode)
                return false;

            string responseStr = response.Content.ReadAsStringAsync().Result;

            buildIdUrl = XmlNodeLoader.LoadValue(responseStr, "/leftItem/executable/url");
            return !string.IsNullOrEmpty(buildIdUrl);
        }

        Exception BuildNotInitializedException()
        {
            return new InvalidOperationException("JenkinsQueueToBuildMapper not started!");
        }

        HttpClient mHttpClient;
        string mMapStorageFile;

        Dictionary<string, string> mResolvedIdsCache = new Dictionary<string, string>();
        List<string> mPendingIds = new List<string>();

        volatile bool mbStopRequested = false;
        bool mbIsInitialized = false;
        readonly object mSyncLock = new object();
        const int QUERY_ALL_PENDINGS_INTERVAL_MILLIS = 10 * 1000;
        const int QUERY_AMONG_PENDINGS_INTERVAL_MILLIS = 500;

        const string GET_QUEUED_ITEM_URI_FORMAT = "queue/item/{0}/api/xml";

        static readonly ILog mLog = LogManager.GetLogger("jenkinsplug");
    }
}
