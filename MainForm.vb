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
        Public Property ScheduledDisableTaskName As String = ""
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
        Private rbDisableNow As RadioButton
        Private rbDisableSchedule As RadioButton
        Private dtpDisableDate As DateTimePicker
        Private dtpDisableTime As DateTimePicker
        Private dtpDate As DateTimePicker
        Private dtpTime As DateTimePicker
        Private btnDisable As Button
        Private btnReactivate As Button
        Private btnRemove As Button
        Private dgvTasks As DataGridView
        Private txtSearch As TextBox
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

        Private dragging As Boolean = False
        Private startPoint As Point = Point.Empty

        Private Sub InitializeUI()
            ' ─── Configurações do Formulário ───
            Me.Text = "AD User Manager"
            Me.ClientSize = New Size(940, 810)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = clrCrust
            Me.ForeColor = clrText
            Me.Font = New Font("Segoe UI", 10)
            Me.FormBorderStyle = FormBorderStyle.None ' Borderless Window
            Me.MaximizeBox = False
            Me.DoubleBuffered = True

            ' ═══════════════════════════════════════════════════════
            '  TITLE BAR CUSTOMIZADA
            ' ═══════════════════════════════════════════════════════
            Dim pnlTitleBar As New Panel()
            pnlTitleBar.Bounds = New Rectangle(0, 0, 940, 40)
            pnlTitleBar.BackColor = clrMantle
            AddHandler pnlTitleBar.MouseDown, Sub(s, e)
                                                  If e.Button = MouseButtons.Left Then
                                                      dragging = True
                                                      startPoint = New Point(e.X, e.Y)
                                                  End If
                                              End Sub
            AddHandler pnlTitleBar.MouseMove, Sub(s, e)
                                                  If dragging Then
                                                      Dim p As Point = PointToScreen(e.Location)
                                                      Location = New Point(p.X - startPoint.X, p.Y - startPoint.Y)
                                                  End If
                                              End Sub
            AddHandler pnlTitleBar.MouseUp, Sub(s, e) dragging = False
            Me.Controls.Add(pnlTitleBar)

            Dim lblTitle As New Label()
            lblTitle.Text = Char.ConvertFromUtf32(&H1F512) & " AD User Manager"
            lblTitle.Font = New Font("Segoe UI Semibold", 11)
            lblTitle.ForeColor = clrBlue
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(15, 10)
            AddHandler lblTitle.MouseDown, Sub(s, e)
                                               If e.Button = MouseButtons.Left Then
                                                   dragging = True
                                                   startPoint = New Point(e.X + lblTitle.Left, e.Y + lblTitle.Top)
                                               End If
                                           End Sub
            AddHandler lblTitle.MouseMove, Sub(s, e)
                                               If dragging Then
                                                   Dim p As Point = PointToScreen(e.Location)
                                                   Location = New Point(p.X - startPoint.X, p.Y - startPoint.Y)
                                               End If
                                           End Sub
            AddHandler lblTitle.MouseUp, Sub(s, e) dragging = False
            pnlTitleBar.Controls.Add(lblTitle)

            Dim btnAbout As New Button()
            btnAbout.Text = Char.ConvertFromUtf32(&H1F310) & " Sobre"
            btnAbout.Size = New Size(80, 30)
            btnAbout.Location = New Point(810, 5)
            btnAbout.FlatStyle = FlatStyle.Flat
            btnAbout.BackColor = clrMantle
            btnAbout.ForeColor = clrSubtext
            btnAbout.FlatAppearance.BorderSize = 0
            btnAbout.Cursor = Cursors.Hand
            AddHandler btnAbout.Click, Sub() Process.Start(New ProcessStartInfo("https://github.com/IDhexter/Agendador-ferias-ad") With {.UseShellExecute = True})
            AddHandler btnAbout.MouseEnter, Sub(s, e)
                                                btnAbout.BackColor = clrSurface0
                                                btnAbout.ForeColor = clrText
                                            End Sub
            AddHandler btnAbout.MouseLeave, Sub(s, e)
                                                btnAbout.BackColor = clrMantle
                                                btnAbout.ForeColor = clrSubtext
                                            End Sub
            pnlTitleBar.Controls.Add(btnAbout)

            Dim btnClose As New Button()
            btnClose.Text = "X"
            btnClose.Font = New Font("Segoe UI Semibold", 10)
            btnClose.Size = New Size(40, 40)
            btnClose.Location = New Point(900, 0)
            btnClose.FlatStyle = FlatStyle.Flat
            btnClose.FlatAppearance.BorderSize = 0
            btnClose.BackColor = clrMantle
            btnClose.ForeColor = clrSubtext
            btnClose.Cursor = Cursors.Hand
            AddHandler btnClose.Click, Sub() Me.Close()
            AddHandler btnClose.MouseEnter, Sub(s, e)
                                                btnClose.BackColor = clrRed
                                                btnClose.ForeColor = clrCrust
                                            End Sub
            AddHandler btnClose.MouseLeave, Sub(s, e)
                                                btnClose.BackColor = clrMantle
                                                btnClose.ForeColor = clrSubtext
                                            End Sub
            pnlTitleBar.Controls.Add(btnClose)

            ' ═══════════════════════════════════════════════════════
            '  CARTÃO 1: ENTRADA DE DADOS E AÇÕES
            ' ═══════════════════════════════════════════════════════
            Dim pnlCardInput As New Panel()
            pnlCardInput.Bounds = New Rectangle(20, 60, 900, 240)
            pnlCardInput.BackColor = clrBase
            Me.Controls.Add(pnlCardInput)

            Dim pnlInputAccent As New Panel()
            pnlInputAccent.Bounds = New Rectangle(0, 0, 4, 240)
            pnlInputAccent.BackColor = clrBlue
            pnlCardInput.Controls.Add(pnlInputAccent)

            ' Linha 1: Usuário
            Dim lblUser As New Label()
            lblUser.Text = "Usuário AD:"
            lblUser.Font = New Font("Segoe UI Semibold", 10)
            lblUser.ForeColor = clrSubtext
            lblUser.AutoSize = True
            lblUser.Location = New Point(20, 25)
            pnlCardInput.Controls.Add(lblUser)

            txtUsername = New TextBox()
            txtUsername.Size = New Size(660, 28)
            txtUsername.Location = New Point(135, 22)
            txtUsername.BackColor = clrSurface0
            txtUsername.ForeColor = clrText
            txtUsername.BorderStyle = BorderStyle.FixedSingle
            txtUsername.Font = New Font("Segoe UI", 11)
            pnlCardInput.Controls.Add(txtUsername)

            ' Linha 2: Data e Hora de Desativação
            Dim lblDisable As New Label()
            lblDisable.Text = "Desativação:"
            lblDisable.Font = New Font("Segoe UI Semibold", 10)
            lblDisable.ForeColor = clrSubtext
            lblDisable.AutoSize = True
            lblDisable.Location = New Point(20, 75)
            pnlCardInput.Controls.Add(lblDisable)

            rbDisableNow = New RadioButton()
            rbDisableNow.Text = "Imediata"
            rbDisableNow.Font = New Font("Segoe UI", 10)
            rbDisableNow.ForeColor = clrText
            rbDisableNow.Location = New Point(135, 73)
            rbDisableNow.AutoSize = True
            rbDisableNow.Checked = True
            pnlCardInput.Controls.Add(rbDisableNow)

            rbDisableSchedule = New RadioButton()
            rbDisableSchedule.Text = "Agendar"
            rbDisableSchedule.Font = New Font("Segoe UI", 10)
            rbDisableSchedule.ForeColor = clrText
            rbDisableSchedule.Location = New Point(230, 73)
            rbDisableSchedule.AutoSize = True
            pnlCardInput.Controls.Add(rbDisableSchedule)

            dtpDisableDate = New DateTimePicker()
            dtpDisableDate.Format = DateTimePickerFormat.Short
            dtpDisableDate.Size = New Size(120, 28)
            dtpDisableDate.Location = New Point(320, 72)
            dtpDisableDate.MinDate = DateTime.Today
            dtpDisableDate.Value = DateTime.Today
            dtpDisableDate.Font = New Font("Segoe UI", 10)
            dtpDisableDate.Visible = False
            pnlCardInput.Controls.Add(dtpDisableDate)

            dtpDisableTime = New DateTimePicker()
            dtpDisableTime.Format = DateTimePickerFormat.Time
            dtpDisableTime.ShowUpDown = True
            dtpDisableTime.Size = New Size(90, 28)
            dtpDisableTime.Location = New Point(450, 72)
            dtpDisableTime.Value = DateTime.Today.AddHours(18)
            dtpDisableTime.Font = New Font("Segoe UI", 10)
            dtpDisableTime.Visible = False
            pnlCardInput.Controls.Add(dtpDisableTime)

            AddHandler rbDisableSchedule.CheckedChanged, Sub(s, ev)
                                                             dtpDisableDate.Visible = rbDisableSchedule.Checked
                                                             dtpDisableTime.Visible = rbDisableSchedule.Checked
                                                         End Sub

            ' Linha 3: Data e Hora de Reativação
            Dim lblDate As New Label()
            lblDate.Text = "Reativação:"
            lblDate.Font = New Font("Segoe UI Semibold", 10)
            lblDate.ForeColor = clrSubtext
            lblDate.AutoSize = True
            lblDate.Location = New Point(20, 125)
            pnlCardInput.Controls.Add(lblDate)

            dtpDate = New DateTimePicker()
            dtpDate.Format = DateTimePickerFormat.Short
            dtpDate.Size = New Size(150, 28)
            dtpDate.Location = New Point(135, 122)
            dtpDate.MinDate = DateTime.Today
            dtpDate.Value = DateTime.Today.AddDays(1)
            dtpDate.Font = New Font("Segoe UI", 10)
            pnlCardInput.Controls.Add(dtpDate)

            Dim lblTime As New Label()
            lblTime.Text = "Hora:"
            lblTime.Font = New Font("Segoe UI Semibold", 10)
            lblTime.ForeColor = clrSubtext
            lblTime.AutoSize = True
            lblTime.Location = New Point(305, 125)
            pnlCardInput.Controls.Add(lblTime)

            dtpTime = New DateTimePicker()
            dtpTime.Format = DateTimePickerFormat.Time
            dtpTime.ShowUpDown = True
            dtpTime.Size = New Size(120, 28)
            dtpTime.Location = New Point(355, 122)
            dtpTime.Value = DateTime.Today.AddHours(8)
            dtpTime.Font = New Font("Segoe UI", 10)
            pnlCardInput.Controls.Add(dtpTime)

            ' Botões de Ação
            btnDisable = CreateStyledButton(Char.ConvertFromUtf32(&H1F4C5) & "  Aplicar Agendamento", clrBlue, clrCrust, 280)
            btnDisable.Location = New Point(20, 175)
            pnlCardInput.Controls.Add(btnDisable)
            AddHandler btnDisable.Click, AddressOf BtnDisable_Click
            AddHandler txtUsername.KeyDown, Sub(s, ev)
                                                If ev.KeyCode = Keys.Enter Then
                                                    BtnDisable_Click(Nothing, EventArgs.Empty)
                                                    ev.SuppressKeyPress = True
                                                End If
                                            End Sub

            btnReactivate = CreateStyledButton(Char.ConvertFromUtf32(&H26A1) & "  Reativar Agora", clrSurface1, clrText, 200)
            btnReactivate.Location = New Point(320, 175)
            pnlCardInput.Controls.Add(btnReactivate)
            AddHandler btnReactivate.Click, AddressOf BtnReactivate_Click

            btnRemove = CreateStyledButton(Char.ConvertFromUtf32(&H1F5D1) & "  Remover Agendamento", clrSurface1, clrText, 230)
            btnRemove.Location = New Point(540, 175)
            pnlCardInput.Controls.Add(btnRemove)
            AddHandler btnRemove.Click, AddressOf BtnRemove_Click

            ' ═══════════════════════════════════════════════════════
            '  CARTÃO 2: TABELA DE TAREFAS
            ' ═══════════════════════════════════════════════════════
            Dim pnlCardGrid As New Panel()
            pnlCardGrid.Bounds = New Rectangle(20, 320, 900, 250)
            pnlCardGrid.BackColor = clrBase
            Me.Controls.Add(pnlCardGrid)

            Dim pnlGridAccent As New Panel()
            pnlGridAccent.Bounds = New Rectangle(0, 0, 4, 250)
            pnlGridAccent.BackColor = clrMauve
            pnlCardGrid.Controls.Add(pnlGridAccent)

            Dim lblGrid As New Label()
            lblGrid.Text = "Tarefas Agendadas"
            lblGrid.Font = New Font("Segoe UI Semibold", 11)
            lblGrid.ForeColor = clrMauve
            lblGrid.AutoSize = True
            lblGrid.Location = New Point(15, 15)
            pnlCardGrid.Controls.Add(lblGrid)

            Dim lblSearch As New Label()
            lblSearch.Text = "Buscar Usuário:"
            lblSearch.Font = New Font("Segoe UI", 9.5F)
            lblSearch.ForeColor = clrSubtext
            lblSearch.AutoSize = True
            lblSearch.Location = New Point(590, 17)
            pnlCardGrid.Controls.Add(lblSearch)

            txtSearch = New TextBox()
            txtSearch.Size = New Size(190, 24)
            txtSearch.Location = New Point(695, 15)
            txtSearch.BackColor = clrSurface0
            txtSearch.ForeColor = clrText
            txtSearch.BorderStyle = BorderStyle.FixedSingle
            AddHandler txtSearch.TextChanged, Sub(s, ev) RefreshGrid()
            pnlCardGrid.Controls.Add(txtSearch)

            dgvTasks = New DataGridView()
            dgvTasks.Location = New Point(20, 50)
            dgvTasks.Size = New Size(860, 185)

            dgvTasks.BackgroundColor = clrMantle
            dgvTasks.GridColor = clrSurface0
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

            dgvTasks.DefaultCellStyle.BackColor = clrSurface0
            dgvTasks.DefaultCellStyle.ForeColor = clrText
            dgvTasks.DefaultCellStyle.SelectionBackColor = clrSurface2
            dgvTasks.DefaultCellStyle.SelectionForeColor = clrText
            dgvTasks.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
            dgvTasks.DefaultCellStyle.Padding = New Padding(8, 2, 4, 2)

            dgvTasks.AlternatingRowsDefaultCellStyle.BackColor = clrMantle

            dgvTasks.ColumnHeadersDefaultCellStyle.BackColor = clrCrust
            dgvTasks.ColumnHeadersDefaultCellStyle.ForeColor = clrSubtext
            dgvTasks.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgvTasks.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            dgvTasks.ColumnHeadersDefaultCellStyle.Padding = New Padding(8, 0, 0, 0)

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
            pnlCardGrid.Controls.Add(dgvTasks)

            ' ═══════════════════════════════════════════════════════
            '  CARTÃO 3: LOG DE ATIVIDADES
            ' ═══════════════════════════════════════════════════════
            Dim pnlCardLog As New Panel()
            pnlCardLog.Bounds = New Rectangle(20, 590, 900, 160)
            pnlCardLog.BackColor = clrBase
            Me.Controls.Add(pnlCardLog)

            Dim pnlLogAccent As New Panel()
            pnlLogAccent.Bounds = New Rectangle(0, 0, 4, 160)
            pnlLogAccent.BackColor = clrGreen
            pnlCardLog.Controls.Add(pnlLogAccent)

            Dim lblLog As New Label()
            lblLog.Text = "Log de Atividades"
            lblLog.Font = New Font("Segoe UI Semibold", 11)
            lblLog.ForeColor = clrMauve
            lblLog.AutoSize = True
            lblLog.Location = New Point(15, 10)
            pnlCardLog.Controls.Add(lblLog)

            rtbLog = New RichTextBox()
            rtbLog.Location = New Point(20, 35)
            rtbLog.Size = New Size(860, 110)
            rtbLog.BackColor = clrMantle
            rtbLog.ForeColor = clrText
            rtbLog.Font = New Font("Consolas", 9.5F)
            rtbLog.BorderStyle = BorderStyle.None
            rtbLog.ReadOnly = True
            rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical
            pnlCardLog.Controls.Add(rtbLog)

            ' ═══════════════════════════════════════════════════════
            '  BARRA DE STATUS
            ' ═══════════════════════════════════════════════════════
            Dim pnlStatus As New Panel()
            pnlStatus.Bounds = New Rectangle(0, 770, 940, 40)
            pnlStatus.BackColor = clrMantle
            Me.Controls.Add(pnlStatus)

            lblStatus = New Label()
            lblStatus.Text = "● Pronto"
            lblStatus.Font = New Font("Segoe UI", 9.5F)
            lblStatus.ForeColor = clrGreen
            lblStatus.AutoSize = True
            lblStatus.Location = New Point(15, 10)
            pnlStatus.Controls.Add(lblStatus)

            Dim lblVersion As New Label()
            lblVersion.Text = "v1.0  |  Corporate Edition"
            lblVersion.Font = New Font("Segoe UI", 8.5F)
            lblVersion.ForeColor = clrOverlay0
            lblVersion.AutoSize = True
            lblVersion.Location = New Point(770, 12)
            pnlStatus.Controls.Add(lblVersion)
        End Sub

        Private Function CreateStyledButton(text As String, bgColor As Color, txtColor As Color, width As Integer) As Button
            Dim btn As New Button()
            btn.Text = text
            btn.Size = New Size(width, 42)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = bgColor
            btn.ForeColor = txtColor
            btn.Font = New Font("Segoe UI Semibold", 10)
            btn.Cursor = Cursors.Hand
            btn.TextAlign = ContentAlignment.MiddleCenter

            Dim originalColor = bgColor
            AddHandler btn.MouseEnter, Sub(s, e)
                                           DirectCast(s, Button).BackColor = ControlPaint.Light(originalColor, 0.15F)
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
            
            Dim disableDate As DateTime = DateTime.Now
            Dim disableNow As Boolean = rbDisableNow.Checked
            
            If Not disableNow Then
                disableDate = dtpDisableDate.Value.Date.Add(dtpDisableTime.Value.TimeOfDay)
                If disableDate <= DateTime.Now Then
                    MessageBox.Show(
                        "A data/hora de desativação deve ser no futuro.",
                        "Data Inválida",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                If disableDate >= reactivationDate Then
                    MessageBox.Show(
                        "A data de desativação deve ser ANTERIOR à data de reativação.",
                        "Data Inválida",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
            Else
                If reactivationDate <= DateTime.Now Then
                    MessageBox.Show(
                        "A data/hora de reativação deve ser no futuro.",
                        "Data Inválida",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
            End If

            ' ── Confirmação ──
            Dim confirmMsg As String = "Deseja executar as seguintes ações?" & vbCrLf & vbCrLf
            If disableNow Then
                confirmMsg &= "1. DESABILITAR o usuário '" & username & "' AGORA no Active Directory" & vbCrLf
            Else
                confirmMsg &= "1. AGENDAR a desativação do usuário '" & username & "' para " & disableDate.ToString("dd/MM/yyyy HH:mm") & vbCrLf
            End If
            confirmMsg &= "2. AGENDAR a reativação automática para " & reactivationDate.ToString("dd/MM/yyyy HH:mm") & vbCrLf & vbCrLf & "Confirma?"

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

                Dim taskNameDisable As String = ""

                If disableNow Then
                    ' ── Passo 2: Desabilitar o usuário agora ──
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
                Else
                    ' ── Passo 2b: Agendar Desativação (Zero-Scripts) ──
                    taskNameDisable = "ADDisable_" & username & "_" & DateTime.Now.ToString("yyyyMMddHHmmss")
                    
                    Dim disableScriptContent As String =
                        "Import-Module ActiveDirectory" & vbCrLf &
                        "try {" & vbCrLf &
                        "    Disable-ADAccount -Identity '" & username & "'" & vbCrLf &
                        "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                        "    Write-Output ""[$timestamp] Usuário '" & username & "' desabilitado com sucesso.""" & vbCrLf &
                        "} catch {" & vbCrLf &
                        "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                        "    Write-Error ""[$timestamp] Erro ao desabilitar '" & username & "': $($_.Exception.Message)""" & vbCrLf &
                        "    exit 1" & vbCrLf &
                        "}" & vbCrLf &
                        "Unregister-ScheduledTask -TaskName '" & taskNameDisable & "' -Confirm:$false -ErrorAction SilentlyContinue"
                    
                    Dim disableBytes As Byte() = System.Text.Encoding.Unicode.GetBytes(disableScriptContent)
                    Dim disableBase64 As String = Convert.ToBase64String(disableBytes)

                    AddLog("Criando tarefa agendada para desativação...", clrYellow)
                    Dim dDateStr As String = disableDate.ToString("yyyy-MM-ddTHH:mm:ss")
                    Dim dTaskCmd As String =
                        "$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " & disableBase64 & "'" & vbCrLf &
                        "$trigger = New-ScheduledTaskTrigger -Once -At '" & dDateStr & "'" & vbCrLf &
                        "$trigger.EndBoundary = (Get-Date '" & dDateStr & "').AddDays(30).ToString('s')" & vbCrLf &
                        "$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DeleteExpiredTaskAfter (New-TimeSpan -Days 30)" & vbCrLf &
                        "$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest" & vbCrLf &
                        "Register-ScheduledTask -TaskName '" & taskNameDisable & "' -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Desabilitar usuario " & username & " no Active Directory' -Force" & vbCrLf &
                        "Write-Output 'TASK_CREATED'"
                    Dim dTaskResult = RunPowerShell(dTaskCmd)
                    If dTaskResult.Success AndAlso dTaskResult.Output.Contains("TASK_CREATED") Then
                        AddLog("Tarefa de desativação agendada para " & disableDate.ToString("dd/MM/yyyy HH:mm"), clrGreen)
                    Else
                        AddLog("ERRO ao agendar desativação: " & dTaskResult.Output, clrRed)
                        MessageBox.Show("Erro ao agendar desativação.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                End If

                ' ── Passo 3: Criar tarefa de reativação (Zero-Scripts) ──
                Dim taskName As String = "ADReactivate_" & username & "_" & DateTime.Now.ToString("yyyyMMddHHmmss")

                Dim scriptContent As String =
                    "Import-Module ActiveDirectory" & vbCrLf &
                    "try {" & vbCrLf &
                    "    Enable-ADAccount -Identity '" & username & "'" & vbCrLf &
                    "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                    "    Write-Output ""[$timestamp] Usuário '" & username & "' reativado com sucesso.""" & vbCrLf &
                    "} catch {" & vbCrLf &
                    "    $timestamp = Get-Date -Format 'dd/MM/yyyy HH:mm:ss'" & vbCrLf &
                    "    Write-Error ""[$timestamp] Erro ao reativar '" & username & "': $($_.Exception.Message)""" & vbCrLf &
                    "    exit 1" & vbCrLf &
                    "}" & vbCrLf &
                    "Unregister-ScheduledTask -TaskName '" & taskName & "' -Confirm:$false -ErrorAction SilentlyContinue"

                Dim scriptBytes As Byte() = System.Text.Encoding.Unicode.GetBytes(scriptContent)
                Dim scriptBase64 As String = Convert.ToBase64String(scriptBytes)

                AddLog("Criando tarefa agendada para reativação...", clrYellow)
                Dim dateStr As String = reactivationDate.ToString("yyyy-MM-ddTHH:mm:ss")
                Dim taskCmd As String =
                    "$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -EncodedCommand " & scriptBase64 & "'" & vbCrLf &
                    "$trigger = New-ScheduledTaskTrigger -Once -At '" & dateStr & "'" & vbCrLf &
                    "$trigger.EndBoundary = (Get-Date '" & dateStr & "').AddDays(30).ToString('s')" & vbCrLf &
                    "$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -DeleteExpiredTaskAfter (New-TimeSpan -Days 30)" & vbCrLf &
                    "$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest" & vbCrLf &
                    "Register-ScheduledTask -TaskName '" & taskName & "' -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Reativar usuario " & username & " no Active Directory' -Force" & vbCrLf &
                    "Write-Output 'TASK_CREATED'"

                Dim taskResult = RunPowerShell(taskCmd)

                ' ── Registrar resultado ──
                Dim entry As New TaskEntry()
                entry.Username = username
                entry.DisabledDate = If(disableNow, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), disableDate.ToString("dd/MM/yyyy HH:mm"))
                entry.ReactivationDate = reactivationDate.ToString("dd/MM/yyyy HH:mm")
                entry.ScheduledTaskName = taskName
                entry.ScheduledDisableTaskName = taskNameDisable

                If taskResult.Success AndAlso taskResult.Output.Contains("TASK_CREATED") Then
                    entry.Status = "Agendado"
                    AddLog("Tarefa agendada '" & taskName & "' criada com sucesso.", clrGreen)
                    AddLog("Reativação programada para: " & reactivationDate.ToString("dd/MM/yyyy") & " às " & reactivationDate.ToString("HH:mm"), clrBlue)
                    SetStatus("● Concluído com sucesso", clrGreen)

                    Dim sucessMsg As String = "Operação realizada com sucesso!" & vbCrLf & vbCrLf
                    If disableNow Then
                        sucessMsg &= "• Usuário '" & username & "' foi DESABILITADO" & vbCrLf
                    Else
                        sucessMsg &= "• Desativação agendada para " & disableDate.ToString("dd/MM/yyyy HH:mm") & vbCrLf
                    End If
                    sucessMsg &= "• Reativação agendada para " & reactivationDate.ToString("dd/MM/yyyy HH:mm") & vbCrLf & vbCrLf & "Tarefa: " & taskName
                    MessageBox.Show(sucessMsg, "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    entry.Status = "Erro no Agendamento"
                    AddLog("AVISO: Usuário inativado, mas ERRO ao criar tarefa agendada: " & taskResult.Output, clrPeach)
                    SetStatus("● Aviso - Tarefa não agendada", clrPeach)
                    MessageBox.Show(
                        "Houve um erro ao criar a tarefa agendada:" & vbCrLf & vbCrLf &
                        taskResult.Output,
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

                    ' Remover tarefa de desativação
                    If Not String.IsNullOrEmpty(entry.ScheduledDisableTaskName) Then
                        RunPowerShell("Unregister-ScheduledTask -TaskName '" & entry.ScheduledDisableTaskName & "' -Confirm:$false -ErrorAction SilentlyContinue")
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

            ' Remover tarefa de desativação
            If Not String.IsNullOrEmpty(entry.ScheduledDisableTaskName) Then
                RunPowerShell("Unregister-ScheduledTask -TaskName '" & entry.ScheduledDisableTaskName & "' -Confirm:$false -ErrorAction SilentlyContinue")
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
            Dim filter As String = If(txtSearch IsNot Nothing AndAlso txtSearch.Text IsNot Nothing, txtSearch.Text.Trim().ToLower(), "")

            For Each entry In history
                If String.IsNullOrEmpty(filter) OrElse entry.Username.ToLower().Contains(filter) Then
                    dgvTasks.Rows.Add(
                        entry.Username,
                        entry.DisabledDate,
                        entry.ReactivationDate,
                        entry.ScheduledTaskName,
                        entry.Status)
                End If
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
