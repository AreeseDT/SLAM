Imports System.ComponentModel
Imports System.IO
Imports System.Text.RegularExpressions
Imports VideoLibrary
Imports NReco.VideoConverter

Public Class YTImport

    Public VideoFile As String

    Sub ProgressChangedHandler(sender, args)
        Console.WriteLine(args.ProgressPercentage)
    End Sub

    Private Sub ImportButton_Click(sender As Object, e As EventArgs) Handles ImportButton.Click
        Dim youtubeMatch As Match = New Regex("youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(TextBox1.Text)

        If youtubeMatch.Success Then
            TextBox1.Enabled = False
            ImportButton.Enabled = False
            DownloadWorker.RunWorkerAsync("youtube.com/watch?v=" & youtubeMatch.Groups(1).Value)
            ToolStripStatusLabel1.Text = "Status: Downloading"
        Else
            MessageBox.Show("Invalid YouTube URL.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            TextBox1.Enabled = True
            ImportButton.Enabled = True
        End If
    End Sub

    Private Sub DownloadWorker_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles DownloadWorker.DoWork
        Try
            If Not Directory.Exists(Path.GetFullPath("temp\")) Then
                Directory.CreateDirectory(Path.GetFullPath("temp\"))
            End If

            'Use the highest audio quality download
            Dim video = YouTube.Default.GetAllVideos(e.Argument).Where(Function(v) Not v.AudioFormat = AudioFormat.Unknown).OrderByDescending(Function(v) v.AudioBitrate).First()

            Dim videoDownloadFile As String = Path.GetFullPath("temp\" & String.Join("", video.Title.Split(Path.GetInvalidFileNameChars())) & video.FileExtension)

            File.WriteAllBytes(videoDownloadFile, video.GetBytes())

            'Some of the downloads are in .webm format, this converts them
            If video.FileExtension IsNot ".mp4" Then
                Dim convert As New FFMpegConverter()
                Dim convertedVideoFile As String = Path.GetFullPath("temp\" & String.Join("", video.Title.Split(Path.GetInvalidFileNameChars())) & ".mp4")
                convert.ConvertMedia(videoDownloadFile, convertedVideoFile, Format.mp4)
                videoDownloadFile = convertedVideoFile
            End If

            e.Result = videoDownloadFile
        Catch ex As Exception
            Form1.LogError(ex)

            e.Result = ex
        End Try

    End Sub

    Private Sub DownloadWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs) Handles DownloadWorker.ProgressChanged
        ToolStripProgressBar1.Value = e.ProgressPercentage
    End Sub

    Private Sub DownloadWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs) Handles DownloadWorker.RunWorkerCompleted
        If e.Result.GetType = GetType(Exception) Then
            MessageBox.Show(e.Result.Message & " See errorlog.txt for more info.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Else
            VideoFile = e.Result
            DialogResult = Windows.Forms.DialogResult.OK
        End If
    End Sub

    Private Sub DonateLabel_Click(sender As Object, e As EventArgs) Handles DonateLabel.Click
        Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=RVLLPGWJUG6CY")
    End Sub

    Private Sub YTImport_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox1.Select()
    End Sub
End Class