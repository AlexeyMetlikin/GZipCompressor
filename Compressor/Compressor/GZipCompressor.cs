using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using Compressor.Extensions;
using Compressor.Models;

namespace Compressor
{
    public class GZipCompressor
    {
        private const int ChunkSize = 1024 * 1024;

        private const string DirectoryCreationError =
            "Ошибка при попытке создания директории выходного файла. Проверьте имя директории и права на возможность ее создания";

        private const string FileDecompressError =
            "Ошибка разархивирования файла: выбран некорректный исходный файл либо файл поврежден";

        private const int ManagedBytesLength = sizeof(int);
        private readonly object _dictionaryLocker = new object();
        private readonly AutoResetEvent _eventSignal;
        private readonly Dictionary<int, Stream> _resultStreams = new Dictionary<int, Stream>();
        private readonly object _threadLocker = new object();
        private readonly Queue<Thread> _threadsQueue;
        private bool _hasFileRead;

        public GZipCompressor()
        {
            _threadsQueue = new Queue<Thread>();
            _eventSignal = new AutoResetEvent(false);
        }

        public int ProcessFileAccordingToCompressionMode(ParamsModel paramsModel)
        {
            _hasFileRead = false;
            CreateOutputFileDirectoryIfNotExists(paramsModel);

            using (var sourceStream = new FileStream(paramsModel.InputFileName, FileMode.Open, FileAccess.Read,
                FileShare.Read))
            {
                using (var targetStream = new FileStream(paramsModel.OutputFileName, FileMode.Create))
                {
                    ProcessFile(targetStream, sourceStream, paramsModel.CompressionMode);
                }
            }

            return 0;
        }

        private static void CreateOutputFileDirectoryIfNotExists(ParamsModel paramsModel)
        {
            var directoryName = Path.GetDirectoryName(paramsModel.OutputFileName);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName ?? throw new ArgumentNullException(DirectoryCreationError));
        }

        private void CompressFileThread(object threadContext)
        {
            var sourceStream = (Stream) threadContext;
            var taskCounter = 0;
            int bytesRead;
            var bufferRead = new byte[ChunkSize];

            while ((bytesRead = sourceStream.Read(bufferRead, 0, ChunkSize)) != 0)
            {
                var bytesToCompress = bufferRead;
                var bytesLength = bytesRead;
                var arrayIndex = taskCounter;
                var thread = new Thread(() => CompressOneArray(bytesToCompress, bytesLength, arrayIndex))
                {
                    IsBackground = true
                };
                thread.Start();
                EnqueueThread(thread);
                _eventSignal.WaitOne();
                taskCounter++;
                bufferRead = new byte[ChunkSize];
            }

            _hasFileRead = true;
        }

        private void CompressOneArray(byte[] bytesToCompress, int length, int index)
        {
            _eventSignal.Set();
            var stream = new MemoryStream();
            using (var gzStream = new GZipStream(stream, CompressionMode.Compress, true))
            {
                gzStream.Write(bytesToCompress, 0, length);
            }

            _resultStreams.SafeAdd(index, stream, _dictionaryLocker);
        }

        private void DecompressFileThread(object objectContext)
        {
            var sourceStream = (Stream) objectContext;
            int bytesRead;
            var buffToReadLength = new byte[ManagedBytesLength];
            var counter = 0;

            while ((bytesRead = sourceStream.Read(buffToReadLength, 0, ManagedBytesLength)) != 0)
            {
                if (bytesRead != ManagedBytesLength)
                    throw new Exception(FileDecompressError);

                var bytesLength = buffToReadLength.ToInt32();
                var bytesForDecompression = new byte[bytesLength];

                if (sourceStream.Read(bytesForDecompression, 0, bytesLength) != bytesLength)
                    throw new Exception(FileDecompressError);

                var arrayIndex = counter;
                var thread = new Thread(() => DecompressOneArray(bytesForDecompression, arrayIndex))
                    {
                       IsBackground = true
                    };
                thread.Start();
                EnqueueThread(thread);
                _eventSignal.WaitOne();
                counter++;
            }

            _hasFileRead = true;
        }

        private void DecompressOneArray(byte[] bytesForDecompression, int index)
        {
            _eventSignal.Set();
            using (var gZipStream = new GZipStream(new MemoryStream(bytesForDecompression), CompressionMode.Decompress))
            {
                var decompressedBytes = new byte[ChunkSize];
                var stream = new MemoryStream();
                int bytesCount;
                while ((bytesCount = gZipStream.Read(decompressedBytes, 0, ChunkSize)) != 0)
                    stream.Write(decompressedBytes, 0, bytesCount);

                _resultStreams.SafeAdd(index, stream, _dictionaryLocker);
            }
        }

        private Thread DequeueThread()
        {
            try
            {
                Monitor.Enter(_threadLocker);
                return !_threadsQueue.Any() ? null : _threadsQueue.Dequeue();
            }
            finally
            {
                Monitor.Exit(_threadLocker);
            }
        }

        private void EnqueueThread(Thread data)
        {
            try
            {
                Monitor.Enter(_threadLocker);
                _threadsQueue.Enqueue(data);
            }
            finally
            {
                Monitor.Exit(_threadLocker);
            }
        }

        private void ProcessFile(Stream targetStream, Stream sourceStream, CompressionMode? compressionMode)
        {
            Thread readingThread, writingThread;
            if (compressionMode != CompressionMode.Decompress)
            {
                readingThread = new Thread(CompressFileThread) {IsBackground = true};
                writingThread = new Thread(SaveCompressedFile) {IsBackground = true};
            }
            else
            {
                readingThread = new Thread(DecompressFileThread) {IsBackground = true};
                writingThread = new Thread(SaveDecompressedFileThread) {IsBackground = true};
            }

            readingThread.Start(sourceStream);
            writingThread.Start(targetStream);

            readingThread.Join();
            writingThread.Join();
        }

        private void SaveCompressedFile(object threadContext)
        {
            var targetStream = (Stream) threadContext;
            var counter = 0;
            while (true)
            {
                var thread = DequeueThread();
                if (thread == null)
                {
                    if (!_hasFileRead)
                        continue;
                    break;
                }

                thread.Join();
                var managedBytes = ((int) _resultStreams[counter].Length).ToByteArray();
                targetStream.Write(managedBytes, 0, managedBytes.Length);
                WriteChunkToStream(counter, targetStream);
                _resultStreams.SafeDelete(counter, _dictionaryLocker);
                counter++;
            }
        }

        private void SaveDecompressedFileThread(object threadContext)
        {
            var targetStream = (Stream) threadContext;
            var counter = 0;
            while (true)
            {
                var thread = DequeueThread();
                if (thread == null)
                {
                    if (!_hasFileRead)
                        continue;
                    break;
                }

                thread.Join();
                _resultStreams[counter].Seek(0, 0);
                WriteChunkToStream(counter, targetStream);
                _resultStreams.SafeDelete(counter, _dictionaryLocker);
                counter++;
            }
        }

        private void WriteChunkToStream(int counter, Stream targetStream)
        {
            var compressedBytes = ((MemoryStream) _resultStreams[counter]).ToArray();
            targetStream.Write(compressedBytes, 0, compressedBytes.Length);
        }
    }
}