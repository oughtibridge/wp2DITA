Imports System.IO
Imports System.Net
Imports HtmlAgilityPack
Imports System.Data
Imports MySql.Data.MySqlClient
Imports wp2DITA.wp2DITA._CategoryEntries
Imports System.Xml

Namespace Generation

    Public Enum ElementPositions
        Beginning
        AfterFirst
        BeforeLast
        AfterLast
    End Enum

    Public Class wp2DITADocumentSet
        Public Property ID As Integer
        Public Property DITADocument As XmlDocument
        Public Property HTMLFile As HtmlDocument
        Public Property OutputDirectory As String
        Public Property HTMLTemplate As String = ""

        Public Sub NewDitaDocument()
            DITADocument = New XmlDocument
            DITADocument.AppendChild(DITADocument.CreateXmlDeclaration("1.0", "UTF-8", Nothing))
            Dim docType As XmlDocumentType = DITADocument.CreateDocumentType("topic", "-//OASIS//DTD DITA Topic//EN", "topic.dtd", Nothing)
            DITADocument.AppendChild(docType)
        End Sub
        Public Sub New(Settings As ApplicationSettings.Settings.ApplicationSettings, PostTitle As String, PostContent As String, PostDate As DateTime)
            OutputDirectory = Settings.GetAppSetting("output-directory", "..\")
            NewHTMLDocument(Settings, PostTitle, PostContent, PostDate)
            NewDitaDocument()
        End Sub

        Private Sub NewHTMLDocument(Settings As ApplicationSettings.Settings.ApplicationSettings, PostTitle As String, PostContent As String, PostDate As Date)
            ' Load template
            HTMLTemplate = Settings.GetAppSetting("topic-template", "topic_template.html")
            Dim TemplateText As String = File.ReadAllText(HTMLTemplate)


            Dim f As String = TemplateText
            f = Replace(f, "${title}", PostTitle)
            f = Replace(f, "${body}", PostContent)
            f = Replace(f, "${postdate}", PostDate.ToString("f"))
            f = Replace(f, "${copyyear}", PostDate.ToString("yyyy"))
            HTMLFile = New HtmlDocument()
            HTMLFile.OptionFixNestedTags = True
            HTMLFile.OptionWriteEmptyNodes = True
            HTMLFile.OptionOutputAsXml = False

            HTMLFile.LoadHtml(f)
        End Sub
    End Class

    Public Class Progress
        Property Articles As Integer
        Property Completed As Integer
        Function Percentage() As Double
            Return Completed * 100 / Articles
        End Function

        Property Stage As String
    End Class
    Public Class WP2DITAConverter
        Property settings As ApplicationSettings.Settings.ApplicationSettings

        'Dim ID As Integer = 0
        Dim OldPosts As Integer = 0
        'Dim logWriter As TextWriter
        Dim conn As New MySql.Data.MySqlClient.MySqlConnection
        Dim OutputDirectory As String = ""
        Dim Styles As New List(Of Style)
        Dim Client As New WebClient
        Dim Host As String
        Dim LastErrorPage As String = "zzz"
        Dim CategoriesAdaptor As MySqlDataAdapter
        Dim dsCategories As CategoryEntriesDataTable
        Dim Closure As List(Of ClosureEntry)
        Public WithEvents Log As LogHandler
        Property DocSet As wp2DITADocumentSet

        Public Event LogEntered(ByVal LogEntry As LogHandler.LogEntry)
        Public Event ProgressChanged(ByVal Progress As Progress)

        Public Sub HandleLogEntered(LogEntry As LogHandler.LogEntry) Handles Log.EventLogged
            RaiseEvent LogEntered(LogEntry)
        End Sub
        Public Sub New()
            Log = New LogHandler
            settings = New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")
            OutputDirectory = settings.GetAppSetting("output-directory", "")
        End Sub
        Sub Generate()
            ' Load in the strings from the settings class
            Dim filter As String = "" ' "ID=4677"
            Dim Progress As New Progress
            Progress.Articles = 0
            Progress.Completed = 0
            Progress.Stage = "Starting ..."
            RaiseEvent ProgressChanged(Progress)

            Dim myConnectionString As String = settings.GetAppSetting("connection-string", "")
            Host = settings.GetAppSetting("host", "localhost")


            Try
                conn.ConnectionString = myConnectionString
                conn.Open()
                If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("Generate-1", LogLevel.Information, "Opened", "")
            Catch ex As MySql.Data.MySqlClient.MySqlException
                Log.AddLogEntry("Generate-2", LogLevel.Error, ex.Message, "")
                Exit Sub
            End Try

            ' Now we have a database connection
            Dim sql As New MySqlCommand(My.Resources.Gen.sqlPosts, conn)
            Dim TopicsAdaptor = New MySqlDataAdapter(sql)

            Closure = BuildClosure(conn)
            Progress.Stage = "Closure built ..."
            RaiseEvent ProgressChanged(Progress)
            'CreateCategoryHierarchy(conn)
            CreateCategoryMap2(conn)
            CreateCategoryMap(conn)

            Progress.Stage = "Maps built ..."
            RaiseEvent ProgressChanged(Progress)
            '        logWriter = New System.IO.StreamWriter(settings.GetAppSetting("log-file", "..\logfile.txt"))

            ' Set up an all encompasing DITA Map
            Dim ditaMapDoc As XmlDocument = NewDITAMap()
            Dim maproot As XmlNode = ditaMapDoc.SelectSingleNode("/map")

            Dim dsPosts As New wp2DITA.PostsDataSet
            TopicsAdaptor.Fill(dsPosts, "Posts")

            Progress.Stage = "Connected ..."
            RaiseEvent ProgressChanged(Progress)

            If settings.GetAppSetting("GenerateQR", "False").ToLower = "true" Then GenerateQRImage(dsPosts)
            Progress.Stage = "Generating topics"
            Progress.Completed = 0
            Progress.Articles = dsPosts.Posts.Count
            RaiseEvent ProgressChanged(Progress)
            For Each dr As wp2DITA.PostsDataSet.PostsRow In dsPosts.Posts.Select(filter)
                DocSet = New wp2DITADocumentSet(settings, dr.post_title, dr.post_content, dr.post_date)
                DocSet.ID = dr.ID

                If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("Generate", LogLevel.Information, "Starting topic:" & dr.post_name, dr.post_name)
                Dim htmlOut As String = DocSet.OutputDirectory & "\" & dr.post_name & ".html"
                Dim ditaOut As String = DocSet.OutputDirectory & "\" & dr.post_name & ".dita"
                Dim ditaoutRef As String = dr.post_name & ".dita"


                Dim PostContent As String = dr.post_content
                'PostContent = Replace(PostContent, "&nbsp;", "&#xA0;")

                ' Check for any posts that are not HTML
                If InStr(PostContent, "<p>") = 0 Then
                    OldPosts += 1

                    If settings.GetAppSetting("show-classic-editor", "false").ToLower = "true" Then
                        Log.AddLogEntry("Generate-3", LogLevel.Information, "Classic editor used for '" & dr.post_title & "'", dr.post_name)
                    End If
                    PostContent = "<p>" & PostContent & "</p>"

                    ' Remove multiple linefeeds
                    Do While InStr(PostContent, vbLf & vbLf) > 0
                        PostContent = Replace(PostContent, vbLf & vbLf, vbLf)
                    Loop
                    ' convert linefeed to new paragraph
                    PostContent = Replace(PostContent, vbLf, "</p><p>")
                End If

                ' Add to the map
                Dim MapEntry As XmlElement = AddMapRef("topicref", ditaoutRef, "local", ditaMapDoc, maproot)

                'Try
                DocSet.HTMLFile.Save(htmlOut)


                If DocSet.HTMLFile.DocumentNode.HasChildNodes Then
                    ParseNodes(DocSet.HTMLFile.DocumentNode.ChildNodes, DocSet.DITADocument, DocSet.DITADocument)
                End If

                ' Purge empty P

                If settings.GetAppSetting("purge-empty-paragraphs", "true") = "true" Then
                    For Each x As XmlElement In DocSet.DITADocument.SelectNodes("/topic/body/p")
                        If Trim(x.InnerText) = "" Or x.InnerText = "\r" Or x.InnerText = vbCr Then

                            'x.InnerText = ""
                            If x.ChildNodes.Count = 0 Then '  And x.FirstChild.NodeType = XmlNodeType.Text Then
                                x.ParentNode.RemoveChild(x)
                            End If
                        End If
                    Next
                End If

                AddMetadata(DocSet.DITADocument, "/topic", dr.post_date)

                Dim metadata As XmlElement = AddElementNode(DocSet.DITADocument, DocSet.DITADocument.SelectSingleNode("/topic/prolog"), "metadata", ElementPositions.AfterLast)
                ' Add categories
                ' Write out each category as a DITA Category.
                ' The <category> element represents any category by which a topic might be classified for retrieval or navigation.
                For Each drCategory As CategoryEntriesRow In dsCategories.Select("id=" & dr.ID.ToString)
                    AddElementNodeWithText(DocSet.DITADocument, metadata, "category", drCategory.name)
                Next

                ' Add keywords
                ' The <keywords> element contains a list of terms from a controlled or uncontrolled subject vocabulary that applies to the topic or map. 
                ' The keywords can be used by a search engine. The keywords are marked up using the <indexterm> And/Or <keyword> elements.
                ' For the prolog we use indexterm

                Dim keywords As XmlElement = AddElementNode(DocSet.DITADocument, metadata, "keywords")
                For Each drCategory As CategoryEntriesRow In dsCategories.Select("id=" & dr.ID.ToString)
                    Dim catList As String = drCategory.name
                    AddElementNodeWithText(DocSet.DITADocument, keywords, "indexterm", catList)

                    Dim Ancestors = From cl In Closure
                                    Where cl.Descendant = drCategory.name
                                    Order By cl.Generations Ascending
                                    Select cl.Ancestor

                    For Each Ancestor In Ancestors
                        AddElementNodeWithText(DocSet.DITADocument, keywords, "indexterm", Ancestor)
                    Next


                Next

                Dim cats = From d In dsCategories
                           Where d.ID = dr.ID
                           Order By d.taxonomy Ascending, d.name Ascending
                           Select d.taxonomy, d.name

                If cats.Count > 0 Then
                    ' Add the published content for the categories
                    Dim footerSection As XmlElement = AddElementNode(DocSet.DITADocument, DocSet.DITADocument.SelectSingleNode("/topic/body"), "section", ElementPositions.AfterLast)
                    Dim footerTitle As XmlElement = AddElementNodeWithText(DocSet.DITADocument, footerSection, "title",
                                                                   settings.GetAppSetting("category-list-heading", "Filed under"))
                    AddAttributeNode(footerTitle, "outputclass", "wp2dita-footer-title")
                    Dim sl As XmlElement = AddElementNode(DocSet.DITADocument, footerSection, "sl")
                    AddAttributeNode(sl, "outputclass", "wp2dita-category-list")
                    For Each drCategory In cats
                        AddAttributeNode(AddElementNodeWithText(DocSet.DITADocument, sl, "sli", drCategory.name & "; "), "outputclass", "wp2dita-category-item wp2dita-category-type-" & drCategory.taxonomy)

                        Dim Ancestors = From cl In Closure
                                        Where cl.Descendant = drCategory.name
                                        Order By cl.Generations Ascending
                                        Select cl.Ancestor
                        For Each Ancestor In Ancestors
                            AddAttributeNode(AddElementNodeWithText(DocSet.DITADocument, sl, "sli", Ancestor & "; "), "outputclass", "wp2dita-category-item wp2dita-category-type-" & drCategory.taxonomy)
                        Next
                    Next
                End If
                AddOtherProps(MapEntry, dr.ID, dr.post_type, dr.post_name)

                Dim PubSection As XmlElement = AddElementNode(DocSet.DITADocument, DocSet.DITADocument.SelectSingleNode("/topic/body"), "section", ElementPositions.AfterLast)
                Dim PubTitle As XmlElement = AddElementNodeWithText(DocSet.DITADocument, PubSection, "title",
                                                                   settings.GetAppSetting("published-heading", "Published"))
                AddAttributeNode(PubTitle, "outputclass", "wp2dita-footer-title")
                AddElementNodeWithText(DocSet.DITADocument, PubSection, "p", Format(dr.post_date, "dddd, dd MMMM yyyy"))
                ' Write out the DITA topic

                Dim writer As New XmlTextWriter(ditaOut, Nothing)
                writer.Indentation = 5
                writer.Formatting = Formatting.Indented
                DocSet.DITADocument.Save(writer)
                writer.Close()
                If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("Generate", LogLevel.Information, "Completed topic:" & dr.post_name, dr.post_name)
                Progress.Completed += 1
                RaiseEvent ProgressChanged(Progress)
            Next
            ditaMapDoc.Save(DocSet.OutputDirectory & "\map.ditamap")

            WriteStyes(Styles)

            If settings.GetAppSetting("show-classic-editor", "false").ToLower = "true" Then Log.AddLogEntry("Generate-4", LogLevel.Information, "There are " & OldPosts.ToString & " posts in the old format")

            conn.Close()
            Client.Dispose()

            Progress.Stage = "Completed."
            RaiseEvent ProgressChanged(Progress)
        End Sub


        Private Sub WriteStyes(Styles As List(Of Style))
            Dim cssFile As New System.IO.StreamWriter(DocSet.OutputDirectory & "\styles.css")

            For Each s As Style In Styles.OrderByDescending(Function(x) x.Count).ToList()
                cssFile.WriteLine("." & s.Name & "{")
                cssFile.WriteLine("}")
            Next
            cssFile.Close()
        End Sub

        Private Function BuildClosure(Conn As MySqlConnection) As List(Of ClosureEntry)
            If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("BuildClosure", LogLevel.Information, "Building closure", "")
            Dim Result As New List(Of ClosureEntry)

            Dim strSql As String = My.Resources.Gen.sqlHierarchy

            Dim sql As New MySqlCommand(strSql, Conn)
            ' slug contains the file name to use
            ' taxonomy has the type of taxonomy (category / location / tag etc)

            Dim CategoriesAdaptor = New MySqlDataAdapter(sql)
            Dim dsHierarchy As New DataSet
            CategoriesAdaptor.Fill(dsHierarchy)

            For Each h As DataRow In dsHierarchy.Tables(0).Rows
                Dim Entry As New ClosureEntry With {
                    .Ancestor = h("Ancestor"),
                    .Descendant = h("Descendant"),
                    .Generations = 1,
                    .Taxonomy = h("Taxonomy")
                }
                Result.Add(Entry)
            Next
            Dim NextCount As Integer = Result.Count
            Dim Generation As Integer = 1
            While NextCount <> 0
                NextCount = 0
                Dim NextGen As New List(Of ClosureEntry)
                For Each ce As ClosureEntry In From r As ClosureEntry In Result
                                               Where r.Generations = Generation
                    Dim Parents = From p In Result
                                  Where p.Descendant = ce.Ancestor And p.Generations = 1


                    For Each ce2 As ClosureEntry In Parents
                        NextGen.Add(New ClosureEntry With {
                                   .Ancestor = ce2.Ancestor,
                                   .Descendant = ce.Descendant,
                                   .Taxonomy = ce.Taxonomy,
                                   .Generations = Generation + 1}
                                   )
                        NextCount += 1
                    Next
                Next
                Result.AddRange(NextGen)
                Generation += 1
            End While

            If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("BuildClosure", LogLevel.Information, "Built closure", "")
            Return Result
        End Function

        Private Sub GenerateQRImage(dsPosts As wp2DITA.PostsDataSet)
            ' Generate QR label

            For Each dr As wp2DITA.PostsDataSet.PostsRow In dsPosts.Posts.Rows
                Dim locFile As String = "images/" & dr.post_name & ".svg"
                locFile = System.Web.HttpUtility.UrlDecode(locFile)
                If Not File.Exists(locFile) Then
                    Client.DownloadFile("https://api.qrserver.com/v1/create-qr-code/?format=svg&size=100x100&data=" & "https://" & Host & "/" & dr.post_name, DocSet.OutputDirectory & "/" & locFile)

                End If
            Next

        End Sub


        Public Sub CreateCategoryHierarchy(conn As MySqlConnection, Optional ParentID As Integer = 0)
            Dim strSql As String = My.Resources.Gen.sqlTaxomony

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
            Dim TaxMapList As New List(Of String)
            For Each drEntry In dsHierarchy

                If Not drEntry.IsParent_nameNull Then
                    If drEntry.Grandparent_id = ParentID Then
                        If drEntry.taxonomy <> LastTax Then
                            If settings.GetAppSetting("show-progress", "false").ToLower = "true" Then Log.AddLogEntry("CreateCategoryHierarchy", LogLevel.Information, "Hierarchy " & drEntry.taxonomy)
                            If LastTax <> "zzz" Then

                                ' close last map
                                Dim TaxMapFilename As String = DocSet.OutputDirectory & "\" & LastTax & ".ditamap"
                                TaxMapList.Add(LastTax & ".ditamap")
                                TaxMap.Save(TaxMapFilename)
                                createBookmap(TaxMapList, LastTax)
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
                                    hierarchyMap.Save(DocSet.OutputDirectory & "\" & "hierarchy_" & LastCat & ".ditamap")
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
                hierarchyMap.Save(DocSet.OutputDirectory & "\" & "hierarchy_" & LastCat & ".ditamap")
            End If

            If LastTax <> "zzz" Then
                ' close the last taxonomy map
                TaxMap.Save(DocSet.OutputDirectory & "\" & LastTax & ".ditamap")
            End If
        End Sub

        Public Sub createBookmap(IncludedMaps As List(Of String), Title As String)
            Dim bookmapDoc As New XmlDocument
            bookmapDoc.Load(settings.GetAppSetting("bookmap-template", ""))
            AddMetadata(bookmapDoc, "/bookmap", Now())
            For Each map As String In IncludedMaps
                Dim Chapter As XmlElement = bookmapDoc.CreateElement("chapter")
                AddAttributeNode(Chapter, "href", Replace(map, "\", "/"))
                AddAttributeNode(Chapter, "format", "ditamap")
                bookmapDoc.DocumentElement.InsertBefore(Chapter, bookmapDoc.SelectSingleNode("/bookmap/backmatter"))
            Next
            AddTextNode(bookmapDoc, bookmapDoc.SelectSingleNode("/bookmap/booktitle/mainbooktitle"), "A title")
            AddTextNode(bookmapDoc, bookmapDoc.SelectSingleNode("/bookmap/booktitle/booktitlealt"), "A subtitle")
            bookmapDoc.Save(DocSet.OutputDirectory & "\" & Title & ".bookmap")
        End Sub
        Public Sub CreateCategoryMap2(conn As MySqlConnection)
            Dim sql As New MySqlCommand(My.Resources.Gen.sqlCategories, conn)
            CategoriesAdaptor = New MySqlDataAdapter(sql)
            dsCategories = New wp2DITA._CategoryEntries.CategoryEntriesDataTable
            CategoriesAdaptor.Fill(dsCategories)

            Dim AllCategories = From c In dsCategories
                                Select c.name, c.taxonomy Distinct

            For Each c In AllCategories
                ' Get the list of posts in the category or any subcategories
                Dim SubCategories = From cl In Closure
                                    Where cl.Ancestor = c.name
                                    Select cl.Descendant, cl.Taxonomy

                Dim inClause As New List(Of String)
                inClause.Add(c.name) ' Add the top level category

                For Each sc In SubCategories
                    inClause.Add(sc.Descendant) ' Add the descendant
                Next

                Dim Posts = From p As CategoryEntriesRow In dsCategories
                            Where inClause.Contains(p.name)
                            Order By p.post_date_gmt
                            Select p.post_name, p.ID, p.post_type

                ' Now set up the map file
                Dim catMap As XmlDocument = NewDITAMap()
                Dim t As XmlElement = AddElementNode(catMap, catMap.SelectSingleNode("/map"), "title")
                AddTextNode(catMap, t, c.name)
                AddMetadata(catMap, "/map", Now())
                Dim filename As String = OutputDirectory & "\" & c.taxonomy & "_" & Replace(c.name, "/", "") & "-2.ditamap"
                For Each post In Posts
                    Dim tr As XmlElement = AddMapRef("topicref", post.post_name & ".dita", "local", catMap, catMap.SelectSingleNode("/map"))
                    AddOtherProps(tr, post.ID, post.post_type, post.post_name)
                Next

                ' Save the ditamap
                catMap.Save(filename)

            Next

        End Sub
        Public Sub CreateCategoryMap(conn As MySqlConnection)

            Dim sql As New MySqlCommand(My.Resources.Gen.sqlCategories, conn)
            ' slug contains the file name to use
            ' taxonomy has the type of taxonomy (category / location / tag etc)

            CategoriesAdaptor = New MySqlDataAdapter(sql)
            dsCategories = New wp2DITA._CategoryEntries.CategoryEntriesDataTable
            CategoriesAdaptor.Fill(dsCategories)

            Dim MapList As New List(Of String)


            Dim LastCat As String = "zzz" ' a magic value to use
            Dim catMap As New XmlDocument
            For Each drCatEntry In dsCategories
                If LastCat <> drCatEntry.taxonomy & "_" & drCatEntry.slug Then
                    If LastCat <> "zzz" Then
                        ' close last map
                        Dim filename As String = OutputDirectory & "\" & LastCat & ".ditamap"
                        MapList.Add(filename)
                        AddMetadata(catMap, "/map", Now())

                        catMap.Save(filename)
                    End If
                    catMap = NewDITAMap()
                    LastCat = drCatEntry.taxonomy & "_" & drCatEntry.slug
                    Dim t As XmlElement = AddElementNode(catMap, catMap.SelectSingleNode("/map"), "title")
                    AddTextNode(catMap, t, drCatEntry.name)

                End If
                If drCatEntry.post_type = "post" Then
                    Dim tr As XmlElement = AddMapRef("topicref", drCatEntry.post_name & ".dita", "local", catMap, catMap.SelectSingleNode("/map"))
                    AddOtherProps(tr, drCatEntry.ID, drCatEntry.post_type, drCatEntry.post_name)
                End If
            Next
            If LastCat <> "zzz" Then
                ' close last map
                AddMetadata(catMap, "/map", Now())
                catMap.Save(OutputDirectory & "\" & LastCat & ".ditamap")
            End If

        End Sub
        Public Function NewDITAMap() As XmlDocument
            Dim ditaMapDoc As New XmlDocument
            ditaMapDoc.AppendChild(ditaMapDoc.CreateXmlDeclaration("1.0", "UTF-8", Nothing))
            Dim docMapType As XmlDocumentType = ditaMapDoc.CreateDocumentType("map", "-//OASIS//DTD DITA Map//EN", "map.dtd", Nothing)
            ditaMapDoc.AppendChild(docMapType)
            Dim mapRoot As XmlElement = AddElementNode(ditaMapDoc, ditaMapDoc, "map")
            Return (ditaMapDoc)
        End Function
        Private Sub AddMetadata(Document As XmlDocument, XMLPath As String, FirstDate As Date)
            ' Add metadata
            Dim MetadataContainer As XmlElement
            Select Case XMLPath
                Case "/topic"
                    MetadataContainer = AddElementNode(Document, Document.SelectSingleNode(XMLPath), "prolog", ElementPositions.AfterFirst)
                Case "/bookmap"
                    MetadataContainer = AddElementNode(Document, Document.SelectSingleNode(XMLPath), "bookmeta", ElementPositions.AfterFirst)
                Case Else
                    MetadataContainer = AddElementNode(Document, Document.SelectSingleNode(XMLPath), "topicmeta", ElementPositions.AfterFirst)
            End Select

            ' Add metadata
            AddElementNodeWithText(Document, MetadataContainer, "author", settings.GetAppSetting("author", ""))
            AddElementNodeWithText(Document, MetadataContainer, "source", "https://happenence.co.uk")
            If XMLPath <> "/bookmap" Then
                AddElementNodeWithText(Document, MetadataContainer, "publisher", settings.GetAppSetting("publisher", ""))
                Dim copyright As XmlElement = AddElementNode(Document, MetadataContainer, "copyright")
                Dim copyryear As XmlElement = AddElementNode(Document, copyright, "copyryear")

                AddAttributeNode(copyryear, "year", FirstDate.ToString("yyyy"))
                AddElementNodeWithText(Document, copyright, "copyrholder", settings.GetAppSetting("copyright", ""))
                Dim critdates As XmlElement = AddElementNode(Document, MetadataContainer, "critdates")
                Dim created As XmlElement = AddElementNode(Document, critdates, "created")
                AddAttributeNode(created, "date", FirstDate.ToString("yyyy-MM-dd"))
            End If

        End Sub
        Private Sub AddOtherProps(Element As XmlElement, PostID As Integer, PostType As String, Slug As String)

            Dim cats = From d In dsCategories
                       Where d.ID = PostID
                       Order By d.taxonomy Ascending, d.name Ascending
                       Select d.taxonomy, d.name

            Dim OtherProps As String = ""
            OtherProps &= "topic(" & PostID.ToString & ") post_type(" & PostType & ") slug(" & Slug & ")"

            If cats.Count > 0 Then
                For Each drCategory In cats ' dsCategories.Select("id=" & dr.ID.ToString).
                    ' Build up the otherprops values
                    ' Add in the parent tags
                    Dim close = From c In Closure
                                Where c.Descendant = drCategory.name And c.Taxonomy = drCategory.taxonomy
                                Select c.Ancestor

                    If close.Count > 0 Then
                        Dim lst As String = " "
                        For Each c In close
                            lst &= Replace(c, " ", "-") & " "
                        Next
                        OtherProps &= " " & drCategory.taxonomy & "(" & lst & Replace(drCategory.name, " ", "-") & ")"
                    Else
                        OtherProps &= " " & drCategory.taxonomy & "(" & Replace(drCategory.name, " ", "-") & ")"
                    End If
                Next
                AddAttributeNode(Element, "otherprops", OtherProps)
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
            Dim t As String = node.Text
            If Trim(t) <> vbLf And t <> "" Then


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
            Dim TagMap(,) As String = {
                {"strong", "b"},
                {"em", "i"},
                {"code", "tt"},
                {"samp", "tt"},
                {"var", "tt"},
                {"pre", "tt"},
                {"span", "ph"},
                {"u", "ph"},
                {"caption", "desc"},
                {"tr", "row"},
                {"td", "entry"},
                {"g", "ph"}
                }
            Dim ditaTag As String = ""
            For i = 0 To (TagMap.Length / 2) - 1
                If node.Name = TagMap(i, 0) Then
                    ditaTag = TagMap(i, 1)
                End If
            Next

            If ditaTag = "" Then
                Throw New Exception("Unexpected tag <" & node.Name & "> in ParseTypographic")
            End If

            Dim s As XmlNode
            Select Case ditaParent.Name
                Case "title", "shortdesc", "section", "example", "desc", "p", "note", "lq", "q", "sli", "li", "itemgroup", "dthd", "ddhd", "dt", "dd",
                     "figgroup", "pre", "lines", "ph", "stentry", "draft-comment", "fn", "cite", "xref", "linkinfo", "entry", "prereq", "context", "cmd",
                     "info", "tutorialinfo", "stepxmp", "choice", "choptionhd", "chdeschd", "choption", "chdesc", "stepresult", "result", "postreq", "refsyn",
                     "proptypehd", "propvaluehd", "propdeschd", "proptype", "propvalue", "propdesc", "screen", "b", "u", "i", "tt", "sup", "sub", "codeph", "codeblock",
                     "pt", "pd", "fragref", "synnote", "tbody", "row", "thead"
                    s = AddElementNode(ditaDocument, ditaParent, ditaTag)
                Case Else
                    If ditaParent.HasChildNodes Then
                        Select Case ditaParent.LastChild.Name
                            Case "title", "shortdesc", "section", "example", "desc", "p", "note", "lq",
                                 "q", "sli", "li", "itemgroup", "dthd", "ddhd", "dt", "dd", "figgroup",
                                 "pre", "lines", "ph", "stentry", "draft-comment", "fn", "cite", "xref", "linkinfo",
                                 "entry", "prereq", "context", "cmd", "info", "tutorialinfo", "stepxmp",
                                 "choice", "choptionhd", "chdeschd", "choption", "chdesc", "stepresult", "result",
                                 "postreq", "refsyn", "proptypehd", "propvaluehd", "propdeschd", "proptype", "propvalue",
                                 "propdesc", "screen", "b", "u", "i", "tt", "sup", "sub", "codeph", "codeblock", "pt", "pd", "fragref", "synnote", "bodydiv", "section"
                                s = AddElementNode(ditaDocument, ditaParent.LastChild, ditaTag)
                            Case Else
                                s = AddElementNode(ditaDocument, ditaParent, ditaTag)
                                If settings.GetAppSetting("show-html-error", "false").ToLower = "true" Then Log.AddLogEntry("ParseTypographic-1", LogLevel.Warning, "Invalid " & ditaTag & " tag created from " & node.Name)
                        End Select
                    Else
                        Dim p As XmlElement = AddElementNode(ditaDocument, ditaParent, "bodydiv")
                        s = AddElementNode(ditaDocument, p, ditaTag)
                        If settings.GetAppSetting("show-html-error", "false").ToLower = "true" Then Log.AddLogEntry("ParseTypographic-2", LogLevel.Information, "bodydiv used to wrap " & ditaTag & " tag created from " & node.Name)
                    End If
            End Select
            AddAttributeNode(s, "outputclass", "html-" & node.Name)
            ParseNodes(node.ChildNodes, s, ditaDocument)
        End Sub
        Public Sub ParseAttribute(Attribute As HtmlAttribute, DITADocument As XmlDocument, ditaParent As XmlElement)
            If Not Left(Attribute.Value, 3) = "{{{" And Not Left(Attribute.Value, 2) = "#_" Then
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
                        Dim u As Uri
                        Try
                            u = New Uri(Attribute.Value)
                        Catch ex As Exception
                            Log.AddLogEntry("ParseAttribute-6", LogLevel.Warning, "Invalid URL for image")
                            Exit Select
                        End Try
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
                                            Log.AddLogEntry("ParseAttribute-1", LogLevel.Information, LastErrorPage)
                                        End If
                                        Log.AddLogEntry("ParseAttribute-2", LogLevel.Warning, "Insecure image - " & u.ToString, u.ToString)
                                    End If
                                    If u.Scheme = "http" And u.IsAbsoluteUri Then
                                        u = New Uri(Replace(u.AbsoluteUri, "http://", "https://"))
                                        Log.AddLogEntry("ParseAttribute-4", LogLevel.Information, "Converted image link to https - " & u.ToString, u.ToString)
                                    End If

                                End If

                                Try

                                    locFile = "images/" & Replace(u.Segments(u.Segments.Length - 1), "/", "")
                                    locFile = System.Web.HttpUtility.UrlDecode(locFile)
                                    If Not File.Exists(locFile) And settings.GetAppSetting("FetchImages", "true") = "true" Then
                                        Client.DownloadFile(u.AbsoluteUri, DocSet.OutputDirectory & "/" & locFile)
                                        Log.AddLogEntry("ParseAttribute-5", LogLevel.Information, "Downloaded image" & u.ToString, u.ToString)
                                    End If
                                    internal = True

                                Catch ex As Exception
                                    locFile = "images/404.jpg"
                                    Log.AddLogEntry("ParseAttribute-3", LogLevel.Error, "Unable to download image - " & ex.Message, u.ToString)
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
                        If settings.GetAppSetting("show-skipped", "false").ToLower = "true" Then Log.AddLogEntry("ParseAttribute-4", LogLevel.Information, "   Skipped attribute " & Attribute.Name)
                End Select
            End If
        End Sub
        Public Sub ParseElementNode(node As HtmlNode, DITADocument As XmlDocument, ditaParent As XmlNode)
            Select Case node.Name
                Case "html"
                    ' add a "topic" node and process children
                    Dim s As XmlNode = AddElementNode(DITADocument, ditaParent, "topic")
                    Dim a As XmlAttribute = DITADocument.CreateAttribute("id")
                    a.Value = "topic-" & DocSet.ID.ToString ' This is the ID from the Wordpress database
                    s.Attributes.Append(a)
                    ParseNodes(node.ChildNodes, s, DITADocument)
                Case "head"
                    ' don't add anything but search for title below
                    ParseNodes(node.ChildNodes, ditaParent, DITADocument)

                Case "body"
                    ParseNodes(node.ChildNodes, AddElementNode(DITADocument, ditaParent, "body"), DITADocument)
                Case "li"
                    If ditaParent.Name = "p" Then
                        If settings.GetAppSetting("show-html-errors", "true").ToLower = "true" Then Log.AddLogEntry("ParseElementNode-1", LogLevel.Warning, "li within p in " & DITADocument.SelectSingleNode("/topic/title").InnerText)

                    End If
                    AddNodeAsIs(node, DITADocument, ditaParent)



                ' Output as is
                Case "p", "ul", "ol", "dl", "dt", "title", "div", "cite", "sub", "sup", "tbody", "thead"

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
                Case "strong", "em", "u", "code", "samp", "var", "pre", "tt", "span", "tfoot", "th", "td", "tr", "thead", "caption"
                    ParseTypographic(node, DITADocument, ditaParent)

                Case "g"

                    If settings.GetAppSetting("show-html-errors", "true").ToLower = "true" Then Log.AddLogEntry("ParseElementNode-2", LogLevel.Warning, ditaParent.SelectSingleNode("/topic/title").InnerText & " - Grammarly <g> tag switched to <ph> for " & node.InnerText)
                    ParseTypographic(node, DITADocument, ditaParent)

                'Figures etc
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

                ' Tables
                Case "table"
                    If ditaParent.Name = "fig" And ditaParent.ChildNodes.Count = 0 Then
                        Dim del As XmlElement = ditaParent
                        ditaParent = ditaParent.ParentNode
                        ditaParent.RemoveChild(del)
                    End If
                    Dim table As XmlElement = AddElementNode(DITADocument, ditaParent, "table")
                    Dim tgroup As XmlElement = AddElementNode(DITADocument, table, "tgroup")
                    ParseNodes(node.ChildNodes, tgroup, DITADocument)
                    ' Now check for loose structure and correct
                    Dim ColCount As Integer = 0

                    If Not tgroup.HasAttribute("cols") Then
                        For Each row As XmlElement In table.SelectNodes("//row")
                            Dim cc As Integer = 0
                            For Each e As XmlElement In row.ChildNodes
                                If e.Name = "entry" Then cc += 1
                            Next
                            If cc > ColCount Then ColCount = cc
                        Next
                    End If
                    AddAttributeNode(tgroup, "cols", ColCount.ToString)
                    If settings.GetAppSetting("show-table-generation", "false") = "true" Then
                        Log.AddLogEntry("ParseElementNode-3", LogLevel.Information, "Table generated with " & ColCount.ToString & "columns")
                    End If
                ' Anchor
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
                    AddAttributeNode(s, "placement", "break")
                    For Each a As HtmlAttribute In node.Attributes
                        ParseAttribute(a, DITADocument, s)
                    Next

                Case Else
                    If settings.GetAppSetting("show-skipped", "false").ToLower = "true" Then Log.AddLogEntry("ParseElementNode-4", LogLevel.Information, "Skipped " & node.Name)
            End Select
        End Sub
        Private Function AddNodeAsIs(node As HtmlNode, DITADocument As XmlDocument, ditaParent As XmlNode) As XmlNode
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
            Return s
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
            Text = Replace(Text, vbLf, " ")
            Text = Replace(Text, vbTab, " ")

            'Text = Trim(Text)
            If Text.Length > 0 Then
                Dim n As XmlText
                Select Case Parent.Name
                    Case "alt", "cite", "desc", "dd", "ddhd", "dl", "dlentry", "dlhead", "dt", "dthd", "fig", "figgroup", "image",
                         "keyword", "li", "lines", "lq", "note", "object", "ol", "p", "param", "ph", "pre", "q", "sl", "sli", "ul",
                         "xref", "title", "b", "i", "u", "tt", "sup", "sub", "section", "bodydiv", "mainbooktitle", "booktitlealt",
                         "tbody"
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
            Else
                Return (Nothing)
            End If
        End Function

        Public Class Style
            Property Name As String
            Property Count As Integer = 0
        End Class

        Public Class ClosureEntry
            Property Ancestor As String
            Property Descendant As String
            Property Generations As Integer
            Property Taxonomy As String
        End Class

    End Class
    Public Enum LogLevel
        Information
        Warning
        [Error]
    End Enum
    Public Class LogHandler

        Property OutputDirectory As String
        Private Settings As New ApplicationSettings.Settings.ApplicationSettings("wp2DITA")

        Public Event EventLogged(ByVal Entry As LogEntry)


        Public Sub New()
            OutputDirectory = Settings.GetAppSetting("output-directory", "..\")

        End Sub

        Property LogEntries As New List(Of LogHandler.LogEntry)

        Public Sub AddLogEntry(Source As String, Level As LogLevel, Message As String, Optional URL As String = "")
            Dim LE As New LogEntry With {
                .Source = Source, .Level = Level, .LogMessage = Message, .Time = Now()}
            If URL <> "" Then
                If Left(URL, 4) <> "http" Then
                    URL = "https://" & Settings.GetAppSetting("host") & "/" & URL
                End If
                LE.LogURL = New Uri(URL)
            End If
            LogEntries.Add(LE)
            RaiseEvent EventLogged(LE)
            ' Console.WriteLine(Now.ToString("yyyy-MMM-dd HH:mm:ss:fff") & " " & LE.LogMessage)
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
            Public Property Source As String
            Public Property Level As LogLevel
            Public Property LogMessage As String
            Public Property LogURL As Uri
            Public Property Time As DateTime

        End Class
    End Class
End Namespace
