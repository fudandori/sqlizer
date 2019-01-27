Imports System.Text.RegularExpressions
Imports System.Windows.Threading
Class MainWindow

    Private processedText As String
    Private ReadOnly defaultFontSize As Integer = 12
    Private ReadOnly keywords() As String = {
        "SELECT", "FROM", "WHERE", "ORDER", "$BY", "ON", "AND", "OR", "JOIN", "%UNION", "%UNION_ALL", "$AS", "ALTER", "$NOT", "$NULL",
        "$ASC", "$DESC", "VALUES", "$IS", "$UPDATE", "SET", "$ROWNUM",
        "CREATE", "DROP", "$TABLE", "TRUNCATE", "ADD", "$COLUMN", "$IN"}

    Private cooldown As DispatcherTimer = New DispatcherTimer

    Public Sub New()

        InitializeComponent()

        'Timer setup
        AddHandler cooldown.Tick, AddressOf dispatcherTimer_Tick
        cooldown.Interval = New TimeSpan(0, 0, 0, 0, 500)

    End Sub

    Private Sub dispatcherTimer_Tick()
        cooldown.Stop()

        Clean()
        LineBreak()
        Paint(Brushes.Blue, Brushes.Green, Brushes.OrangeRed)

    End Sub

    Private Sub FromTextBox_TextChanged()
        cooldown.Stop()

        'Empty the contents of the results window
        ToTextBox.SelectAll()
        ToTextBox.Selection.Text = ""

        cooldown.Start()
    End Sub

    ''' <summary>
    ''' Cleans the text so it looks like a SQL sentence
    ''' </summary>
    Private Sub Clean()

        processedText = FromTextBox.Text

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

        'Addition of space after a comma
        processedText = Regex.Replace(processedText, "\s?,", ", ")

        'Trim stacks of spaces into a single space
        processedText = Regex.Replace(processedText, "\s+", " ")

        'Conversion of UNION ALL to UNION_ALL for parsing purposes
        processedText = Regex.Replace(processedText, "UNION ALL", "UNION_ALL")

        'Removal of a space at the begining, if there is any
        processedText = Regex.Replace(processedText, "^\s", "")

    End Sub

    ''' <summary>
    ''' Searchs the processed text for SQL keywords and special characters and colors them
    ''' </summary>
    ''' <param name="keywordBrush">Brush with the color to be used for recoloring the words</param>
    ''' <param name="stringBrush">Normal text brush</param>
    ''' <param name="operatorBrush">Brush for the operator characters</param>
    Private Sub Paint(keywordBrush As SolidColorBrush, stringBrush As SolidColorBrush, operatorBrush As SolidColorBrush)

        'Select everything on the processed text textbox and save it in an aux variable (plus a space for the coming logic)
        ToTextBox.SelectAll()

        'Reset of the processed text textbox
        ToTextBox.Selection.Text = ""

        Dim pattern As String = KeywordRegexBuilder()

        Dim buffer As String = ""
        Dim lastChar As String = ""
        Dim bufferingString As Boolean = False

        'Cycle trough the processed text with an space at the end so it stops at the last word
        For Each c As Char In processedText & " "

            buffer &= c

            'If a space or line break is detected it means it has buffered a whole word.
            'In case we are buffering a string we ignore this
            If ((c = " "c OrElse c = vbLf) AndAlso Not bufferingString) Then

                'The word is colored and uppercased if it's a SQL Keyword (from the list), or regular otherwise
                If Regex.IsMatch(buffer, pattern, RegexOptions.IgnoreCase) Then
                    DyeWord(buffer.ToUpper.Replace("_"c, " "c), keywordBrush, True)
                Else
                    Write(buffer)
                End If

                'Buffer reset
                buffer = ""

            ElseIf (c = "'"c) Then

                'If a single quote is found a flag is raised to ignore the rest of the conditionals so everything within the quotes falls here
                bufferingString = True

                'When a buffer with a text enclosed in single quotes (full string) is found then it's written in green
                If (Regex.IsMatch(buffer, "^'.+'$")) Then

                    DyeWord(buffer, stringBrush, False)

                    'Buffer and flag reset
                    bufferingString = False
                    buffer = ""

                End If

            ElseIf (c = "("c AndAlso Not bufferingString) Then

                'The word preceding a parenthesis is always a keyword, so it's written with keyword color
                buffer = buffer.Substring(0, buffer.Length - 1)
                DyeWord(buffer.ToUpper, keywordBrush, True)
                DyeWord("(", operatorBrush, True)
                buffer = ""

            ElseIf (Regex.IsMatch(buffer, "^.*\)$") AndAlso Not bufferingString) Then

                'Text preceding the closing parenthesis is dyed
                buffer = buffer.Substring(0, buffer.Length - 1)

                'Check in case it is a keyword
                If Regex.IsMatch(buffer, pattern, RegexOptions.IgnoreCase) Then
                    DyeWord(buffer.ToUpper, keywordBrush, True)
                Else
                    Write(buffer)
                End If

                'Dye closing parenthesis and buffer reset
                DyeWord(")", operatorBrush, True)
                buffer = ""

            ElseIf (IsOperator(c) AndAlso Not bufferingString) Then

                'Paint the text preceding the operator
                Dim text As String = buffer.Substring(0, buffer.Length - 1).Trim
                Write(text)

                'Add a space before the operator if it doesn't exist (for beautifying purposes)
                'Also checks if it's a double char operator (e.g. >=, <>, etc...)
                If (lastChar <> " "c AndAlso Not IsOperator(lastChar)) Then
                    Write(" ")
                End If

                'Dye the operator
                Dim operatorChar As String = buffer.Substring(buffer.Length - 1)
                DyeWord(operatorChar, operatorBrush, True)

                'Buffer reset
                buffer = ""

            ElseIf (IsOperator(lastChar) AndAlso Not IsOperator(c) AndAlso c <> " "c) Then
                'Beutifying space after an operator if it doesn't exist
                Write(" ")
            End If

            lastChar = c

        Next

    End Sub

    Private Function IsOperator(input As String) As Boolean
        Return Regex.IsMatch(input, "^<|>|=|\+|\-|\*|%$")
    End Function

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
            processedText = Regex.Replace(processedText, pattern, replacement, RegexOptions.IgnoreCase)
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
