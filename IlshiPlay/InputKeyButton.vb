Public Class InputKeyButton
	Inherits MetroFramework.Controls.MetroButton

	Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message, ByVal keyData As System.Windows.Forms.Keys) As Boolean
		Select Case CType(msg.WParam.ToInt32, Keys)
			Case Keys.Enter
				'indicates you've handled the message sent by the enter keypress here; basically, you're eating up these messages
				Return True
			Case Else
				Return MyBase.ProcessCmdKey(msg, keyData)
		End Select
	End Function

	Public Sub New()
		'
	End Sub

End Class