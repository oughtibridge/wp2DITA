Public Class ctlSettings
    Private _Settings As ApplicationSettings.Settings.ApplicationSettings

    Public Function GetSettings() As ApplicationSettings.Settings.ApplicationSettings
        Return _Settings
    End Function

    Public Sub SetSettings(Value As ApplicationSettings.Settings.ApplicationSettings)
        _Settings = Value
        For Each ctl As Control In Me.Controls
            Me.Controls.Remove(ctl)
        Next
        For i = Value.Model.ModelledSettings.Count - 1 To 0 Step -1
            Dim s As ApplicationSettings.Settings.ModelledSetting = Value.Model.ModelledSettings(i)

            Select Case s.Type
                Case ApplicationSettings.Settings.SettingType.BooleanSetting
                    Dim ctlCheck As New CheckBox With {
                        .Text = s.Label,
                        .Tag = s.Name,
                        .Checked = (_Settings.GetAppSetting(s.Name) = "true"),
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                        }
                    tt1.SetToolTip(ctlCheck, s.Tooltip)
                    Me.Controls.Add(ctlCheck)
                    AddHandler ctlCheck.CheckedChanged, AddressOf SettingChanged
                Case ApplicationSettings.Settings.SettingType.StringSetting, ApplicationSettings.Settings.SettingType.MultilineStringSetting, ApplicationSettings.Settings.SettingType.ProtectedSetting

                    Dim ctlText As New TextBox With {
                        .Text = _Settings.GetAppSetting(s.Name),
                        .Tag = s.Name,
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                        }

                    If s.Type = ApplicationSettings.Settings.SettingType.MultilineStringSetting Then
                        ctlText.Multiline = True
                        ctlText.Height = (ctlText.Font.Height * 5) + 2
                        ctlText.AcceptsReturn = True
                        ctlText.AcceptsTab = False
                    End If

                    Me.Controls.Add(ctlText)
                    Dim ctlLabel As New Label With {
                        .Text = s.Label,
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                    }
                    tt1.SetToolTip(ctlText, s.Tooltip)

                    Me.Controls.Add(ctlLabel)
                    AddHandler ctlText.TextChanged, AddressOf TextSettingChanged
                Case ApplicationSettings.Settings.SettingType.FilePathSetting, ApplicationSettings.Settings.SettingType.FolderPathSetting
                    Dim ctlpanel As New Panel With {
                        .Dock = DockStyle.Top
                    }

                    Dim ctlText As New TextBox With {
                        .Text = _Settings.GetAppSetting(s.Name),
                        .Tag = s.Label,
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                        }
                    Dim ctlLabel As New Label With {
                        .Text = s.Label,
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                    }
                    Dim ctlBtn As New Button With {
                        .Text = "...",
                        .Width = 25,
                        .Tag = s.Name
                    }
                    ctlBtn.Dock = DockStyle.Right

                    tt1.SetToolTip(ctlText, s.Tooltip)
                    tt1.SetToolTip(ctlpanel, s.Tooltip)

                    ctlpanel.Controls.Add(ctlText)
                    ctlpanel.Controls.Add(ctlBtn)

                    ctlText.Dock = DockStyle.Fill
                    ctlLabel.Dock = DockStyle.Top

                    ctlpanel.Height = ctlText.Height
                    Me.Controls.Add(ctlpanel)
                    Me.Controls.Add(ctlLabel)

                    AddHandler ctlText.TextChanged, AddressOf TextSettingChanged

                    If s.Type = ApplicationSettings.Settings.SettingType.FolderPathSetting Then
                        AddHandler ctlBtn.Click, AddressOf FolderPicker_Click
                    End If
                    If s.Type = ApplicationSettings.Settings.SettingType.FilePathSetting Then
                        AddHandler ctlBtn.Click, AddressOf FilePicker_Click
                    End If

                Case ApplicationSettings.Settings.SettingType.Heading
                    Dim ctlLabel As New Label With {
    .Text = s.Label,
    .Width = Me.Width,
    .Dock = DockStyle.Top
}
                    If i <> Value.Model.ModelledSettings.Count - 1 Then ctlLabel.Padding = New Padding(0, 15, 0, 2)
                    ctlLabel.AutoSize = True
                    ctlLabel.Font = New Font(ctlLabel.Font, FontStyle.Bold)
                    tt1.SetToolTip(ctlLabel, s.Tooltip)

                    Me.Controls.Add(ctlLabel)
            End Select
        Next
    End Sub

    Public Sub FolderPicker_Click(sender As Object, e As EventArgs)
        For Each ctl As Control In sender.parent.controls
            If TypeOf ctl Is TextBox Then
                fbdOpenFolder.SelectedPath = ctl.Text
                fbdOpenFolder.Description = "Bob and co"
                Dim dr As DialogResult = fbdOpenFolder.ShowDialog()
                If dr = DialogResult.OK Then
                    ctl.Text = fbdOpenFolder.SelectedPath
                End If
                Exit For
            End If
        Next

    End Sub

    Public Sub FilePicker_Click(sender As Object, e As EventArgs)
        For Each ctl As Control In sender.parent.controls
            If TypeOf ctl Is TextBox Then

                sfdSaveFile.FileName = ctl.Text
                sfdSaveFile.Title = ctl.Tag
                sfdSaveFile.OverwritePrompt = False
                Dim dr As DialogResult = sfdSaveFile.ShowDialog()
                If dr = DialogResult.OK Then
                    ctl.Text = sfdSaveFile.FileName
                End If
                Exit For
            End If
        Next

    End Sub
    Public Sub SettingChanged(sender As Object, e As EventArgs)
        Dim cb As CheckBox = CType(sender, CheckBox)
        Dim val As String
        If cb.Checked Then
            val = "true"
        Else
            val = "false"
        End If
        _Settings.SetAppSetting(cb.Tag, val)
    End Sub

    Private Sub TextSettingChanged(sender As Object, e As EventArgs)
        Dim tb As TextBox = CType(sender, TextBox)
        _Settings.SetAppSetting(tb.Tag, tb.Text)
    End Sub
End Class
