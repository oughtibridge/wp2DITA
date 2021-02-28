Public Class MainForm
    Dim SettingsModel As String = My.Resources.wp2DITAGUI.SettingsModel
    Public Settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA", SettingsModel)
    Dim WithEvents Generator As New wp2DITA.Generation.WP2DITAConverter
    Private Sub cmdGo_Click(sender As Object, e As EventArgs) Handles cmdGo.Click
        Generator.settings = Settings
        lvErrors.Items.Clear()
        lvErrors.BeginUpdate()

        Generator.Generate()

        For Each c As ColumnHeader In lvErrors.Columns
            c.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent)
        Next
        lvErrors.EndUpdate()

        Application.DoEvents()
    End Sub
    Public LogEntries As List(Of wp2DITA.Generation.LogHandler.LogEntry)
    Public Sub LogEntered(Logentry As wp2DITA.Generation.LogHandler.LogEntry)
        'If Logentry.Level = wp2DITA.Generation.LogLevel.Error Or Logentry.Level = wp2DITA.Generation.LogLevel.Warning Then
        Dim urlString As String = ""
        If Not IsNothing(Logentry.LogURL) Then
            urlString = Logentry.LogURL.ToString

        End If

        Dim itm As New ListViewItem(Logentry.LogMessage)
        itm.SubItems.Add(Logentry.Source)
        itm.SubItems.Add(Logentry.Level.ToString)
        itm.SubItems.Add(urlString)
        itm.SubItems.Add(Logentry.Time.ToString)
        Select Case Logentry.Level
            Case 0
                itm.BackColor = Color.LightBlue
            Case 1
                itm.BackColor = Color.LightGoldenrodYellow
            Case 2
                itm.ForeColor = Color.Red
        End Select
        lvErrors.Items.Add(itm)
        'End If
    End Sub

    Public Sub LogEntryClick(sender As Object, e As EventArgs) Handles lvErrors.DoubleClick
        Dim itm As ListViewItem = CType(sender, ListView).SelectedItems(0)
        Dim url As String = itm.SubItems(3).Text
        If url <> "" Then
            Dim pis As ProcessStartInfo = New ProcessStartInfo(url)
            With pis
                .UseShellExecute = True
            End With
            Process.Start(pis)
        End If
    End Sub
    Public Sub ProgressUpdated(Progress As wp2DITA.Generation.Progress)
        lblProgress.Text = Progress.Stage & " " & Progress.Completed.ToString & "/" & Progress.Articles.ToString
        Application.DoEvents()
    End Sub

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles Me.Load
        AddHandler Generator.ProgressChanged, AddressOf ProgressUpdated
        AddHandler Generator.LogEntered, AddressOf LogEntered

        Dim c0, c1, c2, c3, c4 As New ColumnHeader
        c0.Text = "Log Message"
        c1.Text = "Source"
        c2.Text = "Level"
        c3.Text = "url"
        c4.Text = "Time"
        lvErrors.Columns.Add(c0)
        lvErrors.Columns.Add(c1)

        lvErrors.Columns.Add(c2)

        lvErrors.Columns.Add(c3)

        lvErrors.Columns.Add(c4)

        CtlSettings1.SetSettings(Settings)
    End Sub

End Class
