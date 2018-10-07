Imports System.Text.RegularExpressions

Class MainWindow
    Private Sub FromTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        'Cast the event sender to a TextBox
        Dim textBox As TextBox = CType(sender, TextBox)

        'Variable initialization
        Dim processedText As String = textBox.Text
        Dim textLength As Integer = textBox.Text.Length

        'Empty the contents of the results window
        ToTextBox.SelectAll()
        ToTextBox.Selection.Text = ""

        'Start the text processing if there is something written
        If processedText.Length Then

            'Removal of carriage returns
            processedText = processedText.Replace(vbCrLf, "")

            'Trim everything before first double quotes, if there is anything
            Dim firstQuotes As Integer = processedText.IndexOf("""") + 1
            processedText = If(firstQuotes > -1, processedText.Remove(0, firstQuotes), processedText)

            Dim lastQuotes As Integer = processedText.LastIndexOf("""")
            processedText = If(lastQuotes > -1, processedText.Remove(lastQuotes), processedText)

            Dim remove As Boolean = False
            Dim concatRegex As String = """[\+\s]*"""

            processedText = Regex.Replace(processedText, concatRegex, "")
            processedText = Regex.Replace(processedText, "\\r\\n", "")

            processedText = processedText.ToUpper

            processedText = LineFeed("FROM", processedText)
            processedText = LineFeed("WHERE", processedText)
            processedText = LineFeed("ORDER", processedText)
        End If

        ToTextBox.AppendText(processedText)

    End Sub

    Private Function LineFeed(ByVal word As String, ByVal text As String) As String
        Dim index = text.IndexOf(word)
        Return If(index > -1, text.Substring(0, index - 1) & vbLf & text.Substring(index), text)
    End Function
End Class
