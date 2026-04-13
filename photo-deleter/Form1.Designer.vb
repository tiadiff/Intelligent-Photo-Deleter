<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
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

    ' Controlli WebView2 per i Video
    Friend WithEvents wvLeft As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents wvRight As Microsoft.Web.WebView2.WinForms.WebView2

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        wvLeft = New Microsoft.Web.WebView2.WinForms.WebView2()
        wvRight = New Microsoft.Web.WebView2.WinForms.WebView2()
        CType(wvLeft, ComponentModel.ISupportInitialize).BeginInit()
        CType(wvRight, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' wvLeft
        ' 
        wvLeft.AllowExternalDrop = True
        wvLeft.CreationProperties = Nothing
        wvLeft.DefaultBackgroundColor = Color.White
        wvLeft.Location = New Point(0, 0)
        wvLeft.Name = "wvLeft"
        wvLeft.Size = New Size(0, 0)
        wvLeft.TabIndex = 0
        wvLeft.ZoomFactor = 1R
        ' 
        ' wvRight
        ' 
        wvRight.AllowExternalDrop = True
        wvRight.CreationProperties = Nothing
        wvRight.DefaultBackgroundColor = Color.White
        wvRight.Location = New Point(0, 0)
        wvRight.Name = "wvRight"
        wvRight.Size = New Size(0, 0)
        wvRight.TabIndex = 0
        wvRight.ZoomFactor = 1R
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(14), CByte(14), CByte(16))
        ClientSize = New Size(1184, 761)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        Name = "Form1"
        SizeGripStyle = SizeGripStyle.Hide
        StartPosition = FormStartPosition.CenterScreen
        Text = "Photo Deleter"
        CType(wvLeft, ComponentModel.ISupportInitialize).EndInit()
        CType(wvRight, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

End Class
