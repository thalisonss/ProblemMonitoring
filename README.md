# 🚨 Problem Monitoring

Um utilitário simples para **monitorar queries SQL** e disparar alertas por e-mail quando um problema for identificado.  
Ideal para cenários de **monitoramento de sistemas em produção**, como validação de notas fiscais, cadastros, inconsistências em dados etc.

---

## 📦 Sobre o projeto

Este projeto foi desenvolvido para rodar em **.NET 6/7**, realizando consultas em banco de dados SQL Server.  
Seu objetivo é:

- Executar queries configuradas em um arquivo `config.json`
- Validar se os resultados atendem às condições definidas
- Disparar alertas por e-mail de forma automática
- Permitir execução manual ou agendada (Agendador de Tarefas do Windows)
- Operar em **modo silencioso** (`/silent`) sem abrir console

## 📖 Como funciona

1. O sistema lê o arquivo `config.json`
2. Para cada monitoramento configurado:
   - Executa a query SQL definida
   - Conta o número de linhas retornadas
   - Compara o resultado com o limite (`RowLimit`)
   - Caso a condição seja atendida, dispara um e-mail de alerta
3. O envio de e-mail respeita um intervalo mínimo (`EmailIntervalMinutes`) para evitar disparos em excesso.
4. O título e corpo do e-mail podem ser personalizados com placeholders dinâmicos.

---

## ⚙️ Exemplo de configuração (`config.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_SERVIDOR;Database=SUA_BASE;User Id=usuario;Password=senha;"
  },
  "Email": {
    "From": "monitor@empresa.com",
    "To": "destinatario@empresa.com",
    "SmtpServer": "smtp.empresa.com",
    "Port": 587,
    "Username": "usuario",
    "Password": "senha"
  },
  "Monitors": [
    {
      "Name": "White Martins - vBCIRRF zerado",
      "Query": "SELECT * FROM NotasFiscais WHERE vBCIRRF = 0",
      "RowLimit": 0,
      "EmailIntervalMinutes": 60,
      "Email": {
        "Subject": "[MONITORAMENTO] - Teste {dateNow}",
        "Body": "Estamos com {result} NFs com a tag vBCIRRF zerado, conforme configurado alarmar quando maior ou igual a {rowLimit}."
      }
    }
  ]
}
```
## 🧩 Placeholders dinâmicos

Você pode personalizar o **assunto** e o **corpo do e-mail** usando variáveis que serão substituídas em tempo de execução:

- `{result}` → quantidade de registros retornados pela query  
- `{rowLimit}` → limite configurado para disparo  
- `{dateNow}` → data atual (formato `dd.MM.yyyy`)  
- `{dateNowAndHour}` → data e hora atuais (formato `dd.MM.yyyy HH:mm`)  

Exemplo no `Subject`:
"[MONITORAMENTO] - Teste {dateNow}"


Esse assunto, ao ser enviado, ficaria assim:
[MONITORAMENTO] - Teste 25.09.2025

---

## ▶️ Execução

O projeto é um **Console Application** em .NET.  
Para rodar:

```sh
dotnet run --project ProblemMonitoring
```
Ou, após compilar:

ProblemMonitoring.exe

📌 O log de execução é salvo em log.log no diretório da aplicação.
## 📝 Log de execução

Durante a execução, o programa gera um arquivo `log.log` no diretório da aplicação.  
Exemplo de saída:

2025-09-22 11:59:18 - Running monitor: Teste

2025-09-22 11:59:19 - Monitor 'Teste' result: 5

2025-09-22 11:59:19 - Monitor 'Teste' exceeded row limit (0).

2025-09-22 11:59:20 - [INFO] E-mail enviado para usuario@empresa.com

2025-09-22 11:59:20 - Console Worker finished.

Isso ajuda a verificar **quando e por que um alerta foi disparado**.

---

## 📂 Estrutura do projeto
📦 ProblemMonitoring
┣ 📜 Program.cs # Código principal (leitura do JSON, execução dos monitores, envio de e-mail)
┣ 📜 Config.cs # Classes de configuração (Config, Monitor, Email, etc.)
┣ 📜 Logger.cs # Classe para registrar logs em arquivo
┣ 📜 ModeloDeConfig.json # Configuração de monitores e e-mail
┣ 📜 log.log # Arquivo de log gerado em runtime
┗ 📜 README.md # Documentação do projeto


---

## 🚀 Próximos passos

- [ ] Melhorar tratamento de erros com retry automático para falhas de conexão  
- [ ] Adicionar suporte a outros bancos além de **SQL Server**  
- [ ] Implementar autenticação moderna para envio de e-mails (OAuth2)  
- [ ] Criar versão com interface web para configurar monitores de forma amigável  

---
