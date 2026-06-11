Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Diagnostics
Imports System.IO
Imports System.Text.Json
Imports System.Security.Principal

Namespace ADUserManager

    ''' <summary>
    ''' Modelo de dados para rastrear tarefas de reativação agendadas.
    ''' </summary>
    Public Class TaskEntry
        Public Property Username As String = ""
        Public Property DisabledDate As String = ""
        Public Property ReactivationDate As String = ""
        Public Property ScheduledTaskName As String = ""
        Public Property Status As String = "Agendado"
    End Class

    ''' <summary>
    ''' Formulário principal do AD User Manager.
    ''' Permite desabilitar usuários do AD e agendar a reativação automática.
    ''' </summary>
    Public Class MainForm
        Inherits Form

#Region "Campos"

        ' ═══════════════════════════════════════════════════════
        '  Paleta de Cores (Catppuccin Mocha)
        ' ═══════════════════════════════════════════════════════
        Private ReadOnly clrBase As Color = Color.FromArgb(30, 30, 46)
        Private ReadOnly clrMantle As Color = Color.FromArgb(24, 24, 37)
        Private ReadOnly clrCrust As Color = Color.FromArgb(17, 17, 27)
        Private ReadOnly clrSurface0 As Color = Color.FromArgb(49, 50, 68)
        Private ReadOnly clrSurface1 As Color = Color.FromArgb(69, 71, 90)
        Private ReadOnly clrSurface2 As Color = Color.FromArgb(88, 91, 112)
        Private ReadOnly clrOverlay0 As Color = Color.FromArgb(108, 112, 134)
        Private ReadOnly clrText As Color = Color.FromArgb(205, 214, 244)
        Private ReadOnly clrSubtext As Color = Color.FromArgb(166, 173, 200)
        Private ReadOnly clrBlue As Color = Color.FromArgb(137, 180, 250)
        Private ReadOnly clrRed As Color = Color.FromArgb(243, 139, 168)
        Private ReadOnly clrGreen As Color = Color.FromArgb(166, 227, 161)
        Private ReadOnly clrYellow As Color = Color.FromArgb(249, 226, 175)
        Private ReadOnly clrMauve As Color = Color.FromArgb(203, 166, 247)
        Private ReadOnly clrPeach As Color = Color.FromArgb(250, 179, 135)

        ' ═══════════════════════════════════════════════════════
        '  Controles da Interface
        ' ═══════════════════════════════════════════════════════
        Private txtUsername As TextBox
        Private dtpDate As DateTimePicker
        Private dtpTime As DateTimePicker
        Private btnDisable As Button
        Private btnReactivate As Button
        Private btnRemove As Button
        Private dgvTasks As DataGridView
        Private rtbLog As RichTextBox
        Private lblStatus As Label

        ' ═══════════════════════════════════════════════════════
        '  Dados
        ' ═══════════════════════════════════════════════════════
        Private appDataPath As String
        Private historyFile As String
        Private scriptsDir As String
        Private history As List(Of TaskEntry)

#End Region

#Region "Construtor"

        Public Sub New()
            Try
                ' Configurar caminhos de dados
                appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ADUserManager")
                Directory.CreateDirectory(appDataPath)
                historyFile = Path.Combine(appDataPath, "history.json")
                scriptsDir = Path.Combine(appDataPath, "scripts")
                Directory.CreateDirectory(scriptsDir)

                ' Carregar histórico
                history = New List(Of TaskEntry)()
                LoadHistory()

                ' Construir interface
                InitializeUI()
                RefreshGrid()

                ' Mensagem de boas-vindas
                AddLog("AD User Manager v1.0 iniciado.", clrBlue)
                AddLog("Dados armazenados em: " & appDataPath, clrSubtext)

                ' Verificações são feitas após o form ser exibido
                AddHandler Me.Shown, AddressOf MainForm_Shown
            Catch ex As Exception
                MessageBox.Show(
                    "Erro ao inicializar:" & vbCrLf & vbCrLf & ex.ToString(),
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub MainForm_Shown(sender As Object, e As EventArgs)
            ' Verificar privilégios (não bloqueia a abertura)
            CheckAdminPrivileges()

            ' Verificar módulo AD em background (não bloqueia a interface)
            CheckADModule()
        End Sub

#End Region

#Region "Inicialização da Interface"

        Private Sub InitializeUI()
            ' ─── Configurações do Formulário ───
            Me.Text = "AD User Manager"
            Me.ClientSize = New Size(940, 760)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = clrBase
            Me.ForeColor = clrText
            Me.Font = New Font("Segoe UI", 10)
            Me.FormBorderStyle = FormBorderStyle.FixedSingle
            Me.MaximizeBox = False
            Me.DoubleBuffered = True

            ' ═══════════════════════════════════════════════════════
            '  HEADER
            ' ═══════════════════════════════════════════════════════
            Dim pnlHeader As New Panel()
            pnlHeader.Bounds = New Rectangle(0, 0, 940, 85)
            pnlHeader.BackColor = clrMantle
            Me.Controls.Add(pnlHeader)

            Dim lblIcon As New Label()
            lblIcon.Text = Char.ConvertFromUtf32(&H1F512)
            lblIcon.Font = New Font("Segoe UI Emoji", 26)
            lblIcon.ForeColor = clrBlue
            lblIcon.AutoSize = True
            lblIcon.Location = New Point(22, 14)
            pnlHeader.Controls.Add(lblIcon)

            Dim lblTitle As New Label()
            lblTitle.Text = "AD User Manager"
            lblTitle.Font = New Font("Segoe UI", 20, FontStyle.Bold)
            lblTitle.ForeColor = clrBlue
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(72, 8)
            pnlHeader.Controls.Add(lblTitle)

            Dim lblSubtitle As New Label()
            lblSubtitle.Text = "Gerenciador de Desativação Temporária de Usuários do Active Directory"
            lblSubtitle.Font = New Font("Segoe UI", 9.5F)
            lblSubtitle.ForeColor = clrSubtext
            lblSubtitle.AutoSize = True
            lblSubtitle.Location = New Point(74, 48)
            pnlHeader.Controls.Add(lblSubtitle)

            ' Linha decorativa sob o header
            Dim pnlAccentLine As New Panel()
            pnlAccentLine.Bounds = New Rectangle(0, 82, 940, 3)
            pnlAccentLine.BackColor = clrBlue
            pnlHeader.Controls.Add(pnlAccentLine)

            ' Botão Sobre
            Dim btnAbout As New Button()
            btnAbout.Text = "Sobre"
            btnAbout.Size = New Size(80, 30)
            btnAbout.Location = New Point(830, 25)
            btnAbout.FlatStyle = FlatStyle.Flat
            btnAbout.BackColor = clrSurface0
            btnAbout.ForeColor = clrText
            btnAbout.FlatAppearance.BorderColor = clrOverlay0
            btnAbout.Cursor = Cursors.Hand
            AddHandler btnAbout.Click, Sub()
                                           Process.Start(New ProcessStartInfo("https://github.com/IDhexter/Agendador-ferias-ad") With {.UseShellExecute = True})
                                       End Sub
            pnlHeader.Controls.Add(btnAbout)

            ' ═══════════════════════════════════════════════════════
            '  SEÇÃO DE ENTRADA
            ' ═══════════════════════════════════════════════════════
            Dim pnlInput As New Panel()
            pnlInput.Bounds = New Rectangle(20, 100, 900, 110)
            pnlInput.BackColor = clrSurface0
            Me.Controls.Add(pnlInput)

            ' Borda esquerda decorativa
            Dim pnlInputAccent As New Panel()
            pnlInputAccent.Bounds = New Rectangle(0, 0, 4, 110)
            pnlInputAccent.BackColor = clrMauve
            pnlInput.Controls.Add(pnlInputAccent)

            ' Linha 1: Usuário
            Dim lblUser As New Label()
            lblUser.Text = "Usuário AD:"
            lblUser.Font = New Font("Segoe UI Semibold", 10)
            lblUser.ForeColor = clrSubtext
            lblUser.AutoSize = True
            lblUser.Location = New Point(20, 18)
            pnlInput.Controls.Add(lblUser)

            txtUsername = New TextBox()
            txtUsername.Size = New Size(660, 28)
            txtUsername.Location = New Point(135, 15)
            txtUsername.BackColor = clrSurface1
            txtUsername.ForeColor = clrText
            txtUsername.BorderStyle = BorderStyle.FixedSingle
            txtUsername.Font = New Font("Segoe UI", 11)
            AddHandler txtUsername.KeyDown, Sub(s, ev)
                                               If ev.KeyCode = Keys.Enter Then
                                                   BtnDisable_Click(Nothing, EventArgs.Empty)
                                                   ev.SuppressKeyPress = True
                                               End If
                                           End Sub
            pnlInput.Controls.Add(txtUsername)

            ' Dica do campo
            Dim lblUserHint As New Label()
            lblUserHint.Text = "(sAMAccountName do usuário no AD)"
            lblUserHint.Font = New Font("Segoe UI", 8.5F, FontStyle.Italic)
            lblUserHint.ForeColor = clrOverlay0
            lblUserHint.AutoSize = True
            lblUserHint.Location = New Point(800, 20)
            pnlInput.Controls.Add(lblUserHint)

            ' Linha 2: Data e Hora de Reativação
            Dim lblDate As New Label()
            lblDate.Text = "Reativação:"
            lblDate.Font = New Font("Segoe UI Semibold", 10)
            lblDate.ForeColor = clrSubtext
            lblDate.AutoSize = True
            lblDate.Location = New Point(20, 65)
            pnlInput.Controls.Add(lblDate)

            dtpDate = New DateTimePicker()
            dtpDate.Format = DateTimePickerFormat.Short
            dtpDate.Size = New Size(150, 28)
            dtpDate.Location = New Point(135, 62)
            dtpDate.MinDate = DateTime.Today
            dtpDate.Value = DateTime.Today.AddDays(1)
            dtpDate.Font = New Font("Segoe UI", 10)
            pnlInput.Controls.Add(dtpDate)

            Dim lblTime As New Label()
            lblTime.Text = "Hora:"
            lblTime.Font = New Font("Segoe UI Semibold", 10)
            lblTime.ForeColor = clrSubtext
            lblTime.AutoSize = True
            lblTime.Location = New Point(305, 65)
            pnlInput.Controls.Add(lblTime)

            dtpTime = New DateTimePicker()
            dtpTime.Format = DateTimePickerFormat.Time
            dtpTime.ShowUpDown = True
            dtpTime.Size = New Size(120, 28)
            dtpTime.Location = New Point(355, 62)
            dtpTime.Value = DateTime.Today.AddHours(8) ' Padrão: 08:00
            dtpTime.Font = New Font("Segoe UI", 10)
            pnlInput.Controls.Add(dtpTime)

            Dim lblDateHint As New Label()
            lblDateHint.Text = "(data e hora em que o usuário será REATIVADO automaticamente)"
            lblDateHint.Font = New Font("Segoe UI", 8.5F, FontStyle.Italic)
            lblDateHint.ForeColor = clrOverlay0
            lblDateHint.AutoSize = True
            lblDateHint.Location = New Point(490, 67)
            pnlInput.Controls.Add(lblDateHint)

            ' ═══════════════════════════════════════════════════════
            '  BOTÕES DE AÇÃO
            ' ═══════════════════════════════════════════════════════
            Dim yBtn As Integer = 222

            btnDisable = CreateStyledButton("  Desabilitar e Agendar Reativação", clrRed, 330)
            btnDisable.Location = New Point(20, yBtn)
            Me.Controls.Add(btnDisable)
            AddHandler btnDisable.Click, AddressOf BtnDisable_Click

            btnReactivate = CreateStyledButton("  Reativar Agora", clrGreen, 180)
            btnReactivate.Location = New Point(360, yBtn)
            Me.Controls.Add(btnReactivate)
            AddHandler btnReactivate.Click, AddressOf BtnReactivate_Click

            btnRemove = CreateStyledButton("  Remover Agendamento", clrOverlay0, 220)
            btnRemove.Location = New Point(550, yBtn)
            Me.Controls.Add(btnRemove)
            AddHandler btnRemove.Click, AddressOf BtnRemove_Click

            ' ═══════════════════════════════════════════════════════
            '  TABELA DE TAREFAS AGENDADAS
            ' ═══════════════════════════════════════════════════════
            Dim yGrid As Integer = 278

            Dim lblGrid As New Label()
            lblGrid.Text = "  Tarefas Agendadas"
            lblGrid.Font = New Font("Segoe UI Semibold", 11)
            lblGrid.ForeColor = clrMauve
            lblGrid.AutoSize = True
            lblGrid.Location = New Point(20, yGrid)
            Me.Controls.Add(lblGrid)

            yGrid += 28

            dgvTasks = New DataGridView()
            dgvTasks.Location = New Point(20, yGrid)
            dgvTasks.Size = New Size(900, 185)

            ' Estilização da tabela (tema escuro)
            dgvTasks.BackgroundColor = clrMantle
            dgvTasks.GridColor = clrSurface1
            dgvTasks.BorderStyle = BorderStyle.None
            dgvTasks.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            dgvTasks.RowHeadersVisible = False
            dgvTasks.AllowUserToAddRows = False
            dgvTasks.AllowUserToDeleteRows = False
            dgvTasks.AllowUserToResizeRows = False
            dgvTasks.ReadOnly = True
            dgvTasks.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgvTasks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            dgvTasks.EnableHeadersVisualStyles = False
            dgvTasks.ColumnHeadersHeight = 36
            dgvTasks.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            dgvTasks.RowTemplate.Height = 32

            ' Estilo padrão das células
            dgvTasks.DefaultCellStyle.BackColor = clrSurface0
            dgvTasks.DefaultCellStyle.ForeColor = clrText
            dgvTasks.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 137, 180, 250)
            dgvTasks.DefaultCellStyle.SelectionForeColor = clrText
            dgvTasks.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
            dgvTasks.DefaultCellStyle.Padding = New Padding(8, 2, 4, 2)

            ' Estilo de linhas alternadas
            dgvTasks.AlternatingRowsDefaultCellStyle.BackColor = clrMantle

            ' Estilo do cabeçalho
            dgvTasks.ColumnHeadersDefaultCellStyle.BackColor = clrCrust
            dgvTasks.ColumnHeadersDefaultCellStyle.ForeColor = clrSubtext
            dgvTasks.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgvTasks.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            dgvTasks.ColumnHeadersDefaultCellStyle.Padding = New Padding(8, 0, 0, 0)

            ' Colunas
            dgvTasks.Columns.Add("colUser", "Usuário")
            dgvTasks.Columns.Add("colDisabled", "Desabilitado em")
            dgvTasks.Columns.Add("colReactivation", "Reativação em")
            dgvTasks.Columns.Add("colTaskName", "Nome da Tarefa")
            dgvTasks.Columns.Add("colStatus", "Status")

            dgvTasks.Columns("colUser").FillWeight = 18
            dgvTasks.Columns("colDisabled").FillWeight = 18
            dgvTasks.Columns("colReactivation").FillWeight = 18
            dgvTasks.Columns("colTaskName").FillWeight = 30
            dgvTasks.Columns("colStatus").FillWeight = 16

            AddHandler dgvTasks.CellFormatting, AddressOf DgvTasks_CellFormatting

            Me.Controls.Add(dgvTasks)

            ' ═══════════════════════════════════════════════════════
            '  LOG DE ATIVIDADES
            ' ═══════════════════════════════════════════════════════
            Dim yLog As Integer = 503

            Dim lblLog As New Label()
            lblLog.Text = "  Log de Atividades"
            lblLog.Font = New Font("Segoe UI Semibold", 11)
            lblLog.ForeColor = clrMauve
            lblLog.AutoSize = True
            lblLog.Location = New Point(20, yLog)
            Me.Controls.Add(lblLog)

            yLog += 28

            rtbLog = New RichTextBox()
            rtbLog.Location = New Point(20, yLog)
            rtbLog.Size = New Size(900, 170)
            rtbLog.BackColor = clrMantle
            rtbLog.ForeColor = clrText
            rtbLog.Font = New Font("Consolas", 9.5F)
            rtbLog.BorderStyle = BorderStyle.None
            rtbLog.ReadOnly = True
            rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical
            Me.Controls.Add(rtbLog)

            ' ═══════════════════════════════════════════════════════
            '  BARRA DE STATUS
            ' ═══════════════════════════════════════════════════════
            Dim pnlStatus As New Panel()
            pnlStatus.Bounds = New Rectangle(0, 710, 940, 50)
            pnlStatus.BackColor = clrCrust
            Me.Controls.Add(pnlStatus)

            ' Linha decorativa acima da status bar
            Dim pnlStatusLine As New Panel()
            pnlStatusLine.Bounds = New Rectangle(0, 0, 940, 2)
            pnlStatusLine.BackColor = clrSurface1
            pnlStatus.Controls.Add(pnlStatusLine)

            lblStatus = New Label()
            lblStatus.Text = "● Pronto"
            lblStatus.Font = New Font("Segoe UI", 9.5F)
            lblStatus.ForeColor = clrGreen
            lblStatus.AutoSize = True
            lblStatus.Location = New Point(20, 14)
            pnlStatus.Controls.Add(lblStatus)

            Dim lblVersion As New Label()
            lblVersion.Text = "v1.0  |  PowerShell + Active Directory"
            lblVersion.Font = New Font("Segoe UI", 8.5F)
            lblVersion.ForeColor = clrOverlay0
            lblVersion.AutoSize = True
            lblVersion.Location = New Point(720, 16)
            pnlStatus.Controls.Add(lblVersion)
        End Sub

        ''' <summary>
        ''' Cria um botão estilizado com hover effect.
        ''' </summary>
        Private Function CreateStyledButton(text As String, bgColor As Color, width As Integer) As Button
            Dim btn As New Button()
            btn.Text = text
            btn.Size = New Size(width, 42)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = bgColor
            btn.ForeColor = clrCrust
            btn.Font = New Font("Segoe UI Semibold", 10)
            btn.Cursor = Cursors.Hand
            btn.TextAlign = ContentAlignment.MiddleCenter

            Dim originalColor = bgColor
            AddHandler btn.MouseEnter, Sub(s, e)
                                           DirectCast(s, Button).BackColor = ControlPaint.Light(originalColor, 0.25F)
                                       End Sub
            AddHandler btn.MouseLeave, Sub(s, e)
                                           DirectCast(s, Button).BackColor = originalColor
                                       End Sub
            Return btn
        End Function

#End Region

#Region "Verificações Iniciais"

        ''' <summary>
        ''' Verifica se o aplicativo está rodando com privilégios de administrador.
        ''' </summary>
        Private Sub CheckAdminPrivileges()
            Try
                Dim identity = WindowsIdentity.GetCurrent()
                Dim principal As New WindowsPrincipal(identity)
                If principal.IsInRole(WindowsBuiltInRole.Administrator) Then
                    AddLog("Executando com privilégios de Administrador.", clrGreen)
                Else
                    AddLog("AVISO: Não está rodando como Administrador. Algumas operações podem falhar.", clrPeach)
                End If
            Catch ex As Exception
                AddLog("Não foi possível verificar privilégios: " & ex.Message, clrYellow)
            End Try
        End Sub

        ''' <summary>
        ''' Verifica se o módulo ActiveDirectory do PowerShell está disponível.
        ''' </summary>
        Private Sub CheckADModule()
            Dim result = RunPowerShell("if (Get-Module -ListAvailable -Name ActiveDirectory) { Write-Output 'AD_MODULE_OK' } else { Write-Output 'AD_MODULE_MISSING' }")
            If result.Success Then
                If result.Output.Contains("AD_MODULE_OK") Then
                    AddLog("Módulo ActiveDirectory detectado.", clrGreen)
                ElseIf result.Output.Contains("AD_MODULE_MISSING") Then
                    AddLog("AVISO: Módulo ActiveDirectory NÃO encontrado. Instale o RSAT (Remote Server Administration Tools).", clrRed)
                End If
            Else
                AddLog("Não foi possível verificar o módulo ActiveDirectory.", clrYellow)
            End If

            AddLog("─────────────────────────────────────────────────────────────────", clrSurface1)
        End Sub

#End Region

#Region "Manipuladores de Eventos"

        ''' <summary>
        ''' Desabilita o usuário no AD e cria uma tarefa agendada para reativação.
        ''' </summary>
        Private Sub BtnDisable_Click(sender As Object, e As EventArgs)
            ' ── Validação ──
            Dim username As String = txtUsername.Text.Trim()
            If String.IsNullOrEmpty(username) Then
                MessageBox.Show(
                    "Por favor, informe o nome do usuário (sAMAccountName).",
                    "Campo Obrigatório",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtUsername.Focus()
                Return
            End If

            ' Validar caracteres no username (prevenir injeção)
            If username.Contains("'") OrElse username.Contains("""") OrElse username.Contains(";") Then
                MessageBox.Show(
                    "O nome do usuário contém caracteres inválidos.",
                    "Entrada Inválida",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim reactivationDate As DateTime = dtpDate.Value.Date.Add(dtpTime.Value.TimeOfDay)
            If reactivationDate <= DateTime.Now Then
                MessageBox.Show(
                    "A data/hora de reativação deve ser no futuro.",
                    "Data Inválida",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' ── Confirmação ──
            Dim confirmMsg As String = "Deseja executar as seguintes ações?" & vbCrLf & vbCrLf &
                "1. DESABILITAR o usuário '" & username & "' no Active Directory" & vbCrLf &
                "2. AGENDAR a reativação automática para " & reactivationDate.ToString("dd/MM/yyyy") & " às " & reactivationDate.ToString("HH:mm") & vbCrLf & vbCrLf &
                "Confirma?"

            If MessageBox.Show(confirmMsg, "Confirmar Ação",
                              MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                Return
            End If

            SetStatus("Processando...", clrYellow)
            btnDisable.Enabled = False
            Application.DoEvents()

            Try
                ' ── Passo 1: Verificar se o usuário existe ──
                AddLog("Verificando usuário '" & username & "' no AD...", clrYellow)
                Dim checkCmd As String = "Import-Module ActiveDirectory" & vbCrLf &
                    "try { Get-ADUser -Identity '" & username & "' | Select-Object -ExpandProperty SamAccountName } catch { Write-Error $_.Exception.Message; exit 1 }"
                Dim checkResult = RunPowerShell(checkCmd)

                If Not checkResult.Success Then
                    AddLog("ERRO: Usuário '" & username & "' não encontrado no AD: " & checkResult.Output, clrRed)
                    SetStatus("● Erro - Usuário não encontrado", clrRed)
                    MessageBox.Show(
                        "Usuário não encontrado no Active Directory:" & vbCrLf & vbCrLf & checkResult.Output,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End If

                ' ── Passo 2: Desabilitar o usuário ──
                AddLog("Desabilitando usuário '" & username & "'...", clrYellow)
                Dim disableCmd As String = "Import-Module ActiveDirectory" & vbCrLf &
                    "Disable-ADAccount -Identity '" & username & "'" & vbCrLf &
                    "Write-Output 'DISABLE_SUCCESS'"
                Dim disableResult = RunPowerShell(disableCmd)

                If Not disableResult.Success Then
                    AddLog("ERRO ao desabilitar '" & username & "': " & disableResult.Output, clrRed)
                    SetStatus("● Erro ao desabilitar", clrRed)
                    MessageBox.Show(
                        "Erro ao desabilitar o usuário:" & vbCrLf & vbCrLf & disableResult.Output,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End If

                AddLog("Usuário '" & username & "' DESABILITADO com sucesso.", clrGreen)

                ' ── Passo 3: Criar script de reativação ──
                Dim taskName As String = "ADReactivate_" & username & "_" & DateTime.Now.ToString("yyyyMMddHHmmss")
                Dim scriptPath As String = Path.Combine(scriptsDir, taskName & ".ps1")

                Dim scriptContent As String =
                    "# ═══════════════════════════════════════════════════════════════" & vbCrLf &
                    "# Script de Reativação Automática - AD User Manager" & vbCrLf &
                    "# Gerado em: " & DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") & vbCrLf &
                    "# Usuário: " & username & vbCrLf &
                    "# ═══════════════════════════════════════════════════════════════" & vbCrLf &
                    "" & vbCrLf &
                    "Import-Module ActiveDirectory" & vbCrLf &
                    "try {" & vbCrLf &
                    "    Enable-ADAccount -Identity '" & username & "'" & vbCrLf &
                    "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                    "    Write-Output ""[$timestamp] Usuário '" & username & "' reativado com sucesso.""" & vbCrLf &
                    "} catch {" & vbCrLf &
                    "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                    "    Write-Error ""[$timestamp] Erro ao reativar '" & username & "': $($_.Exception.Message)""" & vbCrLf &
                    "    exit 1" & vbCrLf &
                    "}"
                File.WriteAllText(scriptPath, scriptContent, System.Text.Encoding.UTF8)

                ' ── Passo 4: Criar tarefa agendada ──
                AddLog("Criando tarefa agendada para reativação...", clrYellow)
                Dim dateStr As String = reactivationDate.ToString("yyyy-MM-ddTHH:mm:ss")

                Dim taskCmd As String =
                    "$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-NoProfile -ExecutionPolicy Bypass -File """ & scriptPath & """'" & vbCrLf &
                    "$trigger = New-ScheduledTaskTrigger -Once -At '" & dateStr & "'" & vbCrLf &
                    "$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DeleteExpiredTaskAfter (New-TimeSpan -Days 30)" & vbCrLf &
                    "$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest" & vbCrLf &
                    "Register-ScheduledTask -TaskName '" & taskName & "' -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Reativar usuario " & username & " no Active Directory - Gerado pelo AD User Manager' -Force" & vbCrLf &
                    "Write-Output 'TASK_CREATED'"

                Dim taskResult = RunPowerShell(taskCmd)

                ' ── Registrar resultado ──
                Dim entry As New TaskEntry()
                entry.Username = username
                entry.DisabledDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                entry.ReactivationDate = reactivationDate.ToString("dd/MM/yyyy HH:mm")
                entry.ScheduledTaskName = taskName

                If taskResult.Success AndAlso taskResult.Output.Contains("TASK_CREATED") Then
                    entry.Status = "Agendado"
                    AddLog("Tarefa agendada '" & taskName & "' criada com sucesso.", clrGreen)
                    AddLog("Reativação programada para: " & reactivationDate.ToString("dd/MM/yyyy") & " às " & reactivationDate.ToString("HH:mm"), clrBlue)
                    SetStatus("● Concluído com sucesso", clrGreen)

                    MessageBox.Show(
                        "Operação realizada com sucesso!" & vbCrLf & vbCrLf &
                        "• Usuário '" & username & "' foi DESABILITADO" & vbCrLf &
                        "• Reativação agendada para " & reactivationDate.ToString("dd/MM/yyyy HH:mm") & vbCrLf & vbCrLf &
                        "Tarefa: " & taskName,
                        "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    entry.Status = "Erro no Agendamento"
                    AddLog("AVISO: Usuário desabilitado, mas ERRO ao criar tarefa agendada: " & taskResult.Output, clrPeach)
                    SetStatus("● Aviso - Tarefa não agendada", clrPeach)

                    MessageBox.Show(
                        "O usuário foi DESABILITADO com sucesso, porém houve um erro ao criar a tarefa agendada:" & vbCrLf & vbCrLf &
                        taskResult.Output & vbCrLf & vbCrLf &
                        "Você precisará REATIVAR o usuário manualmente!",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If

                history.Insert(0, entry) ' Inserir no topo
                SaveHistory()
                RefreshGrid()

                ' Limpar campo
                txtUsername.Clear()
                txtUsername.Focus()

            Finally
                btnDisable.Enabled = True
            End Try
        End Sub

        ''' <summary>
        ''' Reativa imediatamente um usuário selecionado na tabela.
        ''' </summary>
        Private Sub BtnReactivate_Click(sender As Object, e As EventArgs)
            If dgvTasks.SelectedRows.Count = 0 OrElse dgvTasks.Rows.Count = 0 Then
                MessageBox.Show(
                    "Selecione uma tarefa na tabela para reativar o usuário.",
                    "Seleção Necessária",
                    MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim rowIndex As Integer = dgvTasks.SelectedRows(0).Index
            If rowIndex >= history.Count Then Return

            Dim entry As TaskEntry = history(rowIndex)

            If entry.Status = "Reativado" Then
                MessageBox.Show("Este usuário já foi reativado.", "Informação",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If MessageBox.Show(
                "Deseja REATIVAR o usuário '" & entry.Username & "' imediatamente?" & vbCrLf & vbCrLf &
                "A tarefa agendada será removida.",
                "Confirmar Reativação",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                Return
            End If

            SetStatus("Reativando usuário...", clrYellow)
            btnReactivate.Enabled = False
            Application.DoEvents()

            Try
                ' Reativar no AD
                Dim enableCmd As String = "Import-Module ActiveDirectory" & vbCrLf &
                    "Enable-ADAccount -Identity '" & entry.Username & "'" & vbCrLf &
                    "Write-Output 'ENABLE_SUCCESS'"
                Dim result = RunPowerShell(enableCmd)

                If result.Success AndAlso result.Output.Contains("ENABLE_SUCCESS") Then
                    AddLog("Usuário '" & entry.Username & "' REATIVADO com sucesso.", clrGreen)

                    ' Remover tarefa agendada
                    Dim removeCmd As String = "Unregister-ScheduledTask -TaskName '" & entry.ScheduledTaskName & "' -Confirm:$false -ErrorAction SilentlyContinue"
                    RunPowerShell(removeCmd)
                    AddLog("Tarefa agendada '" & entry.ScheduledTaskName & "' removida.", clrSubtext)

                    ' Remover script
                    Dim scriptPath As String = Path.Combine(scriptsDir, entry.ScheduledTaskName & ".ps1")
                    If File.Exists(scriptPath) Then
                        File.Delete(scriptPath)
                    End If

                    entry.Status = "Reativado"
                    SaveHistory()
                    RefreshGrid()
                    SetStatus("● Usuário reativado com sucesso", clrGreen)
                Else
                    AddLog("ERRO ao reativar '" & entry.Username & "': " & result.Output, clrRed)
                    SetStatus("● Erro ao reativar", clrRed)
                    MessageBox.Show(
                        "Erro ao reativar o usuário:" & vbCrLf & vbCrLf & result.Output,
                        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            Finally
                btnReactivate.Enabled = True
            End Try
        End Sub

        ''' <summary>
        ''' Remove um agendamento selecionado ou limpa um registro do histórico.
        ''' </summary>
        Private Sub BtnRemove_Click(sender As Object, e As EventArgs)
            If dgvTasks.SelectedRows.Count = 0 OrElse dgvTasks.Rows.Count = 0 Then
                MessageBox.Show(
                    "Selecione uma tarefa na tabela para remover.",
                    "Seleção Necessária",
                    MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim rowIndex As Integer = dgvTasks.SelectedRows(0).Index
            If rowIndex >= history.Count Then Return

            Dim entry As TaskEntry = history(rowIndex)

            ' Se já foi finalizado, apenas remover do histórico
            If entry.Status = "Reativado" OrElse entry.Status = "Cancelado" Then
                If MessageBox.Show(
                    "Deseja remover este registro do histórico?",
                    "Confirmar Remoção",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                    Return
                End If

                history.RemoveAt(rowIndex)
                SaveHistory()
                RefreshGrid()
                AddLog("Registro de '" & entry.Username & "' removido do histórico.", clrSubtext)
                Return
            End If

            ' Se está agendado, cancelar a tarefa
            If MessageBox.Show(
                "Deseja CANCELAR o agendamento de reativação do usuário '" & entry.Username & "'?" & vbCrLf & vbCrLf &
                "⚠ ATENÇÃO: O usuário permanecerá DESABILITADO no AD!" & vbCrLf &
                "Você precisará reativá-lo manualmente.",
                "Confirmar Cancelamento",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                Return
            End If

            SetStatus("Removendo agendamento...", clrYellow)
            Application.DoEvents()

            ' Remover tarefa agendada
            Dim removeCmd As String = "Unregister-ScheduledTask -TaskName '" & entry.ScheduledTaskName & "' -Confirm:$false -ErrorAction SilentlyContinue"
            RunPowerShell(removeCmd)

            ' Remover script
            Dim scriptPath As String = Path.Combine(scriptsDir, entry.ScheduledTaskName & ".ps1")
            If File.Exists(scriptPath) Then
                File.Delete(scriptPath)
            End If

            entry.Status = "Cancelado"
            SaveHistory()
            RefreshGrid()
            AddLog("Agendamento para '" & entry.Username & "' CANCELADO. Usuário permanece desabilitado.", clrPeach)
            SetStatus("● Agendamento cancelado", clrPeach)
        End Sub

        ''' <summary>
        ''' Formata as células da coluna Status com cores distintas.
        ''' </summary>
        Private Sub DgvTasks_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If e.ColumnIndex = 4 AndAlso e.Value IsNot Nothing Then
                Dim statusFont As New Font("Segoe UI Semibold", 9.5F)
                Select Case e.Value.ToString()
                    Case "Agendado"
                        e.CellStyle.ForeColor = clrYellow
                        e.CellStyle.Font = statusFont
                    Case "Reativado"
                        e.CellStyle.ForeColor = clrGreen
                        e.CellStyle.Font = statusFont
                    Case "Cancelado"
                        e.CellStyle.ForeColor = clrRed
                        e.CellStyle.Font = statusFont
                    Case "Erro no Agendamento"
                        e.CellStyle.ForeColor = clrPeach
                        e.CellStyle.Font = statusFont
                End Select
            End If
        End Sub

#End Region

#Region "Execução do PowerShell"

        ''' <summary>
        ''' Executa um comando PowerShell via script temporário e retorna o resultado.
        ''' </summary>
        ''' <param name="command">Conteúdo do script PowerShell a executar.</param>
        ''' <returns>Tupla com Success (Boolean) e Output (String).</returns>
        Private Function RunPowerShell(command As String) As (Success As Boolean, Output As String)
            Try
                Dim psi As New ProcessStartInfo()
                psi.FileName = "powershell.exe"
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -"
                psi.RedirectStandardInput = True
                psi.RedirectStandardOutput = True
                psi.RedirectStandardError = True
                psi.UseShellExecute = False
                psi.CreateNoWindow = True
                psi.StandardOutputEncoding = System.Text.Encoding.UTF8
                psi.StandardErrorEncoding = System.Text.Encoding.UTF8

                Using proc As Process = Process.Start(psi)
                    proc.StandardInput.WriteLine(command)
                    proc.StandardInput.Close()

                    Dim output As String = ""
                    Dim errors As String = ""

                    ' Read errors asynchronously to avoid deadlocks
                    AddHandler proc.ErrorDataReceived, Sub(sender, ev)
                                                           If Not String.IsNullOrEmpty(ev.Data) Then
                                                               errors &= ev.Data & vbCrLf
                                                           End If
                                                       End Sub
                    proc.BeginErrorReadLine()

                    ' Read output synchronously
                    output = proc.StandardOutput.ReadToEnd()
                    
                    proc.WaitForExit(60000) ' Timeout de 60 segundos

                    If proc.ExitCode = 0 AndAlso String.IsNullOrWhiteSpace(errors) Then
                        Return (True, output.Trim())
                    Else
                        Dim errorMsg As String = If(Not String.IsNullOrWhiteSpace(errors), errors.Trim(), output.Trim())
                        Return (False, errorMsg)
                    End If
                End Using
            Catch ex As Exception
                Return (False, "Exceção: " & ex.Message)
            End Try
        End Function

#End Region

#Region "Gerenciamento de Dados"

        ''' <summary>
        ''' Salva o histórico de tarefas em arquivo JSON.
        ''' </summary>
        Private Sub SaveHistory()
            Try
                Dim options As New JsonSerializerOptions()
                options.WriteIndented = True
                Dim json As String = JsonSerializer.Serialize(history, options)
                File.WriteAllText(historyFile, json, System.Text.Encoding.UTF8)
            Catch ex As Exception
                AddLog("Erro ao salvar histórico: " & ex.Message, clrRed)
            End Try
        End Sub

        ''' <summary>
        ''' Carrega o histórico de tarefas do arquivo JSON.
        ''' </summary>
        Private Sub LoadHistory()
            If File.Exists(historyFile) Then
                Try
                    Dim json As String = File.ReadAllText(historyFile, System.Text.Encoding.UTF8)
                    Dim loaded = JsonSerializer.Deserialize(Of List(Of TaskEntry))(json)
                    If loaded IsNot Nothing Then
                        history = loaded
                    End If
                Catch
                    history = New List(Of TaskEntry)()
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Atualiza a DataGridView com os dados do histórico.
        ''' </summary>
        Private Sub RefreshGrid()
            dgvTasks.Rows.Clear()
            For Each entry In history
                dgvTasks.Rows.Add(
                    entry.Username,
                    entry.DisabledDate,
                    entry.ReactivationDate,
                    entry.ScheduledTaskName,
                    entry.Status)
            Next
        End Sub

#End Region

#Region "Auxiliares de Interface"

        ''' <summary>
        ''' Adiciona uma mensagem colorida ao log de atividades.
        ''' </summary>
        Private Sub AddLog(message As String, color As Color)
            If rtbLog Is Nothing Then Return

            Dim timestamp As String = DateTime.Now.ToString("HH:mm:ss")

            rtbLog.SelectionStart = rtbLog.TextLength
            rtbLog.SelectionLength = 0
            rtbLog.SelectionColor = clrOverlay0
            rtbLog.AppendText("[" & timestamp & "] ")

            rtbLog.SelectionStart = rtbLog.TextLength
            rtbLog.SelectionLength = 0
            rtbLog.SelectionColor = color
            rtbLog.AppendText(message & vbCrLf)

            rtbLog.ScrollToCaret()
        End Sub

        ''' <summary>
        ''' Atualiza o texto e a cor da barra de status.
        ''' </summary>
        Private Sub SetStatus(text As String, color As Color)
            If lblStatus IsNot Nothing Then
                lblStatus.Text = text
                lblStatus.ForeColor = color
            End If
        End Sub

#End Region

    End Class

End Namespace
