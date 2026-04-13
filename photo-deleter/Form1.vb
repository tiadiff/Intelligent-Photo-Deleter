Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text.Json
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Media.Imaging

Public Class Form1
    ' Controlli UI
    Private WithEvents btnSelectFolder As ModernButton
    Private lblPath As Label
    Private pnlLeft As GlassPanel
    Private pnlRight As GlassPanel
    Private imgLeft As PictureBox
    Private imgRight As PictureBox
    Private lblInfoLeft As Label
    Private lblInfoRight As Label
    Private WithEvents btnDeleteLeft As ModernButton
    Private WithEvents btnDeleteRight As ModernButton
    Private WithEvents btnBack As ModernButton
    Private WithEvents btnNext As ModernButton
    Private lblStatus As Label
    Private prgStatus As ProgressBar
    Private lblDeletedCount As Label
    Private ctxMenu As ContextMenuStrip
    Private WithEvents btnRotateLeft As ModernButton
    Private WithEvents btnRotateRight As ModernButton
    Private WithEvents btnGoTo As ModernButton
    Private WithEvents btnUndo As ModernButton
    Private pbZoom As PictureBox

    ' Dati
    Private photos As New List(Of String)
    Private currentIndex As Integer = 0
    Private currentFolderPath As String = ""
    
    ' Stato per Undo
    Private lastDeletedOriginalPath As String = ""
    Private lastDeletedCurrentPath As String = ""

    Public Sub New()
        ' Chiamata richiesta dal designer.
        InitializeComponent()

        ' Inizializzazione controlli personalizzati
        SetupUI()
    End Sub

    ' Classe per lo stato dell'applicazione
    Public Class AppState
        Public Property LastFolderPath As String
        Public Property LastIndex As Integer
    End Class

    ' Decoder avanzato WIC per HEIC/HEIF
    Private Function LoadWicImage(path As String) As System.Drawing.Bitmap
        ' Usiamo il decoder WIC per leggere il file
        Dim decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
            New Uri(path),
            System.Windows.Media.Imaging.BitmapCreateOptions.None,
            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad)

        Dim frame = decoder.Frames(0)

        ' Convertiamo BitmapSource in System.Drawing.Bitmap via MemoryStream
        Dim encoder As New System.Windows.Media.Imaging.PngBitmapEncoder()
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(frame))

        Using ms As New MemoryStream()
            encoder.Save(ms)
            Return New System.Drawing.Bitmap(ms)
        End Using
    End Function

    Private ReadOnly StateFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appstate.json")

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        CheckResumeState()
    End Sub

    Private Sub SetupUI()
        Me.Text = "Photo Deleter"
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.DoubleBuffered = True
        Me.KeyPreview = True ' Abilita la cattura dei tasti a livello di form

        ' Menu Contestuale
        ctxMenu = New ContextMenuStrip()
        Dim itemShow = ctxMenu.Items.Add("Mostra in Esplora Risorse")
        AddHandler itemShow.Click, AddressOf ContextMenu_ShowInExplorer

        Dim itemOpen = ctxMenu.Items.Add("Apri a pieno schermo")
        AddHandler itemOpen.Click, AddressOf ContextMenu_OpenWithDefault

        ' Top Panel
        Dim topPanel As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = ThemeColors.BgPanel}
        Me.Controls.Add(topPanel)

        ' Top Layout
        Dim topLayout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 3,
            .RowCount = 1,
            .Padding = New Padding(15, 10, 15, 10)
        }
        topLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 220)) ' Folder btn
        topLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))  ' Path lbl
        topLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 240)) ' Undo btn
        topPanel.Controls.Add(topLayout)

        btnSelectFolder = New ModernButton With {
            .Text = "SELEZIONA CARTELLA",
            .Size = New Size(200, 40),
            .Anchor = AnchorStyles.Left,
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.AccentCyan
        }
        topLayout.Controls.Add(btnSelectFolder, 0, 0)

        lblPath = New Label With {
            .Text = "Nessuna cartella selezionata",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .ForeColor = ThemeColors.TextSecondary,
            .Font = New Font("Segoe UI", 9.5F)
        }
        topLayout.Controls.Add(lblPath, 1, 0)

        btnUndo = New ModernButton With {
            .Text = "ANNULLA DELETE",
            .Size = New Size(230, 40),
            .Anchor = AnchorStyles.Right,
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.AccentAmber,
            .Enabled = False
        }
        topLayout.Controls.Add(btnUndo, 2, 0)

        ' Container Centrale
        Dim mainContainer As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Padding = New Padding(10)
        }
        mainContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        mainContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        Me.Controls.Add(mainContainer)

        ' Pannello Sinistro
        pnlLeft = New GlassPanel With {.Dock = DockStyle.Fill, .BackColor = ThemeColors.BgPanel, .Margin = New Padding(10)}
        setupPhotoPanel(pnlLeft, imgLeft, lblInfoLeft, btnDeleteLeft, btnRotateLeft, "SINISTRA")
        mainContainer.Controls.Add(pnlLeft, 0, 0)

        ' Pannello Destro
        pnlRight = New GlassPanel With {.Dock = DockStyle.Fill, .BackColor = ThemeColors.BgPanel, .Margin = New Padding(10)}
        setupPhotoPanel(pnlRight, imgRight, lblInfoRight, btnDeleteRight, btnRotateRight, "DESTRA")
        mainContainer.Controls.Add(pnlRight, 1, 0)

        ' Bottom Panel
        Dim bottomPanel As New Panel With {.Dock = DockStyle.Bottom, .Height = 80, .BackColor = ThemeColors.BgPanel}
        Me.Controls.Add(bottomPanel)
        SetupBottomPanel(bottomPanel)

        ' Setup pbZoom (Inizialmente nascosto)
        pbZoom = New PictureBox With {
            .Size = New Size(300, 300),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .Visible = False,
            .BackColor = Color.Black,
            .BorderStyle = BorderStyle.FixedSingle
        }
        Me.Controls.Add(pbZoom)
        pbZoom.BringToFront()
        
        ' FIX LAYOUT: Il pannello centrale deve reclamare lo spazio RIMANENTE dopo che Top e Bottom si sono ancorati
        mainContainer.BringToFront()
    End Sub

    Private Sub SetupBottomPanel(bottomPanel As Panel)
        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 5,
            .RowCount = 1,
            .Padding = New Padding(15, 0, 15, 0)
        }
        
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 320)) ' Nav
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))  ' Info
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 90))  ' Vai A
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))  ' Progresso
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))  ' Cestinati
        bottomPanel.Controls.Add(layout)

        ' 1. Navigazione
        Dim navBox As New Panel With { .Size = New Size(310, 50), .Anchor = AnchorStyles.Left }
        btnBack = New ModernButton With {
            .Text = "PRECEDENTE",
            .Size = New Size(145, 38),
            .Location = New Point(0, 6),
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.TextPrimary,
            .Enabled = False
        }
        btnNext = New ModernButton With {
            .Text = "SUCCESSIVO",
            .Size = New Size(145, 38),
            .Location = New Point(155, 6),
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.TextPrimary,
            .Enabled = False
        }
        navBox.Controls.Add(btnBack)
        navBox.Controls.Add(btnNext)
        layout.Controls.Add(navBox, 0, 0)

        ' 2. Stato
        lblStatus = New Label With {
            .Text = "Pronto",
            .AutoSize = True,
            .Anchor = AnchorStyles.None,
            .ForeColor = ThemeColors.TextSecondary,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 9.0F)
        }
        layout.Controls.Add(lblStatus, 1, 0)

        ' 3. Vai A
        btnGoTo = New ModernButton With {
            .Text = "VAI A...",
            .Size = New Size(80, 30),
            .Anchor = AnchorStyles.None,
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.AccentCyan,
            .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold)
        }
        layout.Controls.Add(btnGoTo, 2, 0)

        ' 4. Progresso
        prgStatus = New ProgressBar With {
            .Height = 12,
            .Anchor = AnchorStyles.Left Or AnchorStyles.Right,
            .Style = ProgressBarStyle.Continuous,
            .ForeColor = ThemeColors.Success,
            .BackColor = ThemeColors.BgPanel
        }
        layout.Controls.Add(prgStatus, 2, 0)

        ' 5. Cestinati
        lblDeletedCount = New Label With {
            .Text = "Cestinati: 0 (0 MB)",
            .AutoSize = True,
            .Anchor = AnchorStyles.None,
            .ForeColor = ThemeColors.Danger,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        }
        layout.Controls.Add(lblDeletedCount, 4, 0)
    End Sub

    Private Sub setupPhotoPanel(pnl As GlassPanel, ByRef img As PictureBox, ByRef lblInfo As Label, ByRef btnDel As ModernButton, ByRef btnRot As ModernButton, side As String)
        pnl.Padding = New Padding(10)

        ' Immagine
        img = New PictureBox With {
            .Dock = DockStyle.Fill,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackgroundImageLayout = ImageLayout.Stretch,
            .BackColor = Color.Black,
            .ContextMenuStrip = ctxMenu
        }

        ' Assegnazione corretta della WebView
        Dim wv As Microsoft.Web.WebView2.WinForms.WebView2
        If side = "SINISTRA" Then
            wv = wvLeft
        Else
            wv = wvRight
        End If

        wv.Dock = DockStyle.Fill
        wv.Visible = False
        wv.DefaultBackgroundColor = Color.Black

        ' Usiamo un TableLayoutPanel a 4 righe per un controllo totale della geometria e della centratura
        Dim layout As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 4
        }
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 110)) ' Riga 0: Testo info (alzato per visibilità)
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Riga 1: Area Media
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))   ' Riga 2: Tasto Cestino
        layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))   ' Riga 3: Tasto Ruota
        pnl.Controls.Add(layout)

        ' 1. Testo Info (Riga 0)
        lblInfo = New Label With {
            .Dock = DockStyle.Fill,
            .ForeColor = ThemeColors.TextPrimary,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
            .Text = "Nessuna foto",
            .Padding = New Padding(0, 15, 0, 0) ' Spinge il testo in basso per non farlo tagliare
        }
        layout.Controls.Add(lblInfo, 0, 0)

        ' 2. Area Media (Riga 1)
        Dim mediaArea As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .Margin = New Padding(5)
        }
        mediaArea.Controls.Add(img)
        mediaArea.Controls.Add(wv)
        layout.Controls.Add(mediaArea, 0, 1)

        ' 3. Tasto Cestino (Riga 2)
        btnDel = New ModernButton With {
            .Size = New Size(240, 45),
            .Anchor = AnchorStyles.None, ' Anchor None centra l'oggetto nella cella del TableLayoutPanel
            .Text = "SPOSTA NEL CESTINO",
            .BackColor = Color.FromArgb(40, 20, 20),
            .ForeColor = ThemeColors.Danger
        }
        layout.Controls.Add(btnDel, 0, 2)

        ' 4. Tasto Ruota (Riga 3)
        btnRot = New ModernButton With {
            .Size = New Size(120, 30),
            .Anchor = AnchorStyles.None, ' Anche qui, centratura automatica
            .Text = "RUOTA 90°",
            .BackColor = ThemeColors.Surface,
            .ForeColor = ThemeColors.AccentAmber
        }
        layout.Controls.Add(btnRot, 0, 3)

        AddHandler btnRot.Click, Sub() RotatePhoto(side)
        
        ' Handlers per lo Smart Zoom
        AddHandler img.MouseEnter, AddressOf Img_MouseEnter
        AddHandler img.MouseLeave, AddressOf Img_MouseLeave
        AddHandler img.MouseMove, AddressOf Img_MouseMove

        ' Inizializzazione asincrona dei WebView
        InitWebView(wv)
    End Sub

    Private Async Sub InitWebView(wv As Microsoft.Web.WebView2.WinForms.WebView2)
        Try
            Await wv.EnsureCoreWebView2Async(Nothing)
            AddHandler wv.CoreWebView2.ContextMenuRequested, AddressOf WebView_ContextMenuRequested
            ' Forward dei tasti da WebView2 alla Form principale
            AddHandler wv.KeyUp, Sub(s, ke) 
                If ke.KeyCode = Keys.Left OrElse ke.KeyCode = Keys.Right Then
                    Me.OnKeyDown(ke)
                End If
            End Sub
            RefreshVideoMapping()
        Catch ex As Exception
            ' FIX: Avviso se WebView2 non è installato sul PC
            MessageBox.Show("Impossibile inizializzare il motore video. Assicurati di avere WebView2 Runtime installato sul PC.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub RefreshVideoMapping()
        If String.IsNullOrEmpty(currentFolderPath) Then Return

        Try
            If wvLeft.CoreWebView2 IsNot Nothing Then
                wvLeft.CoreWebView2.SetVirtualHostNameToFolderMapping("localmedia", currentFolderPath, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow)
            End If
            If wvRight.CoreWebView2 IsNot Nothing Then
                wvRight.CoreWebView2.SetVirtualHostNameToFolderMapping("localmedia", currentFolderPath, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow)
            End If
        Catch
            ' Silenzioso
        End Try
    End Sub

    Private Sub btnSelectFolder_Click(sender As Object, e As EventArgs) Handles btnSelectFolder.Click
        Using fbd As New FolderBrowserDialog()
            If fbd.ShowDialog() = DialogResult.OK Then
                currentFolderPath = fbd.SelectedPath
                lblPath.Text = currentFolderPath
                LoadPhotos()
            End If
        End Using
    End Sub

    Private Sub LoadPhotos(Optional resetIndex As Boolean = True)
        Try
            ' FIX: Aggiunto formato .gif
            Dim extensions As String() = {".jpg", ".jpeg", ".png", ".gif", ".bmp", ".heic", ".webp", ".mp4", ".mov", ".avi"}
            Dim di As New DirectoryInfo(currentFolderPath)

            Try
                photos = di.GetFiles("*.*") _
                    .Where(Function(f) extensions.Contains(f.Extension.ToLower())) _
                    .OrderBy(Function(f) f.LastWriteTime) _
                    .Select(Function(f) f.FullName) _
                    .ToList()
            Catch ex As UnauthorizedAccessException
                MessageBox.Show("Non hai i permessi per leggere alcuni file in questa cartella.")
            End Try

            RefreshVideoMapping()
            If resetIndex Then currentIndex = 0
            UpdateDisplay()
        Catch ex As Exception
            MessageBox.Show("Errore durante il caricamento dei file: " & ex.Message)
        End Try
    End Sub

    Private Async Sub UpdateDisplay()
        ' --- 1. UX LOCK: Blocchiamo l'UI per prevenire click accidentali o doppi caricamenti (Queue Flooding) ---
        btnBack.Enabled = False
        btnNext.Enabled = False
        btnDeleteLeft.Enabled = False
        btnDeleteRight.Enabled = False
        btnGoTo.Enabled = False
        lblStatus.Text = "Caricamento in corso..."
        
        ' Rilascia il thread UI per un millisecondo per mostrare i pulsanti disabilitati e svuotare le code
        Await Task.Yield()

        If photos.Count = 0 Then
            ClearSlot(imgLeft, wvLeft, lblInfoLeft, btnDeleteLeft)
            ClearSlot(imgRight, wvRight, lblInfoRight, btnDeleteRight)
            lblStatus.Text = "Nessuna foto trovata."
            Return
        End If

        ' Foto Sinistra
        DisplayPhoto(currentIndex, imgLeft, wvLeft, lblInfoLeft, btnDeleteLeft)

        ' Foto Destra
        If currentIndex + 1 < photos.Count Then
            DisplayPhoto(currentIndex + 1, imgRight, wvRight, lblInfoRight, btnDeleteRight)
        Else
            ClearSlot(imgRight, wvRight, lblInfoRight, btnDeleteRight)
        End If

        ' Formattiamo elegantemente i numeri (es. 20.000 anziché 20000)
        Dim currentVisible = (currentIndex + 1).ToString("N0")
        Dim nextVisible = If(currentIndex + 2 > photos.Count, photos.Count, currentIndex + 2).ToString("N0")
        Dim totCount = photos.Count.ToString("N0")
        
        lblStatus.Text = $"Visualizzando {currentVisible} e {nextVisible} di {totCount}"

        ' Aggiornamento barra di progresso
        If photos.Count > 0 Then
            prgStatus.Maximum = photos.Count
            prgStatus.Value = Math.Min(currentIndex + 2, photos.Count)
        Else
            prgStatus.Value = 0
        End If

        ' --- 2. UX UNLOCK: Riabilitiamo i pulsanti base ---
        btnBack.Enabled = (currentIndex > 0)
        btnNext.Enabled = (currentIndex + 2 < photos.Count)
        btnGoTo.Enabled = True
    End Sub

    Private Sub DisplayPhoto(idx As Integer, imgBox As PictureBox, wv As Microsoft.Web.WebView2.WinForms.WebView2, lbl As Label, btn As ModernButton)
        Try
            Dim path As String = photos(idx)
            Dim fi As New FileInfo(path)
            Dim ext = fi.Extension.ToLower()
            Dim isVideo = {".mp4", ".mov", ".avi"}.Contains(ext)

            ' Mostriamo subito le info per debug (Nome, Formato e poi Data)
            lbl.Text = $"NOME: {fi.Name}{vbCrLf}DATA: {fi.LastWriteTime:dd/MM/yyyy HH:mm}"
            btn.Tag = path
            btn.Enabled = True

            ' Associamo il path anche ai controlli media per il menu contestuale
            imgBox.Tag = path
            wv.Tag = path
            If isVideo Then
                imgBox.Visible = False
                wv.Visible = True
                wv.BringToFront()

                If wv.CoreWebView2 IsNot Nothing Then
                    Dim encodedFileName = Uri.EscapeDataString(fi.Name)
                    Dim html = $"<body style='margin:0; background:black; display:flex; justify-content:center; align-items:center; height:100vh; overflow:hidden;'>" &
                               $"<video src='https://localmedia/{encodedFileName}' autoplay loop muted style='max-width:100%; max-height:100%;'></video>" &
                               $"</body>"
                    wv.NavigateToString(html)
                End If
            Else
                Dim loadedSuccessfully = False

                ' 1. Se è HEIC/HEIF, usiamo direttamente il decoder WIC
                If {".heic", ".heif"}.Contains(ext) Then
                    Try
                        imgBox.Image = LoadWicImage(path)
                        loadedSuccessfully = True
                    Catch
                        ' Fallirà se i codec non sono installati
                    End Try
                End If

                ' 2. Se non è HEIC o se WIC ha fallito, proviamo il caricamento standard
                If Not loadedSuccessfully Then
                    Try
                        Using stream As New FileStream(path, FileMode.Open, FileAccess.Read)
                            Using tempImg = Image.FromStream(stream)
                                imgBox.Image = New Bitmap(tempImg)
                            End Using
                        End Using
                        loadedSuccessfully = True
                    Catch
                        ' Fallimento standard
                    End Try
                End If

                If loadedSuccessfully Then
                    wv.Visible = False
                    imgBox.Visible = True
                    imgBox.BringToFront()
                    GenerateAmbilight(imgBox)
                    If wv.CoreWebView2 IsNot Nothing Then wv.NavigateToString("<html><body style='background:black;'></body></html>")
                Else
                    ' 3. FALLBACK ESTREMO: Proviamo con WebView2
                    imgBox.Visible = False
                    wv.Visible = True
                    wv.BringToFront()

                    If wv.CoreWebView2 IsNot Nothing Then
                        Dim encodedFileName = Uri.EscapeDataString(fi.Name)
                        Dim html = $"<body style='margin:0; background:black; display:flex; justify-content:center; align-items:center; height:100vh; overflow:hidden;'>" &
                                   $"<img src='https://localmedia/{encodedFileName}' style='max-width:100%; max-height:100%; object-fit: contain;'></img>" &
                                   $"</body>"
                        wv.NavigateToString(html)
                    Else
                        lbl.Text &= $"{vbCrLf}ERRORE: Motore di rendering non pronto."
                    End If
                End If
            End If

        Catch ex As Exception
            lbl.Text = "Errore critico caricamento: " & IO.Path.GetFileName(photos(idx))
            imgBox.Image = Nothing
            btn.Enabled = False
        End Try
    End Sub

    Private Sub GenerateAmbilight(imgBox As PictureBox)
        If imgBox.Image Is Nothing Then Return
        
        Try
            ' Mappa 7x7 per distruggere le forme (creando solo macchie di colore)
            ' e bordo di 1 pixel vuoto per creare una morbida vignettatura (Fade to Background)
            Dim ambilightBmp As New Bitmap(7, 7)
            Using g As Graphics = Graphics.FromImage(ambilightBmp)
                ' Sfondo predefinito scuro ai bordi
                g.Clear(ThemeColors.BgPanel)
                
                ' L'immagine compressa al centro (5x5)
                g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                g.DrawImage(imgBox.Image, New Rectangle(1, 1, 5, 5))
                
                ' Scuriamo l'alone per farlo sembrare luce diffusa e far risaltare la foto
                Using b As New SolidBrush(Color.FromArgb(170, 0, 0, 0))
                    g.FillRectangle(b, 1, 1, 5, 5)
                End Using
            End Using
            
            If imgBox.BackgroundImage IsNot Nothing Then
                imgBox.BackgroundImage.Dispose()
            End If
            imgBox.BackgroundImage = ambilightBmp
        Catch
            ' Fallback silenzioso se l'immagine è bloccata
        End Try
    End Sub

    Private Sub ClearSlot(imgBox As PictureBox, wv As Microsoft.Web.WebView2.WinForms.WebView2, lbl As Label, btn As ModernButton)
        If imgBox.Image IsNot Nothing Then
            imgBox.Image.Dispose()
            imgBox.Image = Nothing
        End If
        
        If imgBox.BackgroundImage IsNot Nothing Then
            imgBox.BackgroundImage.Dispose()
            imgBox.BackgroundImage = Nothing
        End If

        wv.Visible = False
        If wv.CoreWebView2 IsNot Nothing Then
            wv.NavigateToString("<html><body style='background:black;'></body></html>")
        End If

        lbl.Text = "Slot Vuoto"
        btn.Enabled = False
        btn.Tag = Nothing
    End Sub

    Private Sub btnDeleteLeft_Click(sender As Object, e As EventArgs) Handles btnDeleteLeft.Click
        DeletePhoto(TryCast(btnDeleteLeft.Tag, String))
    End Sub

    Private Sub btnDeleteRight_Click(sender As Object, e As EventArgs) Handles btnDeleteRight.Click
        DeletePhoto(TryCast(btnDeleteRight.Tag, String))
    End Sub

    ' FIX MASSICCIO: Gestione Asincrona per sbloccare i file Video prima dell'eliminazione
    Private Async Sub DeletePhoto(filePath As String)
        If String.IsNullOrEmpty(filePath) Then Return

        Try
            ' 1. Capire se è un video
            Dim isVideo = {".mp4", ".mov", ".avi"}.Contains(IO.Path.GetExtension(filePath).ToLower())

            ' 2. Svuotiamo gli slot prima di spostare, per rilasciare i Lock di Windows (Sia img che video)
            ClearSlot(imgLeft, wvLeft, lblInfoLeft, btnDeleteLeft)
            ClearSlot(imgRight, wvRight, lblInfoRight, btnDeleteRight)

            ' 3. Se era un video, aspettiamo che WebView2 rilasci fisicamente il file
            If isVideo Then
                Await Task.Delay(350)
            End If

            ' 4. Creazione cartella ed esecuzione spostamento
            Dim destDir As String = IO.Path.Combine(currentFolderPath, "deleted_photos")
            If Not Directory.Exists(destDir) Then Directory.CreateDirectory(destDir)

            Dim destPath As String = IO.Path.Combine(destDir, IO.Path.GetFileName(filePath))

            Dim counter As Integer = 1
            While File.Exists(destPath)
                destPath = IO.Path.Combine(destDir, IO.Path.GetFileNameWithoutExtension(filePath) & "_" & counter & IO.Path.GetExtension(filePath))
                counter += 1
            End While

            File.Move(filePath, destPath)

            ' 5. Aggiornamento contatore visivo
            UpdateDeletedInfo(destDir)

            ' Salvataggio stato per Undo
            lastDeletedOriginalPath = filePath
            lastDeletedCurrentPath = destPath
            If btnUndo IsNot Nothing Then btnUndo.Enabled = True

            ' 6. Aggiornamento lista in memoria
            photos.Remove(filePath)

            ' 6. Ricalcolo dell'indice per evitare di sfondare l'array
            If currentIndex >= photos.Count AndAlso currentIndex > 0 Then
                currentIndex = Math.Max(0, currentIndex - 2)
            End If

            UpdateDisplay()

        Catch ex As IOException
            MessageBox.Show("Il file è ancora in uso dal sistema. Riprova tra un istante.", "File Bloccato", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            UpdateDisplay() ' Ripristina la view
        Catch ex As Exception
            MessageBox.Show("Errore durante lo spostamento: " & ex.Message)
            UpdateDisplay()
        End Try
    End Sub

    Private Sub btnGoTo_Click(sender As Object, e As EventArgs) Handles btnGoTo.Click
        If photos.Count = 0 Then Return

        Dim input = InputBox($"Inserisci il numero della foto (1-{photos.Count}):", "Vai alla Foto", (currentIndex + 1).ToString())
        
        If String.IsNullOrWhiteSpace(input) Then Return

        Dim target As Integer
        If Integer.TryParse(input, target) Then
            If target >= 1 AndAlso target <= photos.Count Then
                ' Calcolo dell'indice per mantenere l'allineamento alle coppie
                ' Se scelgo foto 5, deve mostrarmi 5 e 6 (indice 4).
                ' Se scelgo foto 6, deve comunque portarmi alla coppia (5, 6) -> indice 4.
                currentIndex = ((target - 1) \ 2) * 2
                UpdateDisplay()
            Else
                MessageBox.Show($"Inserisci un numero valido tra 1 e {photos.Count}.", "Input non valido", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        Else
            MessageBox.Show("Inserisci un numero intero valido.", "Errore Input", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Private Sub btnBack_Click(sender As Object, e As EventArgs) Handles btnBack.Click
        If currentIndex > 0 Then
            ' FIX: Math.Max impedisce che currentIndex vada a -1 se era a 1
            currentIndex = Math.Max(0, currentIndex - 2)
            UpdateDisplay()
        End If
    End Sub

    Private Sub btnNext_Click(sender As Object, e As EventArgs) Handles btnNext.Click
        If currentIndex + 2 < photos.Count Then
            currentIndex += 2
            UpdateDisplay()
        End If
    End Sub

    Private Sub UndoLastDelete()
        If String.IsNullOrEmpty(lastDeletedOriginalPath) OrElse String.IsNullOrEmpty(lastDeletedCurrentPath) Then
            Return
        End If
        If Not File.Exists(lastDeletedCurrentPath) Then Return

        Try
            ' Riporta il file nella cartella originale
            File.Move(lastDeletedCurrentPath, lastDeletedOriginalPath)
            
            ' Azzera stato undo
            lastDeletedOriginalPath = ""
            lastDeletedCurrentPath = ""
            If btnUndo IsNot Nothing Then btnUndo.Enabled = False

            ' Ricarica la lista per includere nuovamente la foto (mantenendo l'ordinamento)
            LoadPhotos(False)
            
            ' Aggiorna i cestinati
            Dim destDir As String = IO.Path.Combine(currentFolderPath, "deleted_photos")
            UpdateDeletedInfo(destDir)
            
            MessageBox.Show("Ultima foto eliminata ripristinata con successo.", "Annulla completato", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("Errore durante il ripristino: " & ex.Message)
        End Try
    End Sub

    Private Sub btnUndo_Click(sender As Object, e As EventArgs) Handles btnUndo.Click
        UndoLastDelete()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        ' Gestione (Ctrl+Z)
        If keyData = (Keys.Control Or Keys.Z) Then
            UndoLastDelete()
            Return True
        End If

        ' Usiamo Keys.Left e Keys.Right direttamente (senza modificatori)
        Dim key = keyData And Not Keys.Modifiers
        
        If key = Keys.Left Then
            If btnBack IsNot Nothing AndAlso btnBack.Enabled Then
                btnBack.PerformClick()
                Return True
            End If
        ElseIf key = Keys.Right Then
            If btnNext IsNot Nothing AndAlso btnNext.Enabled Then
                btnNext.PerformClick()
                Return True
            End If
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' --- SMART ZOOM ---
    Private Sub Img_MouseEnter(sender As Object, e As EventArgs)
        If pbZoom IsNot Nothing Then
            pbZoom.Visible = True
            pbZoom.BringToFront()
        End If
    End Sub

    Private Sub Img_MouseLeave(sender As Object, e As EventArgs)
        If pbZoom IsNot Nothing Then
            pbZoom.Visible = False
        End If
    End Sub

    Private Sub Img_MouseMove(sender As Object, e As MouseEventArgs)
        Dim picBox As PictureBox = TryCast(sender, PictureBox)
        If picBox Is Nothing OrElse picBox.Image Is Nothing OrElse pbZoom Is Nothing OrElse Not pbZoom.Visible Then Return

        ' Posizionamento della lente
        Dim ptOnForm = picBox.PointToScreen(e.Location)
        Dim ptRel = Me.PointToClient(ptOnForm)
        
        Dim targetX = ptRel.X + 20
        Dim targetY = ptRel.Y + 20
        If targetX + pbZoom.Width > Me.ClientSize.Width Then targetX = ptRel.X - pbZoom.Width - 10
        If targetY + pbZoom.Height > Me.ClientSize.Height Then targetY = ptRel.Y - pbZoom.Height - 10
        pbZoom.Location = New Point(targetX, targetY)

        Dim img = picBox.Image
        ' Calcolo coordinate relative al SizeMode.Zoom
        Dim scaleW As Double = picBox.Width / img.Width
        Dim scaleH As Double = picBox.Height / img.Height
        Dim scale As Double = Math.Min(scaleW, scaleH)
        
        Dim dispW As Double = img.Width * scale
        Dim dispH As Double = img.Height * scale
        Dim offX As Double = (picBox.Width - dispW) / 2
        Dim offY As Double = (picBox.Height - dispH) / 2
        
        If e.X < offX OrElse e.X > offX + dispW OrElse e.Y < offY OrElse e.Y > offY + dispH Then
            ' Mouse fuori dall'immagine reale (ma dentro il padding nero)
            If pbZoom.Image IsNot Nothing Then
                Using g = Graphics.FromImage(pbZoom.Image)
                    g.Clear(Color.Black)
                End Using
                pbZoom.Invalidate()
            End If
            Return
        End If
        
        Dim origX As Double = (e.X - offX) / scale
        Dim origY As Double = (e.Y - offY) / scale
        
        ' L'area da prelevare dalla foto originale
        Dim zoomW = 300
        Dim zoomH = 300
        Dim rectX = CInt(Math.Max(0, origX - zoomW / 2))
        Dim rectY = CInt(Math.Max(0, origY - zoomH / 2))
        
        If rectX + zoomW > img.Width Then rectX = Math.Max(0, img.Width - zoomW)
        If rectY + zoomH > img.Height Then rectY = Math.Max(0, img.Height - zoomH)
        
        Dim srcRect As New Rectangle(rectX, rectY, Math.Min(zoomW, img.Width), Math.Min(zoomH, img.Height))
        If srcRect.Width <= 0 OrElse srcRect.Height <= 0 Then Return

        If pbZoom.Image Is Nothing OrElse pbZoom.Image.Width <> zoomW Then
            pbZoom.Image = New Bitmap(zoomW, zoomH)
        End If

        Try
            ' Estrazione veloce e rendering
            Using g As Graphics = Graphics.FromImage(pbZoom.Image)
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear
                g.Clear(Color.Black)
                g.DrawImage(img, New Rectangle(0, 0, zoomW, zoomH), srcRect, GraphicsUnit.Pixel)
            End Using
            pbZoom.Invalidate()
        Catch ex As Exception
            ' Ignora errori concorrenziali GDI+
        End Try
    End Sub

    Private Sub RotatePhoto(side As String)
        Dim path As String = ""
        Dim imgBox As PictureBox = Nothing

        If side = "SINISTRA" Then
            path = TryCast(btnDeleteLeft.Tag, String)
            imgBox = imgLeft
        Else
            path = TryCast(btnDeleteRight.Tag, String)
            imgBox = imgRight
        End If

        If String.IsNullOrEmpty(path) OrElse Not File.Exists(path) Then Return

        Try
            ' 1. Svuotiamo la picture box per sbloccare il file se necessario (anche se usiamo New Bitmap)
            If imgBox.Image IsNot Nothing Then
                imgBox.Image.Dispose()
                imgBox.Image = Nothing
            End If

            ' 2. Caricamento e rotazione
            Dim ext = IO.Path.GetExtension(path).ToLower()
            If {".heic", ".heif"}.Contains(ext) Then
                ' Rotazione HEIC via WIC
                Dim decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(New Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad)
                Dim frame = decoder.Frames(0)

                Dim rotated As New System.Windows.Media.Imaging.TransformedBitmap(frame, New System.Windows.Media.RotateTransform(90))

                Dim encoder As New System.Windows.Media.Imaging.PngBitmapEncoder() ' Salviamo come PNG ruotato per compatibilità
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rotated))

                Using ms As New FileStream(path, FileMode.Create, FileAccess.Write)
                    encoder.Save(ms)
                End Using
            Else
                ' Rotazione Standard GDI+
                Using stream As New FileStream(path, FileMode.Open, FileAccess.ReadWrite)
                    Using img As Image = Image.FromStream(stream)
                        img.RotateFlip(RotateFlipType.Rotate90FlipNone)
                        ' Per salvare sovrascrivendo, dobbiamo chiudere e riaprire o usare un temp
                    End Using
                End Using

                ' Metodo più sicuro per GDI+: Carica -> Ruota -> Salva Temp -> Sostituisci
                Dim tempPath = path & ".tmp"
                Using img As Image = Image.FromFile(path)
                    img.RotateFlip(RotateFlipType.Rotate90FlipNone)
                    img.Save(tempPath, img.RawFormat)
                End Using
                File.Delete(path)
                File.Move(tempPath, path)
            End If

            ' 3. Ricarica la visualizzazione
            UpdateDisplay()

        Catch ex As Exception
            MessageBox.Show("Errore durante la rotazione: " & ex.Message)
            UpdateDisplay()
        End Try
    End Sub

    ' --- AGGIORNAMENTO INFO CESTINATI ---

    Private Sub UpdateDeletedInfo(destDir As String)
        If Directory.Exists(destDir) Then
            Dim files = Directory.GetFiles(destDir)
            Dim totalBytes As Long = 0
            For Each f In files
                totalBytes += New FileInfo(f).Length
            Next
            lblDeletedCount.Text = $"Cestinati: {files.Length} ({FormatSize(totalBytes)})"
        Else
            lblDeletedCount.Text = "Cestinati: 0 (0 MB)"
        End If
    End Sub

    Private Function FormatSize(bytes As Long) As String
        Dim kb As Double = bytes / 1024
        Dim mb As Double = kb / 1024
        Dim gb As Double = mb / 1024

        If gb >= 1 Then
            Return $"{gb:F2} GB"
        ElseIf mb >= 1 Then
            Return $"{mb:F2} MB"
        ElseIf kb >= 1 Then
            Return $"{kb:F2} KB"
        Else
            Return $"{bytes} B"
        End If
    End Function

    ' --- AGGIORNAMENTO LISTA FOTO ---

    Private Sub ContextMenu_ShowInExplorer(sender As Object, e As EventArgs)
        Dim item = TryCast(sender, ToolStripItem)
        If item IsNot Nothing Then
            Dim menu = TryCast(item.Owner, ContextMenuStrip)
            If menu IsNot Nothing Then
                Dim ctrl = menu.SourceControl
                If ctrl IsNot Nothing AndAlso ctrl.Tag IsNot Nothing Then
                    ShowInExplorer(ctrl.Tag.ToString())
                End If
            End If
        End If
    End Sub

    Private Sub WebView_ContextMenuRequested(sender As Object, e As Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuRequestedEventArgs)
        ' Rimuoviamo i menu standard di Edge
        e.MenuItems.Clear()

        ' Aggiungiamo i nostri
        Dim wv = DirectCast(sender, Microsoft.Web.WebView2.Core.CoreWebView2)
        Dim path As String = ""
        If wvLeft.CoreWebView2 Is wv Then path = TryCast(wvLeft.Tag, String)
        If wvRight.CoreWebView2 Is wv Then path = TryCast(wvRight.Tag, String)

        If Not String.IsNullOrEmpty(path) Then
            ' 1. Mostra in Explorer
            Dim itemShow = wv.Environment.CreateContextMenuItem("Mostra in Esplora Risorse", Nothing, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuItemKind.Command)
            AddHandler itemShow.CustomItemSelected, Sub() ShowInExplorer(path)
            e.MenuItems.Add(itemShow)

            ' 2. Apri con visualizzatore
            Dim itemOpen = wv.Environment.CreateContextMenuItem("Apri con visualizzatore predefinito", Nothing, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuItemKind.Command)
            AddHandler itemOpen.CustomItemSelected, Sub() OpenWithDefaultViewer(path)
            e.MenuItems.Add(itemOpen)
        End If
    End Sub

    Private Sub ContextMenu_OpenWithDefault(sender As Object, e As EventArgs)
        Dim item = TryCast(sender, ToolStripItem)
        If item IsNot Nothing Then
            Dim menu = TryCast(item.Owner, ContextMenuStrip)
            If menu IsNot Nothing Then
                Dim ctrl = menu.SourceControl
                If ctrl IsNot Nothing AndAlso ctrl.Tag IsNot Nothing Then
                    OpenWithDefaultViewer(ctrl.Tag.ToString())
                End If
            End If
        End If
    End Sub

    Private Sub OpenWithDefaultViewer(filePath As String)
        If File.Exists(filePath) Then
            Try
                Process.Start(New ProcessStartInfo(filePath) With {.UseShellExecute = True})
            Catch ex As Exception
                MessageBox.Show("Impossibile aprire il file: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub ShowInExplorer(filePath As String)
        If File.Exists(filePath) Then
            Process.Start("explorer.exe", $"/select,""{filePath}""")
        End If
    End Sub

    ' --- GESTIONE STATO (SAVE/RESUME) ---

    Private Sub CheckResumeState()
        If Not File.Exists(StateFilePath) Then Return

        Try
            Dim json = File.ReadAllText(StateFilePath)
            Dim state = JsonSerializer.Deserialize(Of AppState)(json)

            If state IsNot Nothing AndAlso Directory.Exists(state.LastFolderPath) Then
                Dim msg = $"Vuoi riprendere da dove avevi lasciato?{vbCrLf}{vbCrLf}" &
                          $"Cartella: {state.LastFolderPath}{vbCrLf}" &
                          $"Foto numero: {state.LastIndex + 1}"

                If MessageBox.Show(msg, "Riprendi sessione", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    currentFolderPath = state.LastFolderPath
                    lblPath.Text = currentFolderPath
                    LoadPhotos(False)

                    If state.LastIndex < photos.Count Then
                        currentIndex = state.LastIndex
                        If currentIndex Mod 2 <> 0 Then currentIndex -= 1
                    End If

                    UpdateDisplay()
                End If
            End If

            ' Aggiorniamo il contatore dei cestinati esistenti
            Dim destDir As String = IO.Path.Combine(state.LastFolderPath, "deleted_photos")
            UpdateDeletedInfo(destDir)
        Catch ex As Exception
            ' Fallimento silenzioso
        End Try
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)

        Try
            ' FIX: Pulizia memoria WebView2 alla chiusura
            If wvLeft IsNot Nothing Then wvLeft.Dispose()
            If wvRight IsNot Nothing Then wvRight.Dispose()

            If Not String.IsNullOrEmpty(currentFolderPath) Then
                Dim state As New AppState With {
                    .LastFolderPath = currentFolderPath,
                    .LastIndex = currentIndex
                }
                Dim json = JsonSerializer.Serialize(state, New JsonSerializerOptions With {.WriteIndented = True})
                File.WriteAllText(StateFilePath, json)
            End If
        Catch
            ' Fallimento silenzioso
        End Try
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub
End Class