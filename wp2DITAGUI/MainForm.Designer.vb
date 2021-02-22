<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.cmdGo = New System.Windows.Forms.Button()
        Me.lvErrors = New System.Windows.Forms.ListView()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.CtlSettings1 = New wp2DITAGUI.ctlSettings()
        Me.SuspendLayout()
        '
        'cmdGo
        '
        Me.cmdGo.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.cmdGo.Location = New System.Drawing.Point(0, 1126)
        Me.cmdGo.Name = "cmdGo"
        Me.cmdGo.Size = New System.Drawing.Size(1392, 34)
        Me.cmdGo.TabIndex = 0
        Me.cmdGo.Text = "Generate"
        Me.cmdGo.UseVisualStyleBackColor = True
        '
        'lvErrors
        '
        Me.lvErrors.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lvErrors.HideSelection = False
        Me.lvErrors.Location = New System.Drawing.Point(0, 735)
        Me.lvErrors.Name = "lvErrors"
        Me.lvErrors.Size = New System.Drawing.Size(1392, 391)
        Me.lvErrors.TabIndex = 1
        Me.lvErrors.UseCompatibleStateImageBehavior = False
        Me.lvErrors.View = System.Windows.Forms.View.List
        Me.lvErrors.Visible = False
        '
        'lblProgress
        '
        Me.lblProgress.AutoSize = True
        Me.lblProgress.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblProgress.Location = New System.Drawing.Point(0, 0)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(81, 25)
        Me.lblProgress.TabIndex = 2
        Me.lblProgress.Text = "Progress"
        '
        'CtlSettings1
        '
        Me.CtlSettings1.AutoScroll = True
        Me.CtlSettings1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.CtlSettings1.Location = New System.Drawing.Point(0, 25)
        Me.CtlSettings1.Name = "CtlSettings1"
        Me.CtlSettings1.Size = New System.Drawing.Size(1392, 710)
        Me.CtlSettings1.TabIndex = 3
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1392, 1160)
        Me.Controls.Add(Me.CtlSettings1)
        Me.Controls.Add(Me.lvErrors)
        Me.Controls.Add(Me.cmdGo)
        Me.Controls.Add(Me.lblProgress)
        Me.Name = "MainForm"
        Me.Text = "Wordpress to DITA Conversion"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents cmdGo As Button
    Friend WithEvents lvErrors As ListView
    Friend WithEvents lblProgress As Label
    Friend WithEvents CtlSettings1 As ctlSettings
End Class
