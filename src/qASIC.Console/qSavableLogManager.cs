using qASIC.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using qASIC.Logging;

namespace qASIC.Console.Logging
{
    public class qSavableLogManager : qLogManager, IEnumerable<qLog>
    {
        public qSavableLogManager() : this(new List<qLog>()) { }
        public qSavableLogManager(IEnumerable<qLog> logs) : base()
        {
            Logs = new List<qLog>(logs);
        }

        #region Logging
        public List<qLog> Logs { get; private set; }

        public event Action<qLog> OnUpdateLog;

        public void Clear()
        {
            if (Closed)
                return;

            Logs?.Clear();
        }

        protected void InvokeOnUpdateLog(qLog log) =>
            OnUpdateLog?.Invoke(log);

        public override void Log(qLog log)
        {
            if (Closed)
                return;

            if (Logs.Contains(log))
            {
                InvokeOnUpdateLog(log);
                return;
            }

            ApplyLogModifiers(log);
            Logs.Add(log);
            InvokeOnLog(log);
            FileWrite(log);
        }
        #endregion

        #region Registering
        public override qLogManager RegisterManager(qLogManager other)
        {
            if (other is qSavableLogManager gameOther && other != this && !RegisteredManagers.Contains(other))
                gameOther.OnUpdateLog += Log;

            return base.RegisterManager(other);
        }

        public override qLogManager UnregisterManager(qLogManager other)
        {
            if (other is qSavableLogManager gameOther && other != this && RegisteredManagers.Contains(other))
                gameOther.OnUpdateLog -= Log;

            return base.UnregisterManager(other);
        }
        #endregion

        #region Writing To Disk
        /// <summary>Path of the log file.</summary>
        public string RawFilePath { get; set; }

        public string FinalFilePath =>
            (PathConverter ?? ConfigPathConverter.CreateStandardConverter()).ConvertToFinal(RawFilePath);

        public ConfigPathConverter PathConverter { get; set; }

        public string FileLogFormat { get; set; } = "[%TIME:HH:mm:ss.fff%] [%TYPE%] %MESSAGE%";

        private Task _fileWriteTask = null;
        private Queue<qLog> _fileWriteQueue = new Queue<qLog>();

        /// <summary>Changes the value of <see cref="RawFilePath"/>.</summary>
        /// <param name="newPath">New file path.</param>
        /// <returns>Returns itself.</returns>
        public qSavableLogManager FileChangePath(string newPath)
        {
            RawFilePath = newPath;
            return this;
        }

        /// <summary>Moves a previous version of the log file to a new location.</summary>
        /// <param name="newOldPath">Path to move the old log file to.</param>
        /// <returns>Returns itself.</returns>
        public qSavableLogManager FileMoveOld(string newOldPath)
        {
            if (File.Exists(newOldPath))
                File.Delete(newOldPath);

            if (File.Exists(FinalFilePath))
                File.Move(FinalFilePath, newOldPath);

            return this;
        }

        /// <summary>Changes the name of a previous version of the log file.</summary>
        /// <param name="newName">New name for the old log file.</param>
        /// <returns>Returns itself.</returns>
        public qSavableLogManager FileRenameOld(string newName) =>
            FileMoveOld($"{Path.GetDirectoryName(FinalFilePath)}/{newName}");

        /// <summary>Clears the log file.</summary>
        /// <returns>Returns itself.</returns>
        public qSavableLogManager FileClear()
        {
            FileWrite(null);
            return this;
        }

        /// <summary>Writes all logs in <see cref="Logs"/> to the file.</summary>
        /// <returns>Return itself.</returns>
        public qSavableLogManager FileWriteExisting()
        {
            foreach (var item in Logs)
                _fileWriteQueue.Enqueue(item);

            FileEnsureWritingTask();
            return this;
        }

        private void FileWrite(qLog log)
        {
            _fileWriteQueue.Enqueue(log);
            FileEnsureWritingTask();
        }

        private void FileEnsureWritingTask()
        {
            if (_fileWriteQueue.Count == 0)
                return;

            if (_fileWriteTask != null && !_fileWriteTask.IsCompleted)
                return;

            _fileWriteTask = FileWriteTask();
            Task.Run(() => _fileWriteTask);
        }

        private async Task FileWriteTask()
        {
            if (RawFilePath == null)
            {
                _fileWriteQueue.Clear();
                return;
            }

            var dir = Path.GetDirectoryName(FinalFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var writer = new StreamWriter(FinalFilePath, true))
            {
                while (_fileWriteQueue.Count > 0)
                {
                    var log = _fileWriteQueue.Dequeue();

                    if (log == null)
                    {
                        await writer.BaseStream.WriteAsync(new byte[0]);
                        continue;
                    }

                    var txt = log.ToString(FileLogFormat);
                    await writer.WriteLineAsync(txt);
                }
            }
        }
        #endregion

        public IEnumerator<qLog> GetEnumerator() =>
            Logs.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}