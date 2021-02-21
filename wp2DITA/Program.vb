Imports wp2DITA.Generation
Module Program

    Public Enum ElementPositions
        Beginning
        AfterFirst
        BeforeLast
        AfterLast
    End Enum
    Dim WithEvents Converter As New WP2DITAConverter


    Sub Main(args As String())
        Converter.Log = New LogHandler
        Converter.Log.AddLogEntry("Main-1", LogLevel.Information, "Started")
        ' Set up the initial values for parameters

        ' Get the configuration
        Dim settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")


        Dim Help As Boolean = False
        If args.Count = 0 Then
            Help = True
        End If
        For Each s As String In args
            If InStr(s, "=") > 0 Then
                Dim nvp() As String = Split(s, "=", 2)
                settings.SetAppSetting(nvp(0), nvp(1))
            Else
                If Left(s, 1) = "/" Or Left(s, 1) = "-" Then
                    s = Mid(s, 2)
                End If
                Select Case s.ToLower
                    Case "list", "l"
                        For Each appS In settings.GetAllAppSettings
                            Console.WriteLine(appS.Name & "=" & appS.Value)
                        Next

                    Case "generate", "g"

                        Converter.Generate()
                        Converter.Log.WriteLogHTML()
                    Case "help", "?"
                        Help = True
                    Case Else
                        Converter.Log.AddLogEntry("Main-2", LogLevel.Error, "Syntax error: " & s, "")
                        Help = True
                End Select
            End If
        Next
        If Help Then
            WriteHelp()
        End If
    End Sub

    Sub LogEventResponse(ByVal LogEntry As LogHandler.LogEntry) Handles Converter.LogEntered
        Console.WriteLine(LogEntry.LogMessage)
    End Sub

    Private Sub WriteHelp()
        Console.WriteLine(My.Resources.Gen.Syntax)
    End Sub
End Module