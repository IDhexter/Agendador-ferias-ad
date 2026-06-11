Imports System.Windows.Forms
Imports System.IO

Module Program

    <STAThread>
    Public Sub Main()
        Dim logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt")

        Try
            File.AppendAllText(logFile, DateTime.Now.ToString() & " - Iniciando aplicação..." & vbCrLf)

            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)

            ' Capturar exceções não tratadas para diagnóstico
            AddHandler Application.ThreadException,
                Sub(sender, e)
                    File.AppendAllText(logFile, DateTime.Now.ToString() & " - ThreadException: " & e.Exception.ToString() & vbCrLf)
                    MessageBox.Show(
                        "Erro inesperado:" & vbCrLf & vbCrLf & e.Exception.Message,
                        "AD User Manager - Erro",
                        MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Sub

            AddHandler AppDomain.CurrentDomain.UnhandledException,
                Sub(sender, e)
                    Dim ex = TryCast(e.ExceptionObject, Exception)
                    Dim msg = If(ex IsNot Nothing, ex.ToString(), e.ExceptionObject.ToString())
                    File.AppendAllText(logFile, DateTime.Now.ToString() & " - UnhandledException: " & msg & vbCrLf)
                    MessageBox.Show(
                        "Erro fatal:" & vbCrLf & vbCrLf & msg,
                        "AD User Manager - Erro Fatal",
                        MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Sub

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)

            File.AppendAllText(logFile, DateTime.Now.ToString() & " - Criando MainForm..." & vbCrLf)
            Dim form = New ADUserManager.MainForm()
            File.AppendAllText(logFile, DateTime.Now.ToString() & " - MainForm criado. Executando Application.Run..." & vbCrLf)
            Application.Run(form)
            File.AppendAllText(logFile, DateTime.Now.ToString() & " - Aplicação encerrada normalmente." & vbCrLf)

        Catch ex As Exception
            File.AppendAllText(logFile, DateTime.Now.ToString() & " - CRASH: " & ex.ToString() & vbCrLf)
            MessageBox.Show(
                "Erro ao iniciar:" & vbCrLf & vbCrLf & ex.Message & vbCrLf & vbCrLf & "Verifique o arquivo crash_log.txt",
                "AD User Manager - Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

End Module
