Public Class KeyColumn
	Inherits DataGridViewComboBoxColumn

	Public Overrides Property CellTemplate As DataGridViewCell
		Get
			Dim tempCell As New DataGridViewComboBoxCell
			tempCell.FlatStyle = Windows.Forms.FlatStyle.Flat
			tempCell.Items.Clear()
			tempCell.Items.AddRange(MainWindow.ALLOWED_KEYS.ToArray)
			Return tempCell
		End Get
		Set(value As DataGridViewCell)
			MyBase.CellTemplate = value
		End Set
	End Property

	Public Sub New()
		MyBase.New()
	End Sub

End Class