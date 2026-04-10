using System;
using System.IO;
using ClosedXML.Excel;
using System.Linq;

namespace ExcelReader {
    class Program {
        static void TestRead(string[] args) {
            var filePath = @"C:\Users\it.dept\Downloads\final_100_employees.xlsx";
            if (!File.Exists(filePath)) { Console.WriteLine("File not found"); return; }
            using (var wb = new XLWorkbook(filePath)) {
                var ws = wb.Worksheet(1);
                var range = ws.RangeUsed();
                if(range == null) { Console.WriteLine("File empty"); return; }
                Console.WriteLine("Rows used: " + range.RowCount());
                var row1 = ws.Row(1);
                for(int i=1; i<=8; i++) {
                    Console.WriteLine("Header " + i + ": " + row1.Cell(i).GetValue<string>());
                }
                var row2 = ws.Row(2);
                for(int i=1; i<=8; i++) {
                    Console.WriteLine("Row 2 Col " + i + ": " + row2.Cell(i).GetValue<string>());
                }
            }
        }
    }
}
