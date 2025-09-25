using System;
using System.IO;

namespace ProblemMonitoring
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly long _maxFileSizeBytes;
        private readonly object _lock = new object();

        public Logger(string logFilePath, long maxFileSizeBytes = 5 * 1024 * 1024) // 5 MB padrão
        {
            _logFilePath = logFilePath;
            _maxFileSizeBytes = maxFileSizeBytes;
        }

        public void Write(string message)
        {
            lock (_lock)
            {
                try
                {
                    RotateIfNeeded();

                    string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                    File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
                }
                catch
                {
                    // Não deixar o app quebrar por erro no log
                }
            }
        }

        private void RotateIfNeeded()
        {
            if (!File.Exists(_logFilePath)) return;

            FileInfo fi = new FileInfo(_logFilePath);
            if (fi.Length < _maxFileSizeBytes) return;

            // Rotacionamento de arquivos de log. Exemplo: log.log para log.log1 e log.log1 para log.log2....
            string dir = Path.GetDirectoryName(_logFilePath);
            string fileName = Path.GetFileNameWithoutExtension(_logFilePath);
            string ext = Path.GetExtension(_logFilePath);

            // Nome de sequencia de arquivos
            for (int i = 5; i >= 1; i--)
            {
                string oldFile = Path.Combine(dir, $"{fileName}{ext}{i}");
                string newFile = Path.Combine(dir, $"{fileName}{ext}{i + 1}");

                if (File.Exists(oldFile))
                {
                    if (File.Exists(newFile)) File.Delete(newFile);
                    File.Move(oldFile, newFile);
                }
            }

            //Exemplo: log.log para log.log1
            string rotated = Path.Combine(dir, $"{fileName}{ext}1");
            if (File.Exists(rotated)) File.Delete(rotated);
            File.Move(_logFilePath, rotated);
        }
    }
}
