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
                    Me.Controls.Add(ctlCheck)
                    AddHandler ctlCheck.CheckedChanged, AddressOf SettingChanged
                Case ApplicationSettings.Settings.SettingType.StringSetting, ApplicationSettings.Settings.SettingType.FilePathSetting, ApplicationSettings.Settings.SettingType.ProtectedSetting
                    Dim ctlLabel As New Label With {
                        .Text = s.Label,
                        .Width = Me.Width,
                                                .Dock = DockStyle.Top
                    }
                    Me.Controls.Add(ctlLabel)
                    Dim ctlText As New TextBox With {
                        .Text = _Settings.GetAppSetting(s.Name),
                        .Tag = s.Name,
                        .Width = Me.Width,
                        .Dock = DockStyle.Top
                        }
                    Me.Controls.Add(ctlText)
                    AddHandler ctlText.TextChanged, AddressOf TextSettingChanged
                Case ApplicationSettings.Settings.SettingType.Heading
                    Dim ctlLabel As New Label With {
    .Text = s.Label,
    .Width = Me.Width,
    .Dock = DockStyle.Top
}
                    Me.Controls.Add(ctlLabel)
            End Select
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
