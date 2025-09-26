using System;
using System.Collections.Generic;

namespace ProblemMonitoring
{
    public class Config
    {
        public int TimerIntervalSeconds { get; set; } = 60;  // Inutilizado, pois agora é executado por Agendador de Tarefas
        public int EmailIntervalMinutes { get; set; } = 30;  // intervalo global entre emails
        public List<Monitor> Monitors { get; set; }
        public EmailConfig EmailConfig { get; set; }
    }

    public class Monitor
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Query { get; set; }
        public int RowLimit { get; set; }
        public bool WindowsNotification { get; set; }
        public int CommandTimeoutSeconds { get; set; } = 30;
        public MonitorEmail Email { get; set; }
        public int ExecutionIntervalSeconds { get; set; } = 600; 
        public DateTime? LastExecution { get; set; } = null;
    }

    public class MonitorEmail
    {
        public bool Enabled { get; set; }
        public List<string> Recipients { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime? LastSent { get; set; }
        public int EmailIntervalMinutes { get; set; }
    }

    public class EmailConfig
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
    }
}
