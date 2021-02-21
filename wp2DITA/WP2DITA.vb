Imports System.IO
Imports ApplicationSettings
Imports HtmlAgilityPack
Namespace wp2DITA
    Class WP2DITAConverter

    End Class
    Public Enum LogLevel
        Information
        Warning
        [Error]
    End Enum
    Class LogHandler

        Property OutputDirectory As String
        Private Settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")
        Property LogEntries As New List(Of LogHandler.LogEntry)

        Public Sub AddLogEntry(Source As String, Level As LogLevel, Message As String, Optional URL As String = "")
            Dim LE As New LogEntry With {
                .Level = Level, .LogMessage = Message, .Time = Now()}
            If URL <> "" Then
                If Left(URL, 4) <> "http" Then
                    URL = "https://" & Settings.GetAppSetting("host") & "/" & URL
                End If
                LE.LogURL = New Uri(URL)
            End If
            LogEntries.Add(LE)
            Console.WriteLine(Now.ToString("yyyy-MMM-dd HH:mm:ss:fff") & " " & LE.LogMessage)
        End Sub

        Public Sub WriteLogHTML()

            Dim entries = From le In LogEntries
                          Order By le.Time, le.Level Descending, le.LogMessage
                          Select le.Level, le.Time, le.LogURL, le.LogMessage
            ' Load template
            Dim template As String = Settings.GetAppSetting("topic-template", "topic_template.html")
            Dim TemplateText As String = File.ReadAllText(template)


            Dim Body As String = "<table>" & vbCrLf
            Body &= "<tr><th>Page</th><th>Message</th></tr>" & vbCrLf
            For Each e In entries
                Dim u As String
                If Not IsNothing(e.LogURL) Then
                    u = "<a href=" & e.LogURL.ToString & ">" & e.Level.ToString & "</a>"
                Else
                    u = e.Level.ToString
                End If
                Body &= "<tr><td>" & u & "</td><td>" & e.LogMessage & "</td></tr>" & vbCrLf
            Next
            Dim f As String = TemplateText
            f = Replace(f, "${title}", "Errors")
            f = Replace(f, "${body}", Body)
            f = Replace(f, "${postdate}", Now.ToString("dd MMM yyyy HH:mm:ss"))
            f = Replace(f, "${copyyear}", Now.ToString("yyyy"))
            Dim htmlDoc = New HtmlDocument()
            htmlDoc.OptionFixNestedTags = True
            htmlDoc.OptionWriteEmptyNodes = True
            htmlDoc.OptionOutputAsXml = False

            htmlDoc.LoadHtml(f)
            htmlDoc.Save(OutputDirectory & "\errors.htm")
        End Sub

        Public Class LogEntry
            Public Property Level As LogLevel
            Public Property LogMessage As String
            Public Property LogURL As Uri
            Public Property Time As DateTime

        End Class
    End Class
End Namespace
