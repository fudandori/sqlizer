Imports System.Text.RegularExpressions

Class MainWindow

    Private processedText As String
    Private ReadOnly keywords() As String = {"SELECT", "FROM", "WHERE", "ORDER", "ON", "AND", "JOIN", "%UNION", "$AS"}
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

            Clean()
            LineBreak()
            Paint(Brushes.Blue)

        End If

        'Write the result
        'ToTextBox.AppendText(processedText)


    End Sub

    ''' <summary>
    ''' Cleans the text so it looks like a SQL sentence
    ''' </summary>
    Private Sub Clean()

        'Removal of carriage returns
        processedText = processedText.Replace(vbCrLf, " ")

        'Removal of everything before first double quotes, if there is anything
        Dim firstQuotes As Integer = processedText.IndexOf("""") + 1
        processedText = If(firstQuotes > -1, processedText.Remove(0, firstQuotes), processedText)

        'Removal of everything after last double quotes, if there's anything
        Dim lastQuotes As Integer = processedText.LastIndexOf("""")
        processedText = If(lastQuotes > -1, processedText.Remove(lastQuotes), processedText)

        'Removal of JAVA concatenation syntax
        processedText = Regex.Replace(processedText, """[\+\s]*""", " ")

        'Removal of JAVA carriage returns
        processedText = Regex.Replace(processedText, "\\r\\n", " ")

        'Removal of spaces with line break
        processedText = Regex.Replace(processedText, "\s+\n", " ")

        'Trim stacks of spaces into a single space
        processedText = Regex.Replace(processedText, "\s+", " ")

        'Removal of a space at the begining, if there is any
        processedText = Regex.Replace(processedText, "^\s", "")

        'Conversion of all the text to uppercase
        processedText = processedText.ToUpper

    End Sub

    ''' <summary>
    ''' Searchs the processed text for SQL keywords and colors them
    ''' </summary>
    ''' <param name="brush">Brush with the color to be used for recoloring the words</param>
    Private Sub Paint(brush As SolidColorBrush)

        'Select everything on the processed text textbox and save it in an aux variable (plus a space for the coming logic)
        ToTextBox.SelectAll()

        'Reset of the processed text textbox
        ToTextBox.Selection.Text = ""

        Dim pattern As String = KeywordRegexBuilder()
        Dim buffer As String = ""

        'Adds the chars to a buffer, when it detects a space it means it has stored a word and processes it for recoloring
        For Each c As Char In processedText & " "
            If (c = " "c OrElse c = vbLf) Then
                If Regex.IsMatch(buffer & c, pattern) Then
                    DyeWord(buffer & c, brush, True)
                Else
                    Write(buffer & c)
                End If
                buffer = ""
            Else
                buffer &= c
            End If
        Next

    End Sub

    ''' <summary>
    ''' Concatenates a word to the processed text in a specified color and with bold style, if stated
    ''' </summary>
    ''' <param name="word">String with the word to concatenate</param>
    ''' <param name="brush">Color to be used to dye the word with</param>
    ''' <param name="bold">Wether the word is bolded or not</param>
    Private Sub DyeWord(word As String, brush As SolidColorBrush, bold As Boolean)

        Dim range As TextRange = New TextRange(ToTextBox.Document.ContentEnd, ToTextBox.Document.ContentEnd)
        range.Text = word
        range.ApplyPropertyValue(TextElement.ForegroundProperty, brush)
        range.ApplyPropertyValue(TextElement.FontWeightProperty, If(bold, FontWeights.Bold, FontWeights.Regular))

    End Sub

    ''' <summary>
    ''' Concatenates a word to the processed text wth regular style
    ''' </summary>
    ''' <param name="word">String with the word to concatenate</param>
    Private Sub Write(word As String)

        Dim range As TextRange = New TextRange(ToTextBox.Document.ContentEnd, ToTextBox.Document.ContentEnd)
        range.Text = word
        range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black)
        range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular)

    End Sub

    ''' <summary>
    ''' Builds a regex pattern that includes all the keywords (removes the wildcards)
    ''' </summary>
    ''' <returns></returns>
    Private Function KeywordRegexBuilder() As String

        Dim pattern As String = "^("

        For Each keyword As String In keywords
            pattern &= Regex.Replace(keyword, "\$|%", "") & "|"
        Next

        pattern = pattern.Remove(pattern.LastIndexOf("|"), 1)

        pattern &= ")\s?\n?$"

        Return pattern

    End Function

    ''' <summary>
    ''' Generates all the necessary line breaks
    ''' </summary>
    Private Sub LineBreak()

        SingleLineFeed()
        DoubleLineFeed()

    End Sub

    ''' <summary>
    ''' Generates a line break before the eligible keywords
    ''' </summary>
    Private Sub SingleLineFeed()

        Dim words As List(Of String) = New List(Of String)

        'Recollection of all the words that require a single line break (no wildcards)
        For Each keyword As String In keywords
            If (Not Regex.IsMatch(keyword, "\$|%")) Then
                words.Add(keyword)
            End If
        Next

        'Addition of a line break before the keywords
        For Each word As String In words
            Dim pattern As String = "(\s|\n)" & word & "(\s|\n)"
            Dim replacement As String = vbCrLf & word & " "
            processedText = Regex.Replace(processedText, pattern, replacement)
        Next
    End Sub

    ''' <summary>
    ''' Generates a double line break, isolating the keyword in a line, for the eligible keywords
    ''' </summary>
    Private Sub DoubleLineFeed()
        Dim words As List(Of String) = New List(Of String)

        'Recollection of all the words that require a double line break (% wildcard)
        For Each keyword As String In keywords
            If (Regex.IsMatch(keyword, "%")) Then
                words.Add(Regex.Replace(keyword, "%", ""))
            End If
        Next

        'Isolation of the keywords in a single line
        For Each word As String In words
            Dim pattern As String = "(\s|\n)+" & word & "(\s|\n)+"
            Dim replacement As String = vbCrLf & word & vbCrLf
            processedText = Regex.Replace(processedText, pattern, replacement)
        Next
    End Sub

    ''' <summary>
    ''' Slider ValueChanged handler
    ''' </summary>
    Private Sub FontSizeSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles FontSizeSlider.ValueChanged

        Dim slider As Slider = CType(sender, Slider)

        FromTextBox.FontSize = defaultFontSize * slider.Value
        ToTextBox.FontSize = defaultFontSize * slider.Value

    End Sub
End Class
