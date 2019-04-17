using System;
using System.Collections.Generic;

namespace CsvReaderSample
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            var reader = new CsvReader();
            IEnumerable<List<string>> records = reader.Read(args[0]);

            int row = 0, col = 0;
            foreach (var record in records)
            {
                row++;
                foreach (var field in record)
                {
                    col++;
                    Console.WriteLine($"[Row,Col] = [{row},{col}] = {field}");
                }
            }
        }
    }
}
