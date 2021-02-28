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
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabLog = New System.Windows.Forms.TabPage()
        Me.lvErrors = New System.Windows.Forms.ListView()
        Me.tabSettings = New System.Windows.Forms.TabPage()
        Me.CtlSettings1 = New ApplicationSettingsGUI.ctlSettings()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.lblProgress = New System.Windows.Forms.Label()
        Me.cmdGo = New System.Windows.Forms.Button()
        Me.TabControl1.SuspendLayout()
        Me.TabLog.SuspendLayout()
        Me.tabSettings.SuspendLayout()
        Me.Panel1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabLog)
        Me.TabControl1.Controls.Add(Me.tabSettings)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(1392, 1160)
        Me.TabControl1.TabIndex = 4
        '
        'TabLog
        '
        Me.TabLog.Controls.Add(Me.lvErrors)
        Me.TabLog.Location = New System.Drawing.Point(4, 34)
        Me.TabLog.Name = "TabLog"
        Me.TabLog.Padding = New System.Windows.Forms.Padding(3)
        Me.TabLog.Size = New System.Drawing.Size(1384, 1122)
        Me.TabLog.TabIndex = 1
        Me.TabLog.Text = "Log"
        Me.TabLog.UseVisualStyleBackColor = True
        '
        'lvErrors
        '
        Me.lvErrors.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lvErrors.FullRowSelect = True
        Me.lvErrors.GridLines = True
        Me.lvErrors.HideSelection = False
        Me.lvErrors.Location = New System.Drawing.Point(3, 3)
        Me.lvErrors.MultiSelect = False
        Me.lvErrors.Name = "lvErrors"
        Me.lvErrors.Size = New System.Drawing.Size(1378, 1116)
        Me.lvErrors.TabIndex = 3
        Me.lvErrors.UseCompatibleStateImageBehavior = False
        Me.lvErrors.View = System.Windows.Forms.View.Details
        '
        'tabSettings
        '
        Me.tabSettings.Controls.Add(Me.CtlSettings1)
        Me.tabSettings.Location = New System.Drawing.Point(4, 34)
        Me.tabSettings.Name = "tabSettings"
        Me.tabSettings.Padding = New System.Windows.Forms.Padding(3)
        Me.tabSettings.Size = New System.Drawing.Size(1384, 1122)
        Me.tabSettings.TabIndex = 0
        Me.tabSettings.Text = "Settings"
        Me.tabSettings.UseVisualStyleBackColor = True
        '
        'CtlSettings1
        '
        Me.CtlSettings1.AutoScroll = True
        Me.CtlSettings1.AutoSize = True
        Me.CtlSettings1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.CtlSettings1.Location = New System.Drawing.Point(3, 3)
        Me.CtlSettings1.Name = "CtlSettings1"
        Me.CtlSettings1.Size = New System.Drawing.Size(1378, 1116)
        Me.CtlSettings1.TabIndex = 0
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.lblProgress)
        Me.Panel1.Controls.Add(Me.cmdGo)
        Me.Panel1.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel1.Location = New System.Drawing.Point(0, 1117)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(1392, 43)
        Me.Panel1.TabIndex = 5
        '
        'lblProgress
        '
        Me.lblProgress.AutoSize = True
        Me.lblProgress.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblProgress.Location = New System.Drawing.Point(0, 0)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Size = New System.Drawing.Size(81, 25)
        Me.lblProgress.TabIndex = 3
        Me.lblProgress.Text = "Progress"
        '
        'cmdGo
        '
        Me.cmdGo.Dock = System.Windows.Forms.DockStyle.Right
        Me.cmdGo.Location = New System.Drawing.Point(1194, 0)
        Me.cmdGo.Name = "cmdGo"
        Me.cmdGo.Size = New System.Drawing.Size(198, 43)
        Me.cmdGo.TabIndex = 1
        Me.cmdGo.Text = "Generate"
        Me.cmdGo.UseVisualStyleBackColor = True
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1392, 1160)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "MainForm"
        Me.Text = "Wordpress to DITA Conversion"
        Me.TabControl1.ResumeLayout(False)
        Me.TabLog.ResumeLayout(False)
        Me.tabSettings.ResumeLayout(False)
        Me.tabSettings.PerformLayout()
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabLog As TabPage
    Friend WithEvents lvErrors As ListView
    Friend WithEvents tabSettings As TabPage
    Friend WithEvents Panel1 As Panel
    Friend WithEvents lblProgress As Label
    Friend WithEvents cmdGo As Button
    Friend WithEvents CtlSettings1 As ApplicationSettingsGUI.ctlSettings
End Class
