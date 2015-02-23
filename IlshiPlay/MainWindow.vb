Imports NAudio.Wave
Imports System.Threading
Imports System.Runtime.InteropServices
Imports System.Xml

Public Class MainWindow
	Inherits MetroFramework.Forms.MetroForm

	Private version As String = "1.4.0 (19/02/2015)"

	' TODO Mettre les infos dans un Singleton

#Region "Property"

	Private output As DirectSoundOut = Nothing
	Private userOutput As DirectSoundOut = Nothing

	Const PRESSED_KEY As Integer = -32767

	Public Shared ReadOnly ALLOWED_KEYS As New List(Of String) _
		({"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"})
	Public Shared ReadOnly TOGGLE_KEYS As New Dictionary(Of Integer, String) From {
		{13, "ENTER"}
	}

	Private Declare Function GetAsyncKeyState Lib "user32" (ByVal vkey As Long) As Short

	Private WithEvents getKeyTimer As New System.Windows.Forms.Timer

	Private toggleKey As Integer? = Nothing

	Private handlingToggleKey As Boolean = False

	Private toggleKeyLocked As Boolean = False

#End Region

#Region "New"

	Public Sub New()
		InitializeComponent()
		Me.RefreshMicrophones()
		Me.cbMicrophone.DropDownStyle = ComboBoxStyle.DropDownList
		Me.KeyPreview = True

		getKeyTimer.Enabled = True
		getKeyTimer.Interval = 1

		Me.RefreshVolume()
		Me.RefreshHandlingToggleKey()
	End Sub

#End Region

#Region "Event"

	Private Sub linkAbout_Click(sender As Object, e As EventArgs) Handles linkAbout.Click
		Dim aboutContent As String = vbNewLine & _
			"Last version :" & vbNewLine & _
			 Version & vbNewLine & vbNewLine & _
			"by Ilshidur"
		MetroFramework.MetroMessageBox.Show(Me, aboutContent, "About " & My.Application.Info.AssemblyName & " ...", MessageBoxButtons.OK, MessageBoxIcon.Information)
	End Sub

	Private Sub cbMicrophone_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbMicrophone.SelectedIndexChanged

	End Sub

	Private Sub tbVolume_Scroll(sender As Object, e As ScrollEventArgs) Handles tbVolume.Scroll
		Me.RefreshVolume()
	End Sub

	Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
		Dim xmlDoc As XDocument = _
			<?xml version="1.0" encoding="utf-8"?>
			<IlshiPlay_Save>
				<SoundList>
					<%= _
						From el In Me.dgvSounds.Rows
						Where CType(el, DataGridViewRow).IsNewRow = False
						Select _
						<sound>
							<soundPath><%= CType(CType(el, DataGridViewRow).Cells(0), DataGridViewTextBoxCell).Value() %></soundPath>
							<key><%= CType(CType(el, DataGridViewRow).Cells(1), DataGridViewComboBoxCell).Value() %></key>
						</sound>
					%>
				</SoundList>
			</IlshiPlay_Save>
		Dim saveWindow As New SaveFileDialog
		saveWindow.AddExtension = True
		saveWindow.CheckPathExists = True
		saveWindow.Filter = "XML Files (*.xml)|*.xml"
		saveWindow.OverwritePrompt = True
		saveWindow.ValidateNames = True
		If saveWindow.ShowDialog() = Windows.Forms.DialogResult.OK Then
			Try
				xmlDoc.Save(saveWindow.FileName)
				MetroFramework.MetroMessageBox.Show(Me, "The file has been saved successfully !", "Saving", MessageBoxButtons.OK, MessageBoxIcon.Information)
			Catch ex As Exception
				MetroFramework.MetroMessageBox.Show(Me, "Error when saving the file :" & vbNewLine & ex.ToString, "Saving", MessageBoxButtons.OK, MessageBoxIcon.Error)
			End Try
		End If
	End Sub

	Private Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
		Dim openWindow As New OpenFileDialog
		openWindow.AddExtension = True
		openWindow.CheckFileExists = True
		openWindow.CheckPathExists = True
		openWindow.Filter = "XML Files (*.xml)|*.xml"
		openWindow.Multiselect = False
		openWindow.ValidateNames = True
		If openWindow.ShowDialog() = Windows.Forms.DialogResult.OK Then
			Try
				Dim xmlDoc As XDocument = XDocument.Load(openWindow.FileName)
				Me.dgvSounds.Rows.Clear()
				For Each sound As XElement In xmlDoc.<IlshiPlay_Save>.<SoundList>.<sound>
					Me.dgvSounds.Rows.Add(sound.<soundPath>.Value, sound.<key>.Value)
				Next
				MetroFramework.MetroMessageBox.Show(Me, "The file has been loaded successfully !", "Loading", MessageBoxButtons.OK, MessageBoxIcon.Information)
			Catch ex As Exception
				MetroFramework.MetroMessageBox.Show(Me, "Error when loading the file :" & vbNewLine & ex.ToString, "Loading", MessageBoxButtons.OK, MessageBoxIcon.Error)
			End Try
		End If
	End Sub

	Private Sub dgvSounds_CellMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgvSounds.CellMouseClick
		If Me.dgvSounds.CurrentRow.IsNewRow = True Then
			If Me.dgvSounds.Rows.Count < ALLOWED_KEYS.Count Then
				Dim loadingWindows As New OpenFileDialog()
				loadingWindows.Filter = "WAV Files (*.wav) | *.wav"
				Dim chemin As String = ""
				If loadingWindows.ShowDialog = Windows.Forms.DialogResult.OK Then
					chemin = loadingWindows.FileName
					Me.dgvSounds.Rows.Add(chemin, ALLOWED_KEYS.Where(Function(k) AscW(k) = Me.GetNonUsedKeys().First).First)
				End If
			Else
				MetroFramework.MetroMessageBox.Show(Me, "You can't add more than " & ALLOWED_KEYS.Count & " sounds.", My.Application.Info.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
			End If
		End If
	End Sub

	Private Sub dgvSounds_EditingControlShowing(sender As Object, e As DataGridViewEditingControlShowingEventArgs) Handles dgvSounds.EditingControlShowing
		If dgvSounds.CurrentCell.ColumnIndex = 1 Then
			Dim cell As ComboBox = CType(e.Control, ComboBox)
			AddHandler cell.SelectedIndexChanged, AddressOf dgvSounds_KeyColumnSelectedIndexChanged
			AddHandler cell.KeyPress, AddressOf dgvSounds_KeyColumnKeyPress
		End If
	End Sub

	' Deny the change by hitting a key in the combobox
	Private Sub dgvSounds_KeyColumnKeyPress(sender As Object, e As KeyPressEventArgs)
		If Me.dgvSounds.CurrentCell.ColumnIndex = 1 Then
			e.Handled = True
		End If
	End Sub

	Private Sub dgvSounds_KeyColumnSelectedIndexChanged(sender As Object, e As EventArgs)
		Dim cell As ComboBox = CType(sender, ComboBox)
		Me.dgvSounds.CurrentCell.Value = Nothing ' Reset the keys => Update 'Me.GetUsedKeys()'
		If Me.GetUsedKeys().Contains(GetKeyCode(CStr(cell.SelectedItem))) Then
			MetroFramework.MetroMessageBox.Show(Me, "Cette touche est déjà prise.", My.Application.Info.AssemblyName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
			cell.SelectedItem = Me.GetNonUsedKeys()(0)
		End If
		Me.dgvSounds.CurrentCell.Value = cell.SelectedItem
	End Sub

	''' <summary>
	''' Triggers on all ticks in order to check the input keys
	''' </summary>
	''' <param name="sender"></param>
	''' <param name="e"></param>
	''' <remarks></remarks>
	Private Sub getKeyTimer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles getKeyTimer.Tick
		If Me.handlingToggleKey = False Then
			For Each pair As KeyValuePair(Of Integer, String) In MainWindow.TOGGLE_KEYS
				If GetAsyncKeyState(pair.Key) = PRESSED_KEY And pair.Key = toggleKey Then
					toggleKeyLocked = Not toggleKeyLocked
					RefreshToggleKeyButton()
				End If
			Next
			If toggleKeyLocked = True Then Exit Sub
			For Each key As Integer In Me.GetUsedKeys()
				' The key must be in the supported input keys
				If GetAsyncKeyState(key) = PRESSED_KEY Then
					If toggleKey Is Nothing Or toggleKey <> key Then
						Me.Play(Me.GetSoundFromKey(key))
					End If
				End If

				If GetAsyncKeyState(Keys.Escape) = PRESSED_KEY Then
					Me.StopPlay()
				End If
			Next
		Else
			For Each pair As KeyValuePair(Of Integer, String) In MainWindow.TOGGLE_KEYS
				If GetAsyncKeyState(pair.Key) = PRESSED_KEY Then
					Me.toggleKey = pair.Key
					Me.handlingToggleKey = False
					Me.RefreshHandlingToggleKey()
				End If
			Next
		End If
	End Sub

	Private Sub bgwPlayer_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles bgwPlayer.DoWork
		While Me.output IsNot Nothing AndAlso Me.output.PlaybackState <> PlaybackState.Stopped
			Thread.Sleep(20)
		End While
		While Me.userOutput IsNot Nothing AndAlso Me.userOutput.PlaybackState <> PlaybackState.Stopped
			Thread.Sleep(20)
		End While
	End Sub
	Private Sub bgwPlayer_RunWorkerCompleted(sender As Object, e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bgwPlayer.RunWorkerCompleted
		Me.StopPlay()
		Me.lblPlayingSong.Visible = False
		Me.Refresh()
	End Sub

	Private Sub btnToggleKey_Click(sender As Object, e As EventArgs) Handles btnToggleKey.Click
		If Me.handlingToggleKey = False Then
			If Me.toggleKey Is Nothing Then
				Me.handlingToggleKey = True
			Else
				Me.toggleKey = Nothing
			End If
		Else
			Me.handlingToggleKey = False
		End If
		Me.toggleKeyLocked = False
		Me.RefreshHandlingToggleKey()
		Me.RefreshToggleKeyButton()
	End Sub

#End Region

#Region "Function"

	Private Sub RefreshMicrophones()
		Me.cbMicrophone.DisplayMember = "Description"
		For Each output As DirectSoundDeviceInfo In DirectSoundOut.Devices
			Me.cbMicrophone.Items.Add(output)
			Me.cbMicrophone.SelectedIndex = 0
		Next
	End Sub

	Private Sub Play(ByVal soundLink As String)

		If Me.bgwPlayer.IsBusy = False And Not soundLink.Equals(String.Empty) Then

			Me.lblPlayingSong.Visible = True
			Me.Refresh()
			Me.lblPlayingSong.Text = "Playing : " & soundLink

			' PLAY
			Dim soundFile = soundLink
			Dim wfr As WaveStream = New WaveFileReader(soundFile)
			Dim wc As WaveChannel32 = New WaveChannel32(wfr)
			wc.Volume = CSng(tbVolume.Value / 100)
			wc.PadWithZeroes = False
			Me.output = New DirectSoundOut(CType(Me.cbMicrophone.SelectedItem, DirectSoundDeviceInfo).Guid)
			Me.output.Init(wc)
			Me.output.Play()
			If Me.cbUserOutput.Checked = True Then
				Dim wfr_ As WaveStream = New WaveFileReader(soundFile)
				Dim wc_ As WaveChannel32 = New WaveChannel32(wfr_)
				wc_.Volume = CSng(tbVolume.Value / 100)
				wc_.PadWithZeroes = False
				Me.userOutput = New DirectSoundOut()
				Me.userOutput.Init(wc_)
				Me.userOutput.Play()
			End If
			Me.bgwPlayer.RunWorkerAsync()

		End If

	End Sub
	Private Sub StopPlay()
		If Me.output IsNot Nothing Then
			Me.output.Stop()
		End If
		If Me.userOutput IsNot Nothing Then
			Me.userOutput.Stop()
		End If
	End Sub

	Private Function GetUsedKeys() As List(Of Integer)
		Return Me.dgvSounds.Rows.Cast(Of DataGridViewRow).Select(Function(n) n.Cells(Me.colPlayButton.Name).Value).Where(Function(i) i IsNot Nothing).Select(Function(k) GetKeyCode(CStr(k))).ToList()
	End Function
	Private Function GetNonUsedKeys() As List(Of Integer)
		Dim unusedKeys As New List(Of Integer)(MainWindow.ALLOWED_KEYS.Select(Function(k) AscW(k)))
		Me.GetUsedKeys().ForEach(Function(r) unusedKeys.Remove(r))
		'unusedKeys.ForEach(Sub(s) MsgBox(s)) ' TEST
		Return unusedKeys
	End Function
	Private Function GetSoundFromKey(ByVal key As Integer) As String
		Dim keys As List(Of String) = Me.dgvSounds.Rows.Cast(Of DataGridViewRow).Where(Function(c) GetKeyCode(CStr(c.Cells(Me.colPlayButton.Name).Value)) = key).Select(Function(n) n.Cells(Me.colSoundLink.Name).Value).Cast(Of String)().ToList()
		If keys.Count < 1 Then Return String.Empty
		Return keys.First()
	End Function

	Private Sub RefreshVolume()
		Me.lblVolume.Text = CStr(Math.Round(Me.tbVolume.Value / 10, 2))
	End Sub

	Private Sub RefreshHandlingToggleKey()
		Select Case Me.handlingToggleKey
			Case True
				Me.btnToggleKey.Text = "Enter key ..."
			Case False
				If Me.toggleKey Is Nothing Then
					Me.btnToggleKey.Text = "Toggle key"
				Else
					Me.btnToggleKey.Text = MainWindow.TOGGLE_KEYS(CInt(Me.toggleKey))
				End If
		End Select
	End Sub

	Private Sub RefreshToggleKeyButton()
		If Me.toggleKeyLocked = True Then
			Me.btnToggleKey.FontWeight = MetroFramework.MetroLabelWeight.Bold
			Me.btnToggleKey.Style = MetroFramework.MetroColorStyle.Red
		Else
			Me.btnToggleKey.FontWeight = MetroFramework.MetroLabelWeight.Light
			Me.btnToggleKey.Style = MetroFramework.MetroColorStyle.Black
		End If
		Me.btnToggleKey.Refresh()
	End Sub

	Private Function GetKeyCode(ByVal vKey As String) As Integer
		If vKey Is Nothing OrElse vKey.Equals(String.Empty) Then Return 0
		Return AscW(vKey)
	End Function

#End Region

End Class