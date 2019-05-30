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

        private bool _hasFileRead;

        public GZipCompressor()
        {
            _chunksQueue = new Queue<Tuple<int, byte[]>>(Convert.ToInt32(
                new ComputerInfo().AvailablePhysicalMemory * 0.8 / ChunkSize));
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

        private static void CreateOutputFileDirectoryIfNotExists(ParamsModel paramsModel)
        {
            var directoryName = Path.GetDirectoryName(paramsModel.OutputFileName);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName ?? throw new ArgumentNullException(DirectoryCreationError));
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

        private void EnqueueChunk(Tuple<int, byte[]> dataPair)
        {
            try
            {
                Monitor.Enter(_locker);
                _chunksQueue.Enqueue(dataPair);
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }

        private void ProcessFile(object threadContext)
        {
            var paramsModel = (ParamsModel) threadContext;
            var stream = paramsModel.CompressionMode != CompressionMode.Compress
                ? new FileStream(paramsModel.OutputFileName, FileMode.Create, FileAccess.Write)
                : (Stream) new GZipStream(new FileStream(paramsModel.OutputFileName, FileMode.Create, FileAccess.Write),
                    CompressionMode.Compress);

            CreateOutputFileDirectoryIfNotExists(paramsModel);

            using (stream)
            {
                while (true)
                {
                    _autoResetEvent.WaitOne();
                    var dataChunk = DequeueChunk();
                    if (dataChunk == null)
                    {
                        if (_hasFileRead)
                            break;

                        _autoResetEvent.Set();
                        continue;
                    }

                    stream.Write(dataChunk.Item2, 0, dataChunk.Item1);
                    _autoResetEvent.Set();
                }
            }

            _autoResetEvent.Set();
        }

        private void ReadFileToMemory(object threadContext)
        {
            var paramsModel = (ParamsModel) threadContext;
            var stream = paramsModel.CompressionMode == CompressionMode.Decompress
                ? (Stream) new GZipStream(new FileStream(paramsModel.InputFileName, FileMode.Open, FileAccess.Read),
                    CompressionMode.Decompress)
                : new FileStream(paramsModel.InputFileName, FileMode.Open, FileAccess.Read);

            using (stream)
            {
                var buffer = new byte[ChunkSize];
                int bytesCount;
                while ((bytesCount = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    _autoResetEvent.WaitOne();
                    EnqueueChunk(new Tuple<int, byte[]>(bytesCount, buffer));
                    buffer = new byte[ChunkSize];
                    _autoResetEvent.Set();
                }

                _hasFileRead = true;
            }
        }
    }
}