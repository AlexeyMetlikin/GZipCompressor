using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Compressor.Extensions;
using Compressor.Models;

namespace Compressor
{
    public class GZipCompressor
    {
        ////TODO: 
        /// 1. Refactor and replace code to some clases
        /// 2. Create custom Exceptions
        /// 3. Make one process of exceptions

        private const int ChunkSize = 1024 * 1024;
        private const string DirectoryCreationError =
            "Ошибка при попытке создания директории выходного файла. Проверьте имя директории и права на возможность ее создания";

        private const string FileDecompressError =
            "Ошибка разархивирования файла: выбран некорректный исходный файл либо файл поврежден";

        private const int ManagedBytesLength = sizeof(int);
        private readonly ConcurrentDictionary<int, Stream> _resultStreams = new ConcurrentDictionary<int, Stream>();
        private readonly int ProcessorCount;
        private bool _hasFileRead;

        public GZipCompressor()
        {
            ProcessorCount = Environment.ProcessorCount;
        }

        public int ProcessFileAccordingToCompressionMode(ParamsModel paramsModel)
        {
            _hasFileRead = false;
            CreateOutputFileDirectoryIfNotExists(paramsModel);
            ProcessFile(paramsModel);

            return 0;
        }

        private static void CreateOutputFileDirectoryIfNotExists(ParamsModel paramsModel)
        {
            var directoryName = Path.GetDirectoryName(paramsModel.OutputFileName);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName ?? throw new ArgumentNullException(DirectoryCreationError));
        }

        private void CompressFileThread(string sourceFileName)
        {
            var threads = new Thread[ProcessorCount];
            for (var i = 0; i < ProcessorCount; i++)
            {
                var taskCounter = i;
                var thread = new Thread(() => CompressOneArray(sourceFileName, taskCounter))
                {
                    IsBackground = true
                };
                threads[i] = thread;
                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            _hasFileRead = true;
        }

        private void CompressOneArray(string sourceFileName, int taskCounter)
        {
            using (var sourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int bytesRead;
                var bufferRead = new byte[ChunkSize];
                sourceStream.Seek(taskCounter * ChunkSize, SeekOrigin.Current);
                while ((bytesRead = sourceStream.Read(bufferRead, 0, ChunkSize)) != 0)
                {
                    var stream = new MemoryStream();
                    using (var gzStream = new GZipStream(stream, CompressionMode.Compress, true))
                    {
                        gzStream.Write(bufferRead, 0, bytesRead);
                    }

                    _resultStreams.TryAdd(taskCounter, stream);

                    taskCounter += ProcessorCount;
                    bufferRead = new byte[ChunkSize];
                    sourceStream.Seek((ProcessorCount - 1) * ChunkSize, SeekOrigin.Current);
                }                
            }
        }

        private void DecompressFileThread(string sourceFileName)
        {
            var threads = new Thread[ProcessorCount];
            for (var i = 0; i < ProcessorCount; i++)
            {
                var taskCounter = i;
                var thread = new Thread(() => DecompressOneArrayNew(sourceFileName, taskCounter))
                {
                    IsBackground = true
                };
                thread.Start();
                threads[i] = thread;
            }

            foreach (var thread in threads)
                thread.Join();

            _hasFileRead = true;
        }

        private void DecompressOneArrayNew(string sourceFileName, int taskCounter)
        {
            using (var sourceStream = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SeekToNext(sourceStream, taskCounter);
                while (true)
                {
                    var bytesLength = GetNextChunkLength(sourceStream);
                    if (bytesLength == 0)
                        return;

                    var bytesForDecompression = new byte[bytesLength];
                    if (sourceStream.Read(bytesForDecompression, 0, bytesLength) != bytesLength)
                        throw new Exception(FileDecompressError);

                    Dec(bytesForDecompression, taskCounter);
                    taskCounter += ProcessorCount;
                    SeekToNext(sourceStream, ProcessorCount - 1);
                }
            }
        }

        private void Dec(byte[] bytesForDecompression, int taskCounter)
        {
            using (var gZipStream = new GZipStream(new MemoryStream(bytesForDecompression), CompressionMode.Decompress))
            {
                var decompressedBytes = new byte[ChunkSize];
                var stream = new MemoryStream();
                int bytesCount;
                while ((bytesCount = gZipStream.Read(decompressedBytes, 0, ChunkSize)) != 0)
                    stream.Write(decompressedBytes, 0, bytesCount);

                _resultStreams.TryAdd(taskCounter, stream);
            }
        }

        private int GetNextChunkLength(FileStream sourceStream)
        {
            var buffToReadLength = new byte[ManagedBytesLength];
            int bytesRead = sourceStream.Read(buffToReadLength, 0, ManagedBytesLength);
            if (bytesRead != ManagedBytesLength && bytesRead > 0)
                throw new Exception(FileDecompressError);

            return buffToReadLength.ToInt32();
        }

        private void SeekToNext(FileStream sourceStream, int taskCounter)
        {
            while (taskCounter > 0)
            {
                var buffLenght = GetNextChunkLength(sourceStream);
                if (buffLenght == 0)
                    return;

                sourceStream.Seek(buffLenght, SeekOrigin.Current);
                taskCounter--;
            }
        }

        private void ProcessFile(ParamsModel paramsModel)
        {
            Action readingThread, writingThread;
            Exception readException = null, writeException = null;
            if (paramsModel.CompressionMode != CompressionMode.Decompress)
            {
                readingThread = () => CompressFileThread(paramsModel.InputFileName);
                writingThread = () => SaveCompressedFile(paramsModel.OutputFileName);
            }
            else
            {
                readingThread = () => DecompressFileThread(paramsModel.InputFileName);
                writingThread = () => SaveDecompressedFileThread(paramsModel.OutputFileName);
            }

            var r = new Thread(() => ThreadMethodWithOutParameter(readingThread, out readException)) { IsBackground = true };
            var w = new Thread(() => ThreadMethodWithOutParameter(writingThread, out writeException)) { IsBackground = true };

            r.Start();
            w.Start();

            r.Join();
            if (readException != null)
            {
                w.Abort();
                throw readException;
            }

            w.Join();
            if (writeException != null)
            {
                r.Abort();
                throw readException;
            }
        }

        private static void ThreadMethodWithOutParameter(Action action, out Exception resultException)
        {
            resultException = null;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                resultException = e;
            }
        }

        private void SaveCompressedFile(string outputFileName)
        {
            using (var targetStream = new FileStream(outputFileName, FileMode.Create))
            {
                var counter = 0;
                while (true)
                {
                    if (_resultStreams.Keys?.Contains(counter) != true)
                    {
                        if (_hasFileRead)
                            throw new Exception(FileDecompressError); ;
                        continue;
                    }

                    var managedBytes = ((int)_resultStreams[counter].Length).ToByteArray();
                    targetStream.Write(managedBytes, 0, managedBytes.Length);
                    WriteChunkToStream(counter, targetStream);
                    counter++;
                }
            }
        }

        private void SaveDecompressedFileThread(string outputFileName)
        {
            using (var targetStream = new FileStream(outputFileName, FileMode.Create))
            {
                var counter = 0;
                while (true)
                {
                    if (_resultStreams.Keys?.Contains(counter) != true)
                    {
                        if (_hasFileRead)
                            break;
                        continue;
                    }

                    WriteChunkToStream(counter, targetStream);
                    counter++;
                }
            }
        }

        private void WriteChunkToStream(int counter, Stream targetStream)
        {
            _resultStreams.TryRemove(counter, out Stream stream);
            stream.Seek(0, SeekOrigin.Begin);
            var compressedBytes = ((MemoryStream)stream).ToArray();
            targetStream.Write(compressedBytes, 0, compressedBytes.Length);
        }
    }
}