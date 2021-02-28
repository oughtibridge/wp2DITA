<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ctlSettings
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.fbdOpenFolder = New System.Windows.Forms.FolderBrowserDialog()
        Me.tt1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.sfdSaveFile = New System.Windows.Forms.SaveFileDialog()
        Me.SuspendLayout()
        '
        'ctlSettings
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AutoSize = True
        Me.Name = "ctlSettings"
        Me.Size = New System.Drawing.Size(550, 348)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents fbdOpenFolder As FolderBrowserDialog
    Friend WithEvents tt1 As ToolTip
    Friend WithEvents sfdSaveFile As SaveFileDialog
End Class
