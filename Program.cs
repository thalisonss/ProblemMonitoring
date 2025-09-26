using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Mail;

namespace ProblemMonitoring
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger(Path.Combine(AppContext.BaseDirectory, "log.log"));
            logger.Write("Console Worker started.");

            var path = Path.Combine(AppContext.BaseDirectory, "config.json");
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<Config>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (config?.Monitors == null) return;

            foreach (var monitor in config.Monitors)
            {
                try
                {
                    DateTime lastExec = monitor.LastExecution ?? DateTime.MinValue;
                    if ((DateTime.Now - lastExec).TotalSeconds < monitor.ExecutionIntervalSeconds)
                    {
                        logger.Write($"Monitor '{monitor.Name}' skipped: interval not passed.");
                        continue;
                    }

                    logger.Write($"Running monitor: {monitor.Name}");
                    int result = 0;

                    using (var conn = new SqlConnection(monitor.ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand(monitor.Query, conn))
                        {
                            cmd.CommandTimeout = monitor.CommandTimeoutSeconds;
                            using var reader = cmd.ExecuteReader();
                            int rowCount = 0;
                            while (reader.Read())
                                rowCount++;
                            result = rowCount;
                        }
                    }

                    logger.Write($"Monitor '{monitor.Name}' result: {result}");

                    monitor.LastExecution = DateTime.Now;

                    if (result >= monitor.RowLimit && monitor.Email.Enabled)
                    {
                        DateTime lastSent = monitor.Email.LastSent ?? DateTime.MinValue;
                        int monitorInterval = monitor.Email.EmailIntervalMinutes > 0
                            ? monitor.Email.EmailIntervalMinutes
                            : 60;

                        TimeSpan interval = TimeSpan.FromMinutes(monitorInterval);

                        if (DateTime.Now - lastSent >= interval)
                        {
                            SendEmail(config, monitor, result, logger);
                            monitor.Email.LastSent = DateTime.Now;
                            logger.Write($"Email sent for monitor '{monitor.Name}'.");
                        }
                        else
                        {
                            logger.Write($"Email for '{monitor.Name}' skipped, interval not passed.");
                        }
                    }

                    SaveConfig(path, config); 

                }
                catch (Exception ex)
                {
                    logger.Write($"Error in monitor '{monitor.Name}': {ex.Message}");
                }
            }

            logger.Write("Console Worker finished.");
        }

        private static string GetQueryResultAsHtmlTable(string connectionString, string query, int timeout)
        {
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(query, conn) { CommandTimeout = timeout };
            using var adapter = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count == 0)
                return "<p><b>Nenhum registro encontrado.</b></p>";

            var sb = new StringBuilder();
            sb.Append("<table style='border-collapse:collapse;font-family:Arial;font-size:12px;width:100%;'>");

            // Cabeçalho
            sb.Append("<tr style='background-color:#0078D7;color:#ffffff;text-align:left;'>");
            foreach (DataColumn col in dt.Columns)
                sb.AppendFormat("<th style='border:1px solid #dddddd;padding:8px;'>{0}</th>", col.ColumnName);
            sb.Append("</tr>");

            // Linhas alternadas
            bool alternate = false;
            foreach (DataRow row in dt.Rows)
            {
                string bgColor = alternate ? "#f9f9f9" : "#ffffff";
                sb.Append($"<tr style='background-color:{bgColor};'>");
                foreach (var item in row.ItemArray)
                    sb.AppendFormat("<td style='border:1px solid #dddddd;padding:8px;'>{0}</td>", item?.ToString() ?? "");
                sb.Append("</tr>");
                alternate = !alternate;
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private static void SendEmail(Config config, Monitor monitor, int result, Logger logger)
        {
            var emailConfig = config.EmailConfig;

            string subject = monitor.Email.Subject;
            subject = subject.Replace("{result}", result.ToString());
            subject = subject.Replace("{rowLimit}", monitor.RowLimit.ToString());
            subject = subject.Replace("{dateNow}", DateTime.Now.ToString("dd.MM.yyyy"));
            subject = subject.Replace("{dateNowAndHour}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));

            string htmlTable = GetQueryResultAsHtmlTable(
                monitor.ConnectionString,
                monitor.Query,
                monitor.CommandTimeoutSeconds
            );

            using var smtp = new SmtpClient(emailConfig.Server, emailConfig.Port)
            {
                Credentials = new NetworkCredential(emailConfig.User, emailConfig.Password),
                EnableSsl = emailConfig.UseSsl
            };

            var message = new MailMessage
            {
                From = new MailAddress(emailConfig.User),
                Subject = subject,
                Body = $@"
        <p><b>Alerta:</b> {monitor.Name}</p>
        <p>Estamos com <b>{result}</b> registros conforme configurado para alarmar quando for maior ou igual a <b>{monitor.RowLimit}</b>.</p>
        <br/>
        <p><b>Dados retornados:</b></p>
        {htmlTable}
    ",
                IsBodyHtml = true
            };


            foreach (var recipient in monitor.Email.Recipients)
                message.To.Add(recipient);

            smtp.Send(message);
            logger.Write($"[INFO] E-mail enviado para {string.Join(", ", monitor.Email.Recipients)}");
        }

        private static void SaveConfig(string path, Config config)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(config, options));
        }
    }
}
