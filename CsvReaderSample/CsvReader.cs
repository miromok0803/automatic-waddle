using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvReaderSample
{
    public class CsvReader
    {
        private readonly char _delimiter;
        private readonly char _quote;
        private string _filePath;
        private int _lineNumber = 0;
        private readonly StringBuilder _lineBuffer = new StringBuilder();
        private readonly StringBuilder _fieldBuffer = new StringBuilder();

        public CsvReader() : this(',', '"')
        {
        }

        public CsvReader(char delimiter, char quote)
        {
            _delimiter = delimiter;
            _quote = quote;
        }

        public IEnumerable<List<string>> Read(string filePath)
        {
            return Read(filePath, Encoding.UTF8);
        }

        public IEnumerable<List<string>> Read(string filePath, Encoding encoding)
        {
            _filePath = System.IO.Path.GetFullPath(filePath);
            _lineNumber = 0;

            int quoteCount = 0;
            foreach (string line in System.IO.File.ReadLines(filePath, encoding))
            {
                _lineNumber++;

                if (line.Length == 0 && _lineBuffer.Length == 0)
                {   // blank line
                    continue;
                }

                // RFC4180. Quote character count must even in one record.
                quoteCount = quoteCount + line.Count(c => c.Equals(_quote));
                if (quoteCount % 2 != 0)
                {
                    _lineBuffer.AppendLine(line);
                    continue;
                }

                if (_lineBuffer.Length == 0)
                {
                    yield return FieldParser(line);
                }
                else
                {
                    _lineBuffer.Append(line);
                    yield return FieldParser(_lineBuffer.ToString());
                }

                quoteCount = 0;
                _lineBuffer.Clear();
            }

            if (_lineBuffer.Length > 0)
            {   // Quoted field not closed.
                throw new Exception();
            }
        }

        private enum FieldParseState
        {
            Init,
            NormalField,
            QuotedField,
            ClosingQuotedField
        }

        private List<string> FieldParser(string source)
        {
            List<string> fields = new List<string>();
            FieldParseState state = FieldParseState.Init;

            _fieldBuffer.Clear();
            _fieldBuffer.EnsureCapacity(source.Length);
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                switch (state)
                {
                    case FieldParseState.Init:
                        if (c.Equals(_quote))
                        {
                            state = FieldParseState.QuotedField;
                        }
                        else if (c.Equals(_delimiter))
                        {
                            fields.Add(string.Empty);
                        }
                        else
                        {
                            state = FieldParseState.NormalField;
                            _fieldBuffer.Append(c);
                        }
                        break;

                    case FieldParseState.NormalField:
                        if (c.Equals(_quote))
                        {
                            throw new Exception($"Malformed Error. File:{_filePath}({_lineNumber}) \n Data:\n{source}");
                        }
                        else if (c.Equals(_delimiter))
                        {
                            state = FieldParseState.Init;
                            _fieldBuffer.Append(c);
                            fields.Add(_fieldBuffer.ToString());
                            _fieldBuffer.Clear();
                        }
                        else
                        {
                            _fieldBuffer.Append(c);
                        }
                        break;

                    case FieldParseState.QuotedField:
                        if (c.Equals(_quote))
                        {
                            state = FieldParseState.ClosingQuotedField;
                        }
                        else if (c.Equals(_delimiter))
                        {
                            _fieldBuffer.Append(c);
                        }
                        else
                        {
                            _fieldBuffer.Append(c);
                        }
                        break;

                    case FieldParseState.ClosingQuotedField:
                        if (c.Equals(_quote))
                        {
                            // Cancel close quoted field, and add quote character.
                            state = FieldParseState.QuotedField;
                            _fieldBuffer.Append(c);
                        }
                        else if (c.Equals(_delimiter))
                        {
                            state = FieldParseState.Init;
                            fields.Add(_fieldBuffer.ToString());
                            _fieldBuffer.Clear();
                        }
                        else
                        {
                            throw new Exception($"Malformed Error. File:{_filePath}({_lineNumber}) \n Data:\n{source}");
                        }
                        break;
                }
            }

            // Init -> Add empty field.
            // NormalField -> Add last field.
            // ClosingQuotedField -> Add last quoted field.
            // QuotedField -> Quote Count logic defect ?
            if (state == FieldParseState.QuotedField)
            {
                throw new Exception();
            }

            fields.Add(_fieldBuffer.ToString());
            return fields;
        }
    }
}