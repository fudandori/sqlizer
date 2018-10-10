Imports System.Text.RegularExpressions

Class MainWindow

    Private processedText As String
    Private ReadOnly keywords() As String = {"FROM", "WHERE", "ORDER", "ON", "AND"}
    Private ReadOnly defaultFontSize As Integer = 12

    Private Sub FromTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
        'Cast the event sender to a TextBox
        Dim textBox As TextBox = CType(sender, TextBox)

        'Variable initialization
        processedText = textBox.Text
        Dim textLength As Integer = textBox.Text.Length

        'Empty the contents of the results window
        ToTextBox.SelectAll()
        ToTextBox.Selection.Text = ""

        'Start the text processing if there is something written
        If processedText.Length Then

            CleanText()

            'Conversion of all the text to uppercase
            processedText = processedText.ToUpper

            'Addition of a new line after key SQL words (defined in the "keywords" field)
            For Each word As String In keywords
                LineFeed(word)
            Next
        End If

        'Write the result
        ToTextBox.AppendText(processedText)

        Recolor()
    End Sub

    ''' <summary>
    ''' Adds a carriage return after the specified keyword
    ''' </summary>
    ''' <param name="word">The keyword after which to insert the new line</param>
    Private Sub LineFeed(ByVal word As String)
        Dim index = processedText.IndexOf(word)
        processedText = If(index > -1, processedText.Substring(0, index) & vbLf & processedText.Substring(index), processedText)
    End Sub

    ''' <summary>
    ''' Cleans the text so it looks like a SQL sentence
    ''' </summary>
    Private Sub CleanText()

        'Removal of carriage returns
        processedText = processedText.Replace(vbCrLf, "")

        'Remove everything before first double quotes, if there is anything
        Dim firstQuotes As Integer = processedText.IndexOf("""") + 1
        processedText = If(firstQuotes > -1, processedText.Remove(0, firstQuotes), processedText)

        'Remove everything after last double quotes, if there's anything
        Dim lastQuotes As Integer = processedText.LastIndexOf("""")
        processedText = If(lastQuotes > -1, processedText.Remove(lastQuotes), processedText)

        'Removal of JAVA concatenation syntax
        processedText = Regex.Replace(processedText, """[\+\s]*""", "")

        'Removal of JAVA carriage returns
        processedText = Regex.Replace(processedText, "\\r\\n", "")

        'Removal of spaces with line break
        processedText = Regex.Replace(processedText, "\s+\n", "")

        'Trim stacks of spaces into a single space
        processedText = Regex.Replace(processedText, "\s+", " ")

        'Removal of a space at the begining, if there is any
        processedText = Regex.Replace(processedText, "^\s", "")

    End Sub

    Private Sub FontSizeSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles FontSizeSlider.ValueChanged
        Dim slider As Slider = CType(sender, Slider)

        FromTextBox.FontSize = defaultFontSize * slider.Value
        ToTextBox.FontSize = defaultFontSize * slider.Value
    End Sub

    Private Sub Recolor()
        ToTextBox.SelectAll()
        Dim selection As String = ToTextBox.Selection.Text & " "
        ToTextBox.Selection.Text = ""

        Dim buffer As String = ""

        For Each c As Char In selection
            If (c = " "c) Then
                If Regex.IsMatch(buffer, "SELECT|FROM|WHERE") Then
                    DyeWord(buffer & c, Brushes.Blue, True)
                Else
                    DyeWord(buffer & c, Brushes.Black, False)
                End If
                buffer = ""
            Else
                buffer &= c
            End If
        Next

    End Sub

    Private Sub DyeWord(word As String, brush As SolidColorBrush, bold As Boolean)
        Dim range As TextRange = New TextRange(ToTextBox.Document.ContentEnd, ToTextBox.Document.ContentEnd)
        range.Text = word
        range.ApplyPropertyValue(TextElement.ForegroundProperty, brush)
        range.ApplyPropertyValue(TextElement.FontWeightProperty, If(bold, FontWeights.Bold, FontWeights.Regular))
    End Sub
End Class
