# Agendador Férias AD

Gerenciador de Desativação Temporária de Usuários do Active Directory (AD).

Este aplicativo foi desenvolvido em **VB.NET (WinForms) e .NET 8** com o objetivo de facilitar o processo de desativação temporária (férias, licenças, afastamentos) de contas de usuários no Active Directory, realizando o agendamento automático para reativação das contas na data e hora especificadas.

## Funcionalidades

- **Desativação Imediata**: Desabilita o usuário no Active Directory no exato momento da execução.
- **Agendamento Automático**: Cria uma Tarefa Agendada no Windows (Task Scheduler) configurada para rodar em uma data e hora futuras. Esta tarefa roda um script PowerShell oculto que habilita o usuário no AD na data programada.
- **Histórico e Status Visual**: Mantém um histórico detalhado das operações pendentes e concluídas, gravando tudo em um banco de dados local (JSON). O status é exibido com cores (Agendado, Reativado, Cancelado).
- **Cancelamento de Agendamentos**: Permite cancelar tarefas de reativação pendentes. Ao cancelar, a tarefa do Windows correspondente é limpa automaticamente.
- **Execução Segura (Bypass de Heurística Antivírus)**: Os comandos PowerShell interagem nativamente com o console (via StandardInput), prevenindo que ferramentas como o Windows Defender acusem falsos positivos ou excluam a aplicação.

## Requisitos do Sistema

- **Sistema Operacional**: Windows 10, Windows 11, ou Windows Server 2016+.
- **Privilégios**: É necessário privilégio de Administrador para que a aplicação possa alterar o estado do AD e registrar tarefas no Windows Task Scheduler (o `.exe` já solicita elevação automaticamente via UAC).
- **Dependências de Infraestrutura**: O computador onde a aplicação está rodando precisa ter os módulos do Active Directory instalados (**RSAT** - Remote Server Administration Tools).
- **Runtime .NET**: Pode ser compilado como *Framework-Dependent* (requer .NET 8 Desktop Runtime instalado) ou *Self-Contained* (não requer dependências).

## Como Instalar e Rodar

1. Baixe o código fonte.
2. Certifique-se de possuir o SDK do .NET 8 instalado.
3. Abra a pasta do projeto no terminal e execute o comando para gerar o `.exe` de arquivo único e leve:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
   ```
4. O arquivo `ADUserManager.exe` gerado na pasta `publish` poderá ser colocado no Desktop ou em qualquer outra pasta e estará pronto para uso.

## Detalhes Técnicos

A comunicação com o Active Directory é feita inteiramente via comandos shell do PowerShell internamente:
- `Disable-ADAccount`
- `Enable-ADAccount`
- `Register-ScheduledTask`
- `Unregister-ScheduledTask`

Todas as tarefas agendadas são criadas no diretório `\ADUserManager\` dentro do Task Scheduler do Windows, para manter o sistema limpo e organizado.

## Licença
Este software é fornecido conforme as necessidades de administração local. Sinta-se à vontade para fazer forks e pull requests.
