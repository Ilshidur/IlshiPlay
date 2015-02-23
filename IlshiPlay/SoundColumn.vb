Public Class SoundColumn
	Inherits DataGridViewTextBoxColumn

	Public Overrides Property CellTemplate As DataGridViewCell
		Get
			Dim tempCell As New DataGridViewTextBoxCell
			'
			Return tempCell
		End Get
		Set(value As DataGridViewCell)
			MyBase.CellTemplate = value
		End Set
	End Property

	Public Sub New()

	End Sub

End Class