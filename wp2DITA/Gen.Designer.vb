﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On

Imports System

Namespace My.Resources
    
    'This class was auto-generated by the StronglyTypedResourceBuilder
    'class via a tool like ResGen or Visual Studio.
    'To add or remove a member, edit your .ResX file then rerun ResGen
    'with the /str option, or rebuild your VS project.
    '''<summary>
    '''  A strongly-typed resource class, for looking up localized strings, etc.
    '''</summary>
    <Global.System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0"),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Friend Class Gen
        
        Private Shared resourceMan As Global.System.Resources.ResourceManager
        
        Private Shared resourceCulture As Global.System.Globalization.CultureInfo
        
        <Global.System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")>  _
        Friend Sub New()
            MyBase.New
        End Sub
        
        '''<summary>
        '''  Returns the cached ResourceManager instance used by this class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared ReadOnly Property ResourceManager() As Global.System.Resources.ResourceManager
            Get
                If Object.ReferenceEquals(resourceMan, Nothing) Then
                    Dim temp As Global.System.Resources.ResourceManager = New Global.System.Resources.ResourceManager("wp2DITA.Gen", GetType(Gen).Assembly)
                    resourceMan = temp
                End If
                Return resourceMan
            End Get
        End Property
        
        '''<summary>
        '''  Overrides the current thread's CurrentUICulture property for all
        '''  resource lookups using this strongly typed resource class.
        '''</summary>
        <Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
        Friend Shared Property Culture() As Global.System.Globalization.CultureInfo
            Get
                Return resourceCulture
            End Get
            Set
                resourceCulture = value
            End Set
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SELECT wp_terms.slug, wp_posts.post_type, wp_terms.name, wp_term_taxonomy.taxonomy, wp_posts.post_name, wp_posts.ID, post_date_gmt
        '''FROM   wp_term_relationships INNER JOIN
        '''             wp_term_taxonomy ON wp_term_relationships.term_taxonomy_id = wp_term_taxonomy.term_taxonomy_id INNER JOIN
        '''             wp_terms ON wp_term_taxonomy.term_id = wp_terms.term_id INNER JOIN
        '''             wp_posts ON wp_term_relationships.object_id = wp_posts.ID
        '''WHERE wp_posts.post_status=&apos;publish&apos;
        '''ORDER BY wp_term_taxonomy.ta [rest of string was truncated]&quot;;.
        '''</summary>
        Friend Shared ReadOnly Property sqlCategories() As String
            Get
                Return ResourceManager.GetString("sqlCategories", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SELECT Parent.name AS Ancestor, Child.name AS Descendant, wp_term_taxonomy.taxonomy
        '''FROM   wp_terms Child INNER JOIN
        '''             wp_term_taxonomy ON Child.term_id = wp_term_taxonomy.term_id INNER JOIN
        '''             wp_terms Parent ON wp_term_taxonomy.parent = Parent.term_id.
        '''</summary>
        Friend Shared ReadOnly Property sqlHierarchy() As String
            Get
                Return ResourceManager.GetString("sqlHierarchy", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SELECT ID, post_name, post_date, post_title, post_content, post_type 
        '''FROM wp_posts 
        '''WHERE post_author=1 
        '''and post_status=&apos;publish&apos;
        '''order by post_date asc.
        '''</summary>
        Friend Shared ReadOnly Property sqlPosts() As String
            Get
                Return ResourceManager.GetString("sqlPosts", resourceCulture)
            End Get
        End Property
        
        '''<summary>
        '''  Looks up a localized string similar to SELECT wp_terms.name, wp_terms.slug, parent_terms.name AS Parent_name, parent_terms.slug AS Parent_slug, child_term_taxonomy.description, child_term_taxonomy.taxonomy, parent_term_taxonomy.parent AS Grandparent_ID
        '''FROM   wp_terms parent_terms INNER JOIN
        '''             wp_term_taxonomy parent_term_taxonomy ON parent_terms.term_id = parent_term_taxonomy.term_id RIGHT OUTER JOIN
        '''             wp_terms INNER JOIN
        '''             wp_term_taxonomy child_term_taxonomy ON child_term_taxonomy.term_id = wp_terms.term_i [rest of string was truncated]&quot;;.
        '''</summary>
        Friend Shared ReadOnly Property sqlTaxomony() As String
            Get
                Return ResourceManager.GetString("sqlTaxomony", resourceCulture)
            End Get
        End Property
    End Class
End Namespace