using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Compressor.Constants;
using Compressor.Models;
using Microsoft.VisualBasic.Devices;

namespace Compressor
{
    public class GZipCompressor
    {
        private const int ChunkSize = 1024 * 1024;

        private const string DirectoryCreationError =
            "Ошибка при попытке создания директории выходного файла. Проверьте имя директории и права на возможность ее создания";

        private readonly AutoResetEvent _autoResetEvent;
        private readonly Queue<Tuple<int, byte[]>> _chunksQueue;
        private readonly object _locker = new object();
        private readonly ulong _maxChunksCount;
        private bool _hasFileRead;

        public GZipCompressor()
        {
            _maxChunksCount = (ulong)(new ComputerInfo().TotalPhysicalMemory / (double) ChunkSize * 0.9);
            _chunksQueue = new Queue<Tuple<int, byte[]>>();
            _autoResetEvent = new AutoResetEvent(true);
        }

        public int ProcessFileAccordingToCompressionMode(ParamsModel paramsModel)
        {
            _hasFileRead = false;
            if (!paramsModel.CompressionMode.HasValue)
                throw new ArgumentNullException(ParamsValidationErrorMessages.CompressionModeIsRequired);

            var readingThread = new Thread(ReadFileToMemory) {IsBackground = true};
            var writingThread = new Thread(ProcessFile) {IsBackground = true};

            readingThread.Start(paramsModel);
            writingThread.Start(paramsModel);

            readingThread.Join();
            writingThread.Join();

            return 0;
        }

        private void ReadFileToMemory(object threadContext)
        {
            var paramsModel = (ParamsModel)threadContext;
            var stream = paramsModel.CompressionMode == CompressionMode.Decompress
                ? (Stream)new GZipStream(new FileStream(paramsModel.InputFileName, FileMode.Open, FileAccess.Read),
                    CompressionMode.Decompress)
                : new FileStream(paramsModel.InputFileName, FileMode.Open, FileAccess.Read);

            var readingThreads = new List<Thread>();
            using (stream)
            {
                for (var i = 0; i < Environment.ProcessorCount; i++)
                {
                    var readingThread = new Thread(ReadFileInOneThread) { IsBackground = true };
                    readingThread.Start(stream);
                    readingThreads.Add(readingThread);
                }

                readingThreads.ForEach(thread => thread.Join());
            }

            _hasFileRead = true;
        }

        private void ProcessFile(object threadContext)
        {
            var paramsModel = (ParamsModel)threadContext;
            var stream = paramsModel.CompressionMode != CompressionMode.Compress
                ? new FileStream(paramsModel.OutputFileName, FileMode.Create, FileAccess.Write)
                : (Stream)new GZipStream(new FileStream(paramsModel.OutputFileName, FileMode.Create, FileAccess.Write),
                    CompressionMode.Compress);

            CreateOutputFileDirectoryIfNotExists(paramsModel);
            using (stream)
            {
                while (true)
                {
                    var dataChunk = DequeueChunk();
                    if (dataChunk == null)
                    {
                        if (_hasFileRead)
                            break;

                        continue;
                    }

                    stream.Write(dataChunk.Item2, 0, dataChunk.Item1);
                }
            }
        }

        private void ReadFileInOneThread(object threadContext)
        {
            var stream = (Stream)threadContext;
            while (true)
            {
                _autoResetEvent.WaitOne();
                var buffer = new byte[ChunkSize];
                var bytesCount = stream.Read(buffer, 0, buffer.Length);
                if (bytesCount == 0)
                    break;

                EnqueueChunk(new Tuple<int, byte[]>(bytesCount, buffer));
                _autoResetEvent.Set();
            }

            _autoResetEvent.Set();
        }

        private void CreateOutputFileDirectoryIfNotExists(ParamsModel paramsModel)
        {
            var directoryName = Path.GetDirectoryName(paramsModel.OutputFileName);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName ?? throw new ArgumentNullException(DirectoryCreationError));
        }

        private void EnqueueChunk(Tuple<int, byte[]> dataPair)
        {
            try
            {
                while ((ulong)_chunksQueue.Count >= _maxChunksCount)
                {
                }

                Monitor.Enter(_locker);
                _chunksQueue.Enqueue(dataPair);
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }

        private Tuple<int, byte[]> DequeueChunk()
        {
            try
            {
                Monitor.Enter(_locker);
                return !_chunksQueue.Any() ? null : _chunksQueue.Dequeue();
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }
    }
}