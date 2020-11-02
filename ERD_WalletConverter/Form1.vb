Public Class Form1
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim ofd1 As New OpenFileDialog With {.CheckFileExists = True, .Filter = "JSON|*.json"}
        If ofd1.ShowDialog = DialogResult.OK Then
            TextBox1.Text = ofd1.FileName
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim ofd1 As New SaveFileDialog With {.Filter = "PEM|*.PEM"}
        If ofd1.ShowDialog = DialogResult.OK Then
            TextBox3.Text = ofd1.FileName
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If String.IsNullOrEmpty(TextBox1.Text) Then
            MessageBox.Show("json file empty.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If
        If String.IsNullOrEmpty(TextBox2.Text) Then
            MessageBox.Show("no password supplied.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If
        If String.IsNullOrEmpty(TextBox3.Text) Then
            MessageBox.Show("pem file empty.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If
        Dim a As New ERDLib.Elrond
        Try
            a.FromJSONKeystore(TextBox1.Text, TextBox2.Text)
            a.ToPEM(TextBox3.Text)
            MessageBox.Show("done.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show("pem file empty.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Sub
End Class
