// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class TfUtil
    {
        public static async Task DownloadTfLegacyAsync(string externalsPath, Action<string> debug, CancellationToken cancellationToken)
        {
            ArgUtil.NotNull(externalsPath, nameof(externalsPath));
            ArgUtil.NotNull(debug, nameof(debug));

            string vstsomExternalsPath = Path.Combine(externalsPath, "vstsom-legacy");
            string tfExternalsPath = Path.Combine(externalsPath, "tf-legacy");

            if (!Directory.Exists(tfExternalsPath))
            {
                const string tfDownloadUrl = "https://vstsagenttools.blob.core.windows.net/tools/vstsom/m153_47c0856d/vstsom.zip";
                string tempTfDirectory = Path.Combine(externalsPath, "tf_download_temp");
                Directory.CreateDirectory(tempTfDirectory);
                string downloadTfPath = Path.ChangeExtension(Path.Combine(tempTfDirectory, "tftemp"), ".completed");

                if (!File.Exists(downloadTfPath))
                {
                    await DownloadAsync(downloadTfPath, tfDownloadUrl, debug, cancellationToken);
                }
                else
                {
                    //executionContext.Debug($"Git intance {version} already downloaded.");
                }
            }
            else
            {
                //executionContext.Debug($"Git instance {gitFileName} already exists.");
            }

            if (!Directory.Exists(vstsomExternalsPath))
            {
                const string vstsomDownloadUrl = "https://vstsagenttools.blob.core.windows.net/tools/vstsom/m122_887c6659/vstsom.zip";
                string tempVstsomDirectory = Path.Combine(externalsPath, "vstsom_download_temp");
                Directory.CreateDirectory(tempVstsomDirectory);
                string downloadVstsomPath = Path.ChangeExtension(Path.Combine(tempVstsomDirectory, "vstsomtemp"), ".completed");

                //executionContext.Debug($"Git intance {version} already downloaded.");

                if (!File.Exists(downloadVstsomPath))
                {
                    await DownloadAsync(downloadVstsomPath, vstsomDownloadUrl, debug, cancellationToken);
                }
                else
                {
                    //executionContext.Debug($"Git instance {gitFileName} already exists.");
                }
            }
            else
            {
                //executionContext.Debug($"Git instance {gitFileName} already exists.");
            }
        }

        private static async Task DownloadAsync(string downloadPath, string blobUrl, Action<string> debug, CancellationToken cancellationToken)
        {
            debug($@"Tool zip file will be downloaded and saved as {downloadPath}");

            int retryCount = 0;
            const int retryLimit = 3;
            const int timeout = 180;
            const int defaultFileStreamBufferSize = 4096;
            const int retryDelay = 10000;

            while (retryCount < retryLimit)
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(timeout));
                using CancellationTokenSource downloadCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

                try
                {
                    using HttpClient httpClient = new();
                    using Stream stream = await httpClient.GetStreamAsync(blobUrl);
                    using FileStream fs = new(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: defaultFileStreamBufferSize, useAsync: true);

                    await stream.CopyToAsync(fs);
                    debug("Finished downloading tool.");
                    await fs.FlushAsync(downloadCts.Token);
                    fs.Close();
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    debug("Tool download has been cancelled.");
                    throw;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    debug("Failed to download tool");
                    //Trace.Error(ex);
                    debug("Retry tool download in 10 seconds.");
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }
    }
}