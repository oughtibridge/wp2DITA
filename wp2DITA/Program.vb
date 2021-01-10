Imports System.Data
Imports MySql.Data.MySqlClient
Imports System.IO
Imports HtmlAgilityPack
Imports System.Xml.Xsl
Imports System.Xml
Imports System.Net

Module Program
    Dim ID As Integer = 0
    Dim OldPosts As Integer = 0
    Dim logWriter As TextWriter
    Dim conn As New MySql.Data.MySqlClient.MySqlConnection
    Dim outdir As String = "C:\GIT\wp2DITA\out"
    Dim CheckImages As Boolean = False
    Dim Styles As New List(Of Style)
    Dim Client As New WebClient
    Dim Host As String
    Dim settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")
    Dim LastErrorPage As String = "zzz"
    Sub Main(args As String())
        ' Set up the initial values for parameters
        If args.Count = 0 Then
            Console.WriteLine(My.Resources.wp2DITA.Syntax)
        End If
        For Each s As String In args
            If InStr(s, "=") > 0 Then
                Dim nvp() As String = Split(s, "=", 2)
                settings.SetAppSetting(nvp(0), nvp(1))
            Else
                Select Case s.ToLower
                    Case "list", "l"
                        For Each appS In settings.GetAllAppSettings
                            Console.WriteLine(appS.Name & ":" & appS.Sequence.ToString & "=" & appS.Value)
                        Next
                        Console.WriteLine()
                    Case "generate", "g"
                        Generate()
                    Case Else
                        Console.WriteLine("No value specified for " & s)
                End Select
            End If
        Next
    End Sub
    Sub Generate()
        ' Load in the strings from the settings class
        Dim filter As String = "" ' " and ID=3809"
        Dim template As String = settings.GetAppSetting("topic-template", "C:\GIT\wp2DITA\wp2dita\topic_template.html")
        Dim myConnectionString As String = settings.GetAppSetting("connection-string", "")
        Host = settings.GetAppSetting("host", "localhost")

        ' Load template
        Dim TemplateText As String = File.ReadAllText(template)

        Try
            conn.ConnectionString = myConnectionString
            conn.Open()
            If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Console.WriteLine("Opened")
        Catch ex As MySql.Data.MySqlClient.MySqlException
            Console.WriteLine(ex.Message)
            Exit Sub
        End Try
        ' Now we have a database connection
        Dim sql As New MySqlCommand(My.Resources.wp2DITA.sqlPosts & filter, conn)
        Dim TopicsAdaptor = New MySqlDataAdapter(sql)
        CreateCategoryHierarchy(conn)
        CreateCategoryMap(conn)

        logWriter = New System.IO.StreamWriter("c:\git\wp2dita\out\logfile.txt")

        ' Set up an all encompasing DITA Map
        Dim ditaMapDoc As XmlDocument = NewDITAMap()
        Dim maproot As XmlNode = ditaMapDoc.SelectSingleNode("/map")

        Dim dsPosts As New wp2DITA.PostsDataSet
        TopicsAdaptor.Fill(dsPosts, "Posts")

        If settings.GetAppSetting("GenerateQR", "False").ToLower = "true" Then  GenerateQRImage(dsPosts)

        For Each dr As wp2DITA.PostsDataSet.PostsRow In dsPosts.Posts.Rows
            Dim htmlOut As String = outdir & "\" & dr.post_name & ".html"
            Dim ditaOut As String = outdir & "\" & dr.post_name & ".dita"
            Dim ditaoutRef As String = dr.post_name & ".dita"

            ID = CInt(dr("ID"))
            Dim PostContent As String = dr.post_content
            'PostContent = Replace(PostContent, "&nbsp;", "&#xA0;")

            ' Check for any posts that are not HTML
            If InStr(PostContent, "<p>") = 0 Then
                OldPosts += 1

                If settings.GetAppSetting("show-classic-editor", "false").ToLower = "true" Then Console.WriteLine(dr.post_title)
                PostContent = "<p>" & PostContent & "</p>"

                ' Remove multiple linefeeds
                Do While InStr(PostContent, vbLf & vbLf) > 0
                    PostContent = Replace(PostContent, vbLf & vbLf, vbLf)
                Loop
                ' convert linefeed to new paragraph
                PostContent = Replace(PostContent, vbLf, "</p><p>")
            Else
                ' Add to the map
                AddMapRef("topicref", ditaoutRef, "local", ditaMapDoc, maproot)

            End If

            Dim f As String = TemplateText
            f = Replace(f, "${title}", dr.post_title)
            f = Replace(f, "${body}", PostContent)
            f = Replace(f, "${postdate}", dr.post_date.ToString("f"))
            f = Replace(f, "${copyyear}", dr.post_date.ToString("yyyy"))
            Dim htmlDoc = New HtmlDocument()
            htmlDoc.OptionFixNestedTags = True
            htmlDoc.OptionWriteEmptyNodes = True
            htmlDoc.OptionOutputAsXml = False

            htmlDoc.LoadHtml(f)

            Dim str As String = htmlDoc.Text
            'Try
            htmlDoc.Save(htmlOut)

            Dim ditaDoc As XmlDocument = NewTopic(dr.post_date)

            If htmlDoc.DocumentNode.HasChildNodes Then
                ParseNodes(htmlDoc.DocumentNode.ChildNodes, ditaDoc, ditaDoc)
            End If
            AddMetadata(ditaDoc, "/topic", Now())

            ' Write out the DITA topic
            ditaDoc.Save(ditaOut)
        Next
        ditaMapDoc.Save(outdir & "\map.ditamap")

        Dim cssFile As New System.IO.StreamWriter(outdir & "\styles.css")

        For Each s As Style In Styles.OrderByDescending(Function(x) x.Count).ToList()
            cssFile.WriteLine("." & s.Name & "{")
            cssFile.WriteLine("}")
        Next
        If settings.GetAppSetting("show-classic-editor", "false").ToLower = "true" Then Console.WriteLine("There are " & OldPosts.ToString & " posts in the old format")
        logWriter.Close()
        cssFile.Close()
        conn.Close()
        Client.Dispose()
    End Sub

    Private Sub GenerateQRImage(dsPosts As wp2DITA.PostsDataSet)
        ' Generate QR label

        For Each dr As wp2DITA.PostsDataSet.PostsRow In dsPosts.Posts.Rows
            Dim locFile As String = "images/" & dr.post_name & ".svg"
            locFile = System.Web.HttpUtility.UrlDecode(locFile)
            If Not File.Exists(locFile) Then
                Client.DownloadFile("https://api.qrserver.com/v1/create-qr-code/?format=svg&size=100x100&data=" & "https://" & Host & "/" & dr.post_name, outdir & "/" & locFile)

            End If
        Next

    End Sub

    Private Sub AddMetadata(ditaDoc As XmlDocument, XMLPath As String, postDate As Date)
        ' Add metadata
        Dim MetadataContainer As XmlElement
        Select Case XMLPath
            Case "/topic"
                MetadataContainer = AddElementNode(ditaDoc, ditaDoc.SelectSingleNode(XMLPath), "prolog", ElementPositions.AfterFirst)
            Case Else
                MetadataContainer = AddElementNode(ditaDoc, ditaDoc.SelectSingleNode(XMLPath), "topicmeta", ElementPositions.AfterFirst)
        End Select

        ' Add metadata
        AddElementNodeWithText(ditaDoc, MetadataContainer, "author", settings.GetAppSetting("author", ""))
        AddElementNodeWithText(ditaDoc, MetadataContainer, "source", "https://happenence.co.uk")
        AddElementNodeWithText(ditaDoc, MetadataContainer, "publisher", settings.GetAppSetting("publisher", ""))

        Dim copyright As XmlElement = AddElementNode(ditaDoc, MetadataContainer, "copyright")
        Dim copyryear As XmlElement = AddElementNode(ditaDoc, copyright, "copyryear")
        AddAttributeNode(copyryear, "year", postDate.ToString("yyyy"))
        AddElementNodeWithText(ditaDoc, copyright, "copyrholder", settings.GetAppSetting("copyright", ""))



        Dim critdates As XmlElement = AddElementNode(ditaDoc, MetadataContainer, "critdates")
        Dim created As XmlElement = AddElementNode(ditaDoc, critdates, "created")
        AddAttributeNode(created, "date", postDate.ToString("yyyy-MM-dd"))

    End Sub

    Public Sub CreateCategoryHierarchy(conn As MySqlConnection, Optional ParentID As Integer = 0)
        Dim strSql As String = My.Resources.wp2DITA.sqlTaxomony

        Dim sql As New MySqlCommand(strSql, conn)
        ' slug contains the file name to use
        ' taxonomy has the type of taxonomy (category / location / tag etc)

        Dim CategoriesAdaptor = New MySqlDataAdapter(sql)
        Dim dsHierarchy As New wp2DITA.dsHierarchy.HierarchyDataTable
        CategoriesAdaptor.Fill(dsHierarchy)

        Dim LastCat As String = "zzz" ' a magic value to use
        Dim LastTax As String = "zzz" ' a magic value to use

        Dim hierarchyMap As XmlDocument
        Dim TaxMap As XmlDocument

        For Each drEntry In dsHierarchy

            If Not drEntry.IsParent_nameNull Then
                If drEntry.Grandparent_id = ParentID Then
                    If drEntry.taxonomy <> LastTax Then
                        If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Console.WriteLine("Hierarchy " & drEntry.taxonomy)
                        If LastTax <> "zzz" Then
                            ' close last map
                            TaxMap.Save(outdir & "\" & LastTax & ".ditamap")
                        End If
                        TaxMap = NewDITAMap()
                        LastTax = drEntry.taxonomy
                    End If
                    If Not drEntry.IsGrandparent_idNull Then
                        If drEntry.taxonomy & "_" & drEntry.Parent_slug <> LastCat Then
                            If LastCat <> "zzz" Then
                                ' Add a link to posts not sub-divided
                                Dim nodeLast As XmlElement = AddMapRef("mapref", LastCat & ".ditamap", "local", hierarchyMap, hierarchyMap.SelectSingleNode("/map"))
                                'Dim titleLast As XmlElement = AddElementNode(hierarchyMap, hierarchyMap.SelectSingleNode("/map"), "title")
                                ' close last map
                                AddMetadata(hierarchyMap, "/map", Now())
                                hierarchyMap.Save(outdir & "\" & "hierarchy_" & LastCat & ".ditamap")
                            End If
                            hierarchyMap = NewDITAMap()
                            LastCat = drEntry.taxonomy & "_" & drEntry.Parent_slug
                            Dim node As XmlElement = AddMapRef("mapref", "hierarchy_" & LastCat & ".ditamap", "local", TaxMap, TaxMap.SelectSingleNode("/map"))
                            Dim title As XmlElement = AddElementNode(hierarchyMap, hierarchyMap.SelectSingleNode("/map"), "title")
                            AddTextNode(hierarchyMap, title, drEntry.Parent_name)
                        End If
                        Dim n As XmlElement = AddMapRef("mapref", drEntry.taxonomy & "_" & drEntry.slug & ".ditamap", "local", hierarchyMap, hierarchyMap.SelectSingleNode("/map"))
                    End If
                End If
            End If
        Next
        If LastCat <> "zzz" Then
            ' close last category map
            hierarchyMap.Save(outdir & "\" & "hierarchy_" & LastCat & ".ditamap")
        End If

        If LastTax <> "zzz" Then
            ' close the last taxonomy map
            TaxMap.Save(outdir & "\" & LastTax & ".ditamap")
        End If
    End Sub

    Public Sub CreateCategoryMap(conn As MySqlConnection)

        Dim sql As New MySqlCommand(My.Resources.wp2DITA.sqlCategories, conn)
        ' slug contains the file name to use
        ' taxonomy has the type of taxonomy (category / location / tag etc)

        Dim CategoriesAdaptor = New MySqlDataAdapter(sql)
        Dim dsCategories As New wp2DITA._CategoryEntries.CategoryEntriesDataTable
        CategoriesAdaptor.Fill(dsCategories)


        Dim LastCat As String = "zzz" ' a magic value to use
        Dim catMap As New XmlDocument
        For Each drCatEntry In dsCategories
            If LastCat <> drCatEntry.taxonomy & "_" & drCatEntry.slug Then
                If LastCat <> "zzz" Then
                    ' close last map

                    AddMetadata(catMap, "/map", Now())
                    catMap.Save(outdir & "\" & LastCat & ".ditamap")
                End If
                catMap = NewDITAMap()
                LastCat = drCatEntry.taxonomy & "_" & drCatEntry.slug
                Dim t As XmlElement = AddElementNode(catMap, catMap.SelectSingleNode("/map"), "title")
                AddTextNode(catMap, t, drCatEntry.name)

            End If
            AddMapRef("topicref", drCatEntry.post_name & ".dita", "local", catMap, catMap.SelectSingleNode("/map"))
        Next
        If LastCat <> "zzz" Then
            ' close last map
            AddMetadata(catMap, "/map", Now())
            catMap.Save(outdir & "\" & LastCat & ".ditamap")
        End If
    End Sub
    Public Sub ParseNodes(htmlNodes As HtmlNodeCollection, ditaParent As XmlNode, DITADocument As XmlDocument)

        For Each node As HtmlNode In htmlNodes
            Select Case node.Name
                Case "br", "hr"
                    Dim s As XmlElement
                    Select Case ditaParent.Name
                        Case "topic", "body" ' If the target DITA parent is a topic or body, add a child P tag and make that the parent container for future nodes
                            s = AddElementNode(DITADocument, ditaParent, "p")
                            AddAttributeNode(s, "xtrf", "injected P")
                        Case "title" ' can't add a P to a title or have two titles
                            s = AddElementNode(DITADocument, ditaParent, "ph")
                            AddAttributeNode(s, "xtrf", "injected PH")
                        Case Else ' otherwise duplicate the node
                            s = AddElementNode(DITADocument, ditaParent.ParentNode, ditaParent.Name)
                            AddAttributeNode(s, "xtrf", "duplicated")
                            ditaParent = s
                    End Select
                    ' s = AddElementNode(DITADocument, ditaParent.ParentNode, ditaParent.Name)

                    AddAttributeNode(s, "outputclass", "html-" & node.Name)
                    AddTextNode(DITADocument, s, " ")
                Case Else
                    ParseNode(node, ditaParent, DITADocument)

            End Select
        Next

    End Sub
    Public Sub ParseNode(node As HtmlNode, ditaParent As XmlNode, DITADocument As XmlDocument)

        Select Case node.NodeType
            Case HtmlNodeType.Element
                ParseElementNode(node, DITADocument, ditaParent)
            Case HtmlNodeType.Text
                If ditaParent.NodeType = XmlNodeType.Element Then
                    ParseTextNode(node, False, DITADocument, ditaParent)
                End If
            Case HtmlNodeType.Comment
        End Select

    End Sub
    Public Sub ParseTextNode(node As HtmlTextNode, WrapChildText As Boolean, DITADocument As XmlDocument, ditaParent As XmlElement)
        If Trim(CType(node, HtmlTextNode).Text) <> vbLf Then

            Dim t As String = CType(node, HtmlTextNode).Text
            t = Replace(t, "&nbsp;", Chr(160).ToString)
            If WrapChildText Then
                Dim s As XmlElement = AddElementNode(DITADocument, ditaParent, "div")
                AddTextNode(DITADocument, s, t)
            Else
                AddTextNode(DITADocument, ditaParent, t)
            End If
        End If
    End Sub

    Public Sub ParseTypographic(node As HtmlNode, ditaDocument As XmlDocument, ditaParent As XmlElement)
        Dim ditaTag As String
        Select Case node.Name
            Case "strong"
                ditaTag = "b"
            Case "em"
                ditaTag = "i"

            Case "code", "samp", "var", "pre"
                ditaTag = "tt"
            Case "span"
                ditaTag = "ph"
            Case Else
                ditaTag = node.Name
        End Select


        Dim s As XmlNode
        Select Case ditaParent.Name
            Case "title", "shortdesc", "section", "example", "desc", "p", "note", "lq", "q", "sli", "li", "itemgroup", "dthd", "ddhd", "dt", "dd", "figgroup", "pre", "lines", "ph", "stentry", "draft-comment", "fn", "cite", "xref", "linkinfo", "entry", "prereq", "context", "cmd", "info", "tutorialinfo", "stepxmp", "choice", "choptionhd", "chdeschd", "choption", "chdesc", "stepresult", "result", "postreq", "refsyn", "proptypehd", "propvaluehd", "propdeschd", "proptype", "propvalue", "propdesc", "screen", "b", "u", "i", "tt", "sup", "sub", "codeph", "codeblock", "pt", "pd", "fragref", "synnote"
                s = AddElementNode(ditaDocument, ditaParent, ditaTag)
            Case Else
                If ditaParent.HasChildNodes Then
                    Select Case ditaParent.LastChild.Name
                        Case "title", "shortdesc", "section", "example", "desc", "p", "note", "lq", "q", "sli", "li", "itemgroup", "dthd", "ddhd", "dt", "dd", "figgroup", "pre", "lines", "ph", "stentry", "draft-comment", "fn", "cite", "xref", "linkinfo", "entry", "prereq", "context", "cmd", "info", "tutorialinfo", "stepxmp", "choice", "choptionhd", "chdeschd", "choption", "chdesc", "stepresult", "result", "postreq", "refsyn", "proptypehd", "propvaluehd", "propdeschd", "proptype", "propvalue", "propdesc", "screen", "b", "u", "i", "tt", "sup", "sub", "codeph", "codeblock", "pt", "pd", "fragref", "synnote",
"bodydiv", "section"
                            s = AddElementNode(ditaDocument, ditaParent.LastChild, ditaTag)
                        Case Else
                            s = AddElementNode(ditaDocument, ditaParent, ditaTag)
                            If settings.GetAppSetting("show-html-error", "false").ToLower = "true" Then Console.WriteLine("Invalid " & ditaTag & " tag created from " & node.Name)
                    End Select
                Else
                    Dim p As XmlElement = AddElementNode(ditaDocument, ditaParent, "bodydiv")
                    s = AddElementNode(ditaDocument, p, ditaTag)
                    If settings.GetAppSetting("show-html-error", "false").ToLower = "true" Then Console.WriteLine("bodydiv used to wrap " & ditaTag & " tag created from " & node.Name)
                End If
        End Select
        AddAttributeNode(s, "outputclass", "html-" & node.Name)
        ParseNodes(node.ChildNodes, s, ditaDocument)
    End Sub
    Public Sub ParseAttribute(Attribute As HtmlAttribute, DITADocument As XmlDocument, ditaParent As XmlElement)
        Select Case Attribute.Name
            Case "href"
                Dim internal As Boolean = False
                Dim u As New Uri(Attribute.Value)
                '
                If InStr(Attribute.Value, ":  ") > 0 Then

                    If InStr(u.DnsSafeHost, "happenence.co.uk") > 0 And InStr(u.LocalPath, "wp-content") = 0 And u.LocalPath <> "/" Then
                        ' Check the file is still a published post
                        Dim strFilter As String = "And post_name ='" & Replace(u.LocalPath, "/", "") & "'"
                        Dim sql As New MySqlCommand("SELECT count(ID) FROM wp_posts " _
                                    & "WHERE post_type='post' and post_status='publish' " & strFilter, conn)

                        Dim cnt As Integer = sql.ExecuteScalar
                        If cnt = 1 Then
                            internal = True
                        End If
                    End If

                End If


                If internal Then
                    Dim locfile As String = Replace(u.LocalPath, "/", "") & ".dita"
                    AddAttributeNode(ditaParent, "href", locfile)
                    locfile = System.Web.HttpUtility.UrlDecode(locfile)
                    AddAttributeNode(ditaParent, "scope", "local")
                    AddAttributeNode(ditaParent, "format", "dita")
                Else
                    AddAttributeNode(ditaParent, "href", u.AbsoluteUri)
                    AddAttributeNode(ditaParent, "scope", "external")
                    AddAttributeNode(ditaParent, "format", "html")
                End If
            Case "src"
                Dim internal As Boolean = False
                Dim u As New Uri(Attribute.Value)
                Dim locFile As String = ""

                '
                If InStr(Attribute.Value, ":") > 0 Then

                    If InStr(u.DnsSafeHost, "happenence.co.uk") > 0 And InStr(u.LocalPath, "wp-content") > 0 And u.LocalPath <> "/" Then
                        ' download the image to disk

                        If u.Scheme <> "https" Then
                            If settings.GetAppSetting("show-http-errors", "false").ToLower = "true" Then
                                Dim lep As String = ditaParent.SelectSingleNode("/topic/title").InnerText
                                If lep <> LastErrorPage Then
                                    LastErrorPage = lep
                                    Console.WriteLine(LastErrorPage)
                                End If
                                Console.WriteLine("Insecure image - " & u.ToString)
                            End If
                            If u.Scheme = "http" And u.IsAbsoluteUri Then
                                u = New Uri(Replace(u.AbsoluteUri, "http://", "https://"))

                            End If

                            End If

                            Try

                            locFile = "images/" & Replace(u.Segments(u.Segments.Length - 1), "/", "")
                            locFile = System.Web.HttpUtility.UrlDecode(locFile)
                            If Not File.Exists(locFile) And CheckImages Then
                                Client.DownloadFile(u.AbsoluteUri, outdir & "/" & locFile)

                            End If
                            internal = True

                        Catch ex As Exception
                            locFile = "images/404.jpg"
                            Console.WriteLine(u.ToString)
                            internal = True
                        End Try

                    End If

                End If


                If internal Then
                    AddAttributeNode(ditaParent, "href", locFile)
                    AddAttributeNode(ditaParent, "scope", "local")
                    AddAttributeNode(ditaParent, "scalefit", "yes")
                    'AddAttributeNode(ditaParent, "width", "6in")

                Else
                    AddAttributeNode(ditaParent, "href", u.AbsoluteUri)
                    AddAttributeNode(ditaParent, "scope", "external")
                    AddAttributeNode(ditaParent, "format", "jpg")
                End If
            Case "class"
                AddAttributeNode(ditaParent, "outputclass", Attribute.Value)
            Case "alt" ', "height", "width"
                AddAttributeNode(ditaParent, Attribute.Name, Attribute.Value)
            Case Else
                If settings.GetAppSetting("show-skipped", "false").ToLower = "true" Then Console.WriteLine("   Skipped attribute " & Attribute.Name)
        End Select
    End Sub
    Public Sub ParseElementNode(node As HtmlNode, DITADocument As XmlDocument, ditaParent As XmlNode)
        Select Case node.Name
            Case "html"
                ' add a "topic" node and process children
                Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "topic")
                Dim a As XmlAttribute = DITADocument.CreateAttribute("id")
                a.Value = "topic-" & ID.ToString ' This is the ID from the Wordpress database
                s.Attributes.Append(a)
                ParseNodes(node.ChildNodes, s, DITADocument)
            Case "head"
                ' don't add anything but search for title below
                ParseNodes(node.ChildNodes, ditaParent, DITADocument)

            Case "body"
                ParseNodes(node.ChildNodes, AddElementNode(DITADocument, ditaParent, "body"), DITADocument)
            Case "li"
                If ditaParent.Name = "p" Then
                    If settings.GetAppSetting("show-html-errors", "true").ToLower = "true" Then Console.WriteLine("li within p in " & DITADocument.SelectSingleNode("/topic/title").InnerText)

                End If
                AddNodeAsIs(node, DITADocument, ditaParent)
            Case "p", "ul", "ol", "dl", "dt", "title", "div", "cite"
                ' Output as is
                ' If last node is a section, add it to that
                AddNodeAsIs(node, DITADocument, ditaParent)
            Case "h1", "h2", "h3", "h4", "h5", "h6"
                ' Introduce a new section element
                Dim s As XmlNode = AddElementNode(DITADocument, DITADocument.SelectSingleNode("/topic/body"), "section")
                ' Add a "section" node   
                Dim t As XmlElement = AddElementNode(DITADocument, s, "title")
                For Each n As HtmlNode In node.ChildNodes
                    If n.NodeType = HtmlNodeType.Text Then
                        ParseTextNode(n, False, DITADocument, t)
                    Else
                        ParseElementNode(n, DITADocument, t)
                    End If
                Next

                ' Cases where switching elements is needed
            Case "strong", "em", "u", "code", "samp", "var", "pre", "tt", "sub", "sup", "span"
                ParseTypographic(node, DITADocument, ditaParent)

            Case "figure"
                Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "fig")
                For Each a As HtmlAttribute In node.Attributes
                    ParseAttribute(a, DITADocument, s)
                Next
                ParseNodes(node.ChildNodes, s, DITADocument)
            Case "figcaption"
                Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "title", ElementPositions.BeforeLast)
                For Each a As HtmlAttribute In node.Attributes
                    ParseAttribute(a, DITADocument, s)
                Next
                ParseNodes(node.ChildNodes, s, DITADocument)
            Case "blockquote"
                Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "lq")
                For Each a As HtmlAttribute In node.Attributes
                    ParseAttribute(a, DITADocument, s)
                Next
                ParseNodes(node.ChildNodes, s, DITADocument)

                ' Complex elements
            Case "a"
                ' Pick up the element, then walk the attribuites to find the href
                Dim s As XmlNode
                Select Case ditaParent.Name
                    Case "body"
                        Dim p As XmlElement = AddElementNode(DITADocument, ditaParent, "bodydiv")
                        s = AddElementNode(DITADocument, p, "xref")
                    Case "title", "cite"
                        Dim p As XmlElement = AddElementNode(DITADocument, ditaParent, "ph")
                        s = AddElementNode(DITADocument, p, "xref")
                    Case Else
                        s = AddElementNode(DITADocument, ditaParent, "xref")
                End Select
                For Each a As HtmlAttribute In node.Attributes
                    ParseAttribute(a, DITADocument, s)
                Next
                ParseNodes(node.ChildNodes, s, DITADocument)
            Case "img"
                Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "image")
                For Each a As HtmlAttribute In node.Attributes
                    ParseAttribute(a, DITADocument, s)
                Next


            Case Else
                If settings.GetAppSetting("show-skipped", "false").ToLower = "true" Then Console.WriteLine("Skipped " & node.Name)
        End Select
    End Sub

    Private Sub AddNodeAsIs(node As HtmlNode, DITADocument As XmlDocument, ditaParent As XmlNode)
        Dim s As XmlNode
        If ditaParent.HasChildNodes Then
            Select Case ditaParent.LastChild.Name
                Case "section"
                    s = AddElementNode(DITADocument, ditaParent.LastChild, node.Name)

                Case Else
                    s = AddElementNode(DITADocument, ditaParent, node.Name)
            End Select
        Else
            s = AddElementNode(DITADocument, ditaParent, node.Name)

        End If
        For Each a As HtmlAttribute In node.Attributes
            ParseAttribute(a, DITADocument, s)
        Next
        ParseNodes(node.ChildNodes, s, DITADocument)
    End Sub

    Public Function NewDITAMap() As XmlDocument
        Dim ditaMapDoc As New XmlDocument
        ditaMapDoc.AppendChild(ditaMapDoc.CreateXmlDeclaration("1.0", "UTF-8", Nothing))
        Dim docMapType As XmlDocumentType = ditaMapDoc.CreateDocumentType("map", "-//OASIS//DTD DITA Map//EN", "map.dtd", Nothing)
        ditaMapDoc.AppendChild(docMapType)
        Dim mapRoot As XmlElement = AddElementNode(ditaMapDoc, ditaMapDoc, "map")
        Return (ditaMapDoc)
    End Function
    Public Function NewTopic(PublishDate As Date) As XmlDocument
        Dim ditaDoc As New XmlDocument
        ditaDoc.AppendChild(ditaDoc.CreateXmlDeclaration("1.0", "UTF-8", Nothing))
        Dim docType As XmlDocumentType = ditaDoc.CreateDocumentType("topic", "-//OASIS//DTD DITA Topic//EN", "topic.dtd", Nothing)
        ditaDoc.AppendChild(docType)
        Return ditaDoc
    End Function
    Public Function AddMapRef(RefType As String, href As String, scope As String, Document As XmlDocument, ParentNode As XmlNode) As XmlNode
        Dim mapref As XmlNode = AddElementNode(Document, ParentNode, RefType)
        Dim maphref As XmlAttribute = Document.CreateAttribute("href")
        Dim mapScope As XmlAttribute = Document.CreateAttribute("scope")
        maphref.Value = href
        mapScope.Value = scope
        mapref.Attributes.Append(maphref)
        mapref.Attributes.Append(mapScope)
        Return mapref
    End Function

    Public Function AddElementNode(Document As XmlDocument, Parent As XmlNode, Name As String, Optional Position As ElementPositions = ElementPositions.AfterLast) As XmlNode
        Dim n As XmlElement = Document.CreateElement(Name)
        Select Case Position
            Case ElementPositions.BeforeLast
                Parent.InsertBefore(n, Parent.LastChild)
            Case ElementPositions.AfterLast
                Parent.AppendChild(n)
            Case ElementPositions.Beginning
                Parent.InsertBefore(n, Parent.FirstChild)
            Case ElementPositions.AfterFirst

                Parent.InsertAfter(n, Parent.FirstChild)
            Case Else
                Throw New Exception("Unexpected element position in AddElementNode")
        End Select
        Return n
    End Function
    Public Enum ElementPositions
        Beginning
        AfterFirst
        BeforeLast
        AfterLast
    End Enum
    Public Function AddElementNodeWithText(Document As XmlDocument, Parent As XmlNode, Name As String, Text As String, Optional Prepend As ElementPositions = ElementPositions.AfterLast) As XmlNode
        Dim n As XmlElement = AddElementNode(Document, Parent, Name, Prepend)
        Dim t As XmlText = Document.CreateTextNode(Text)
        n.AppendChild(t)
        Return n
    End Function
    Public Sub AddAttributeNode(Parent As XmlElement, Name As String, Value As String)
        Parent.SetAttribute(Name, Value)
        If Name = "outputclass" Then
            For Each s As String In Value.Split(" ")
                Dim f As List(Of Style) = Styles.FindAll(Function(p) p.Name = s)
                If f.Count > 0 Then
                    f(0).Count += 1
                Else

                    Styles.Add(New Style With {
                               .Name = s,
                               .Count = 0
                               })
                End If
            Next
        End If
    End Sub
    Public Function AddTextNode(Document As XmlDocument, Parent As XmlNode, Text As String) As XmlText

        Dim n As XmlText
        Select Case Parent.Name
            Case "alt", "cite", "desc", "dd", "ddhd", "dl", "dlentry", "dlhead", "dt", "dthd", "fig", "figgroup", "image", "keyword", "li", "lines", "lq", "note", "object", "ol", "p", "param", "ph", "pre", "q", "sl", "sli", "ul", "xref", "title", "b", "i", "u", "tt", "sup", "sub", "section", "bodydiv"
                n = Document.CreateTextNode(Text)
                Parent.AppendChild(n)
            Case Else
                If Parent.HasChildNodes Then
                    Select Case Parent.LastChild.Name
                        Case "p", "section", "bodydiv"

                            n = Document.CreateTextNode(Text)
                            Parent.LastChild.AppendChild(n)
                        Case Else
                            Dim s As XmlElement = AddElementNode(Document, Parent, "p")
                            n = AddTextNode(Document, s, Text)
                    End Select

                Else

                    Dim s As XmlElement = AddElementNode(Document, Parent, "p")
                    n = AddTextNode(Document, s, Text)
                End If
        End Select

        Return n
    End Function


    Public Class Style
        Property Name As String
        Property Count As Integer = 0
    End Class
End Module