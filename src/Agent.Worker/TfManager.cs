using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class TfUtil
    {
        public static async Task DownloadTfLegacyAsync(IExecutionContext executionContext)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            string externalsPath = Path.Combine(executionContext.GetVariableValueOrDefault("Agent.HomeDirectory"), Constants.Path.ExternalsDirectory);
            ArgUtil.NotNull(externalsPath, nameof(externalsPath));

            string tfLegacyExternalsPath = Path.Combine(externalsPath, "tf-legacy");

            if (!Directory.Exists(tfLegacyExternalsPath))
            {
                const string tfDownloadUrl = "https://vstsagenttools.blob.core.windows.net/tools/vstsom/m153_47c0856d/vstsom.zip";

                string tempTfDirectory = Path.Combine(externalsPath, "tf_download_temp");
                Directory.CreateDirectory(tempTfDirectory);
                string downloadTfPath = Path.ChangeExtension(Path.Combine(tempTfDirectory, "tfdownload"), ".completed");

                if (!File.Exists(downloadTfPath))
                {
                    await DownloadAsync(downloadTfPath, tfDownloadUrl, executionContext);
                }
                else
                {
                    executionContext.Debug($"tf is already downloaded to {downloadTfPath}.");
                }

                try
                {
                    ZipFile.ExtractToDirectory(downloadTfPath, tfLegacyExternalsPath);
                    File.WriteAllText(downloadTfPath, DateTime.UtcNow.ToString());
                    executionContext.Debug("tf-legacy has been extracted and cleaned up");
                }
                catch (Exception ex)
                {
                    executionContext.Error(ex);
                }
            }
            else
            {
                executionContext.Debug($"tf-legacy download already exists at {tfLegacyExternalsPath}.");
            }

            string vstsomLegacyExternalsPath = Path.Combine(externalsPath, "vstsom-legacy");

            if (!Directory.Exists(vstsomLegacyExternalsPath))
            {
                const string vstsomDownloadUrl = "https://vstsagenttools.blob.core.windows.net/tools/vstsom/m122_887c6659/vstsom.zip";

                string tempVstsomDirectory = Path.Combine(externalsPath, "vstsom_download_temp");
                Directory.CreateDirectory(tempVstsomDirectory);
                string downloadVstsomPath = Path.ChangeExtension(Path.Combine(tempVstsomDirectory, "vstsomdownload"), ".completed");

                if (!File.Exists(downloadVstsomPath))
                {
                    await DownloadAsync(downloadVstsomPath, vstsomDownloadUrl, executionContext);
                }
                else
                {
                    executionContext.Debug($"vstsom is already downloaded to {downloadVstsomPath}.");
                }

                try
                {
                    ZipFile.ExtractToDirectory(downloadVstsomPath, vstsomLegacyExternalsPath);
                    File.WriteAllText(downloadVstsomPath, DateTime.UtcNow.ToString());
                    executionContext.Debug("vstsom-legacy has been extracted and cleaned up");
                }
                catch (Exception ex)
                {
                    executionContext.Error(ex);
                }
            }
            else
            {
                executionContext.Debug($"vstsom-legacy download already exists at {vstsomLegacyExternalsPath}.");
            }
        }

        private static async Task DownloadAsync(string downloadPath, string blobUrl, IExecutionContext executionContext)
        {
            int retryCount = 0;
            const int retryLimit = 3;
            const int timeout = 180;
            const int defaultFileStreamBufferSize = 4096;
            const int retryDelay = 10000;

            while (retryCount < retryLimit)
            {
                using CancellationTokenSource downloadCts = new(TimeSpan.FromSeconds(timeout));
                using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(downloadCts.Token, executionContext.CancellationToken);
                CancellationToken cancellationToken = linkedTokenSource.Token;

                try
                {
                    using HttpClient httpClient = new();
                    using Stream stream = await httpClient.GetStreamAsync(blobUrl, cancellationToken);
                    using FileStream fs = new(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: defaultFileStreamBufferSize, useAsync: true);

                    await stream.CopyToAsync(fs, cancellationToken);
                    executionContext.Debug("Finished downloading tool.");
                    await fs.FlushAsync(cancellationToken);
                    fs.Close();
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    executionContext.Debug("Tool download has been cancelled.");
                    throw;
                }
                catch (Exception)
                {
                    retryCount++;
                    executionContext.Debug("Failed to download tool");
                    executionContext.Debug("Retry tool download in 10 seconds.");
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }
    }
}
