Public Class MainForm
    Public Settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")
    Dim WithEvents Generator As New wp2DITA.Generation.WP2DITAConverter
    Private Sub cmdGo_Click(sender As Object, e As EventArgs) Handles cmdGo.Click
        Generator.settings = Settings

        Generator.Generate()
    End Sub
    Public LogEntries As List(Of wp2DITA.Generation.LogHandler.LogEntry)
    Public Sub LogEntered(Logentry As wp2DITA.Generation.LogHandler.LogEntry)
        If Logentry.Level = wp2DITA.Generation.LogLevel.Error Or Logentry.Level = wp2DITA.Generation.LogLevel.Warning Then
            lvErrors.Items.Add(New ListViewItem(Logentry.LogMessage))
            Application.DoEvents()
        End If
    End Sub
    Public Sub ProgressUpdated(Progress As wp2DITA.Generation.Progress)
        lblProgress.Text = Progress.Stage & " " & Progress.Completed.ToString & "/" & Progress.Articles.ToString
        Application.DoEvents()
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        AddHandler Generator.ProgressChanged, AddressOf ProgressUpdated
        AddHandler Generator.LogEntered, AddressOf LogEntered

    End Sub
End Class
