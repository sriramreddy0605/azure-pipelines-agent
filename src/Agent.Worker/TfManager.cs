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
        public static async Task DownloadLegacyTfToolsAsync(IExecutionContext executionContext)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            string externalsPath = Path.Combine(executionContext.GetVariableValueOrDefault("Agent.HomeDirectory"), Constants.Path.ExternalsDirectory);
            ArgUtil.NotNull(externalsPath, nameof(externalsPath));

            string tfLegacyExternalsPath = Path.Combine(externalsPath, "tf-legacy");

            if (!Directory.Exists(tfLegacyExternalsPath))
            {
                const string tfDownloadUrl = "https://vstsagenttools.blob.core.windows.net/tools/vstsom/m153_47c0856d/vstsom.zip";
                string tempTfDirectory = Path.Combine(externalsPath, "tf_download_temp");

                await InstallAsync(executionContext, tfDownloadUrl, tempTfDirectory, tfLegacyExternalsPath);
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

                await InstallAsync(executionContext, vstsomDownloadUrl, tempVstsomDirectory, vstsomLegacyExternalsPath);
            }
            else
            {
                executionContext.Debug($"vstsom-legacy download already exists at {vstsomLegacyExternalsPath}.");
            }
        }

        private static async Task InstallAsync(IExecutionContext executionContext, string blobUrl, string tempDirectory, string extractPath)
        {
            Directory.CreateDirectory(tempDirectory);
            string downloadPath = Path.ChangeExtension(Path.Combine(tempDirectory, "download"), ".completed");
            string toolName = new DirectoryInfo(extractPath).Name;

            if (!File.Exists(downloadPath))
            {
                await DownloadAsync(executionContext, toolName, blobUrl, downloadPath);
            }
            else
            {
                executionContext.Debug($"{toolName} is already downloaded to {downloadPath}.");
                return;
            }

            try
            {
                executionContext.Debug($"Extracting {toolName}...");
                ZipFile.ExtractToDirectory(downloadPath, extractPath);
                File.WriteAllText(downloadPath, DateTime.UtcNow.ToString());
                executionContext.Debug($"{toolName} has been extracted and cleaned up");
            }
            catch (Exception ex)
            {
                executionContext.Error(ex);
            }
        }

        private static async Task DownloadAsync(IExecutionContext executionContext, string toolName, string blobUrl, string downloadPath)
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
                    executionContext.Debug($"Finished downloading {toolName}.");
                    await fs.FlushAsync(cancellationToken);
                    fs.Close();
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    executionContext.Debug($"{toolName} download has been cancelled.");
                    throw;
                }
                catch (Exception)
                {
                    retryCount++;
                    executionContext.Debug($"Failed to download {toolName}");
                    executionContext.Debug($"Retry {toolName} download in 10 seconds.");
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }
    }
}
