// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(PagingLogger))]
    public interface IPagingLogger : IAgentService
    {
        long TotalLines { get; }
        void Setup(Guid timelineId, Guid timelineRecordId);

        void Write(string message);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716: Identifiers should not match keywords")]
        void End();
    }

    public class PagingLogger : AgentService, IPagingLogger, IDisposable
    {
        public static string PagingFolder = "pages";

        // 8 MB
        public const int PageSize = 8 * 1024 * 1024;

        private Guid _timelineId;
        private Guid _timelineRecordId;
        private string _pageId;
        private FileStream _pageData;
        private StreamWriter _pageWriter;
        private int _byteCount;
        private int _pageCount;
        private long _totalLines;
        private string _dataFileName;
        private string _pagesFolder;
        private IJobServerQueue _jobServerQueue;
        private const string groupStartTag = "##[group]";
        private const string groupEndTag = "##[endgroup]";
        private bool _groupOpened = false;
        public long TotalLines => _totalLines;

        public override void Initialize(IHostContext hostContext)
        {
            ArgUtil.NotNull(hostContext, nameof(hostContext));
            base.Initialize(hostContext);
            _totalLines = 0;
            _pageId = Guid.NewGuid().ToString();
            _pagesFolder = Path.Combine(hostContext.GetDiagDirectory(), PagingFolder);
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
            Directory.CreateDirectory(_pagesFolder);
        }

        public void Setup(Guid timelineId, Guid timelineRecordId)
        {
            _timelineId = timelineId;
            _timelineRecordId = timelineRecordId;
        }

        //
        // Write a metadata file with id etc, point to pages on disk.
        // Each page is a guid_#.  As a page rolls over, it events it's done
        // and the consumer queues it for upload
        // Ensure this is lazy.  Create a page on first write
        //
        public void Write(string message)
        {
            // lazy creation on write
            if (_pageWriter == null)
            {
                try
                {
                    Create();
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                    // If we cannot create a page, drop the write to avoid crashing the agent.
                    return;
                }
            }

            if (message.Contains(groupStartTag, StringComparison.OrdinalIgnoreCase))
            {
                _groupOpened = true;
            } 
            if (_groupOpened && message.Contains(groupEndTag, StringComparison.OrdinalIgnoreCase))
            {
                // Ignore group end tag only if group was opened, otherwise it is a normal message 
                // because in web console ##[endgroup] becomes empty line without ##[group] tag
                _groupOpened = false;
                _totalLines--;
            } 

            string line = $"{DateTime.UtcNow.ToString("O")} {message}";
            try
            {
                _pageWriter.WriteLine(line);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                return; // best effort logging
            }

            _totalLines++;
            if (line.IndexOf('\n') != -1)
            {
                foreach (char c in line)
                {
                    if (c == '\n')
                    {
                        _totalLines++;
                    }
                }
            }

            _byteCount += System.Text.Encoding.UTF8.GetByteCount(line);
            if (_byteCount >= PageSize)
            {
                try
                {
                    NewPage();
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                }
            }
        }

        public void End()
        {
            EndPage();
        }

        private void Create()
        {
            NewPage();
        }

        private void NewPage()
        {
            EndPage();
            _byteCount = 0;
            _dataFileName = Path.Combine(_pagesFolder, $"{_pageId}_{++_pageCount}.log");
            _pageData = new FileStream(_dataFileName, FileMode.CreateNew);
            _pageWriter = new StreamWriter(_pageData, System.Text.Encoding.UTF8);
        }

        private void EndPage()
        {
            if (_pageWriter != null)
            {
                try
                {
                    _pageWriter.Flush();
                    _pageData.Flush();
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                }
                finally
                {
                    try { _pageWriter.Dispose(); } catch { }
                    _pageWriter = null;
                    try { _pageData.Dispose(); } catch { }
                    _pageData = null;
                }

                try
                {
                    _jobServerQueue.QueueFileUpload(_timelineId, _timelineRecordId, "DistributedTask.Core.Log", "CustomToolLog", _dataFileName, true);
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                EndPage();
            }
        }

    }
}
