using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fdb.Enums;

namespace Fdb
{
    internal static class FdbProgram
    {
        private static void Main(string[] args)
        {
            string path;

            if (args.Length >= 1)
            {
                path = args[0];
            }
            else
            {
                Console.Write("Input: ");
                path = Console.ReadLine();
            }
            
            Console.WriteLine($"Reading {path} ...");
            var fdb = new FdbFile(path);

            Edit(fdb);
            
            fdb.Write(fdb);

            Console.Write("Output: ");
            var output = Console.ReadLine();

            if (!File.Exists(output)) File.Create(output).Dispose();

            var bytes = fdb.Complete();

            File.WriteAllBytes(output, bytes);
            
            Console.WriteLine("Complete!");
        }

        private static void Edit(FdbFile file)
        {
            var index = 0;
            while (true)
            {
                Console.Clear();
                for (var i = index; i < file.TableCount; i++)
                {
                    Console.WriteLine($"[{i}] " +
                                      $"{file.TableHeader.ColumnHeaders[i].TableName}\t" +
                                      $"{file.TableHeader.ColumnHeaders[i].ColumnCount} columns\t" +
                                      $"{file.TableHeader.RowTopHeaders[i].RowCount} rows"
                    );
                    if (i == index + 9)
                    {
                        break;
                    }
                }

                Console.WriteLine("<up>/<down>/<index>/<save>/<create>");
                
                var input = Console.ReadLine();
                
                if (input == default) continue;

                switch (input.ToLower())
                {
                    case "up":
                        index -= 10;
                        if (index < 0) index = 0;
                        continue;
                    case "down":
                        index += 10;
                        if (index > file.TableCount) index = (int) file.TableCount;
                        continue;
                    case "save":
                        return;
                    case "create":
                        AddTable(file);
                        break;
                    default:
                        if (int.TryParse(input, out var selected))
                        {
                            try
                            {
                                var select = (
                                    file.TableHeader.ColumnHeaders[selected],
                                    file.TableHeader.RowTopHeaders[selected]
                                );
                                
                                EditTable(select);
                            }
                            catch
                            {
                                Console.WriteLine("Selected out of range!");
                            }
                        }
                        break;
                }
            }
        }

        private static void AddTable(FdbFile file)
        {
            Console.Write("New Table Name: ");
            var tableName = Console.ReadLine();

            var tableInfo = new List<(string, DataType)>();

            int columnIndex = default;
            while (true)
            {
                Console.Write($"[{columnIndex}] Column Name: ");
                var columnName = Console.ReadLine();

                while (true)
                {
                    Console.Write($"[{columnIndex}] Column Type: ");
                    
                    if (Enum.TryParse(typeof(DataType), Console.ReadLine(), out var newType))
                    {
                        var data = (DataType) newType;
                        
                        tableInfo.Add((columnName, data));
                        
                        break;
                    }

                    Console.WriteLine("Invalid type");
                }

                Console.Write("Another column? <y>/<>");

                switch (Console.ReadLine()?.ToLower())
                {
                    case "y":
                        columnIndex++;
                        continue;
                    default:
                        goto Finish;
                }
            }
            
            Finish:

            file.TableCount++;

            var columnHeader = file.TableHeader.ColumnHeaders.ToList();
            var rowsHeader = file.TableHeader.RowTopHeaders.ToList();

            var header = new FdbColumnHeader
            {
                ColumnCount = (uint) tableInfo.Count,
                TableName = new FdbString(tableName),
                Data = new FdbColumnData
                {
                    ColumnName = tableInfo.Select(s => new FdbString(s.Item1)).ToArray(),
                    Type = tableInfo.Select(s => s.Item2).ToArray()
                }
            };
            
            var rowHeader = new FdbRowBucket
            {
                RowCount = 128,
                RowHeader = new FdbRowHeader
                {
                    RowInfos = Enumerable.Repeat<FdbRowInfo>(null, 128).ToArray()
                }
            };

            columnHeader.Add(header);
            rowsHeader.Add(rowHeader);

            file.TableHeader.ColumnHeaders = columnHeader.ToArray();
            file.TableHeader.RowTopHeaders = rowsHeader.ToArray();
        }

        private static void EditTable((FdbColumnHeader, FdbRowBucket) table)
        {
            var rows = new List<FdbRowInfo>();

            var (columnHeader, rowTopHeader) = table;
            foreach (var rowInfo in rowTopHeader.RowHeader.RowInfos)
            {
                if (rowInfo == default) continue;
                var info = rowInfo;
                while (true)
                {
                    rows.Add(info);
                    info = info.Linked;

                    if (info == default) break;
                }
            }

            var index = 0;
            while (true)
            {
                Console.Clear();

                for (var i = 0; i < columnHeader.ColumnCount; i++)
                {
                    var name = columnHeader.Data.ColumnName[i];
                    var type = columnHeader.Data.Type[i];
                    Console.Write($"[{type}] <{name}> ");
                }
                Console.Write("\n");
                
                for (var i = index; i < rows.Count; i++)
                {
                    var data = rows[i].DataHeader.Data;

                    Console.Write($"[{i}]");
                    foreach (var o in data.Data)
                    {
                        Console.Write($" {o} ");
                    }

                    Console.Write("\n");
                    
                    if (i == index + 19)
                    {
                        break;
                    }
                }
                
                Console.WriteLine("<up>/<down>/<add>/<index>/<exit>");
                
                var input = Console.ReadLine();
                
                if (input == default) continue;

                switch (input.ToLower())
                {
                    case "up":
                        index -= 20;
                        if (index < 0) index = 0;
                        continue;
                    case "down":
                        index += 20;
                        if (index > rows.Count) index = rows.Count;
                        continue;
                    case "add":
                        
                        var row = new FdbRowInfo();
                        row.DataHeader = new FdbRowDataHeader();
                        row.DataHeader.ColumnCount = columnHeader.ColumnCount;
                        row.DataHeader.Data = new FdbRowData(row.DataHeader);
                        var data = row.DataHeader.Data;

                        for (var i = 0; i < columnHeader.ColumnCount; i++)
                        {
                            data.Types[i] = columnHeader.Data.Type[i];
                            
                            Console.Write($"[{data.Types[i]}] <{columnHeader.Data.ColumnName[i]}> = ");

                            var value = Console.ReadLine();

                            if (value == default)
                            {
                                i--;
                                continue;
                            }

                            try
                            {
                                switch (data.Types[i])
                                {
                                    case DataType.Nothing:
                                        data.Data[i] = 0;
                                        break;
                                    case DataType.Integer:
                                        data.Data[i] = int.Parse(value);
                                        break;
                                    case DataType.Unknown1:
                                        data.Data[i] = int.Parse(value);
                                        break;
                                    case DataType.Float:
                                        data.Data[i] = float.Parse(value);
                                        break;
                                    case DataType.Text:
                                        data.Data[i] = new FdbString(value);
                                        break;
                                    case DataType.Boolean:
                                        data.Data[i] = bool.Parse(value);
                                        break;
                                    case DataType.Bigint:
                                        data.Data[i] = new FdbBitInt(long.Parse(value));
                                        break;
                                    case DataType.Unknown2:
                                        data.Data[i] = int.Parse(value);
                                        break;
                                    case DataType.Varchar:
                                        data.Data[i] = new FdbString(value);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                            catch
                            {
                                i--;
                            }
                        }

                        var added = false;
                        for (var i = 0; i < rowTopHeader.RowCount; i++)
                        {
                            if (rowTopHeader.RowHeader.RowInfos[i] != default) continue;
                            rowTopHeader.RowHeader.RowInfos[i] = row;
                            added = true;
                            break;
                        }

                        if (added == false)
                        {
                            rowTopHeader.RowCount *= 2;

                            Array.Resize(ref rowTopHeader.RowHeader.RowInfos, (int) rowTopHeader.RowCount);
                            
                            for (var i = 0; i < rowTopHeader.RowCount; i++)
                            {
                                if (rowTopHeader.RowHeader.RowInfos[i] != default) continue;
                                rowTopHeader.RowHeader.RowInfos[i] = row;
                                break;
                            }

                        }

                        rows.Add(row);
                        
                        break;
                    case "exit":
                        return;
                    default:
                        if (int.TryParse(input, out var selected))
                        {
                            try
                            {
                                EditRow(rows[selected], columnHeader, rowTopHeader, rows);
                            }
                            catch
                            {
                                Console.WriteLine("Invalid command");
                            }
                        }
                        break;
                }
            }
        }

        public static void EditRow(FdbRowInfo data, FdbColumnHeader header, FdbRowBucket rowHeader, List<FdbRowInfo> rows)
        {
            while (true)
            {
                Console.Clear();

                for (var i = 0; i < header.ColumnCount; i++)
                {
                    var name = header.Data.ColumnName[i];
                    var type = header.Data.Type[i];
                    Console.Write($"[{type}] <{name}> ");
                }
                
                Console.Write("\n");
                
                for (var index = 0; index < data.DataHeader.Data.Data.Length; index++)
                {
                    var o = data.DataHeader.Data.Data[index];
                    Console.Write($"[{index}:{data.DataHeader.Data.Types[index]}] {o} ");
                }

                Console.Write("\n");
                
                Console.WriteLine("<remove>/<index>/<exit>/<type>");
                
                var input = Console.ReadLine();
                
                if (input == default) continue;

                switch (input.ToLower())
                {
                    case "remove":
                        rows.Remove(data);

                        var linked = data.Linked;
                        while (linked != default)
                        {
                            var current = linked;
                            linked = linked.Linked;
                            current.Linked = default;

                            rowHeader.RowCount++;
                            Array.Resize(ref rowHeader.RowHeader.RowInfos, (int) rowHeader.RowCount);
                            rowHeader.RowHeader.RowInfos[rowHeader.RowCount - 1] = current;
                        }
                        
                        return;
                    case "type":
                        Console.Write("Change type at index: ");
                        
                        input = Console.ReadLine();
                        
                        if (input == default) continue;

                        if (int.TryParse(input, out var selectedType))
                        {
                            try
                            {
                                var types = Enum.GetNames(typeof(DataType));

                                for (var index = 0; index < types.Length; index++)
                                {
                                    var type = types[index];
                                    Console.Write($"[{index}] <{type}> ");
                                }
                                
                                Console.Write("\n");

                                Console.Write("New type = ");
                                if (Enum.TryParse(typeof(DataType), Console.ReadLine(), out var newType))
                                {
                                    data.DataHeader.Data.Types[selectedType] = (DataType) newType;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid command");
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Invalid command");
                            }
                        }

                        break;
                    case "exit":
                        return;
                    default:
                        if (int.TryParse(input, out var selected))
                        {
                            var info = data.DataHeader.Data;

                            Console.Write($"[{info.Types[selected]}] <{header.Data.ColumnName[selected]}> = ");
                            var value = Console.ReadLine();

                            if (value == default)
                            {
                                continue;
                            }

                            try
                            {
                                switch (info.Types[selected])
                                {
                                    case DataType.Nothing:
                                        info.Data[selected] = 0;
                                        break;
                                    case DataType.Integer:
                                        info.Data[selected] = int.Parse(value);
                                        break;
                                    case DataType.Unknown1:
                                        info.Data[selected] = int.Parse(value);
                                        break;
                                    case DataType.Float:
                                        info.Data[selected] = float.Parse(value);
                                        break;
                                    case DataType.Text:
                                        info.Data[selected] = new FdbString(value);
                                        break;
                                    case DataType.Boolean:
                                        info.Data[selected] = bool.Parse(value);
                                        break;
                                    case DataType.Bigint:
                                        info.Data[selected] = new FdbBitInt(long.Parse(value));
                                        break;
                                    case DataType.Unknown2:
                                        info.Data[selected] = int.Parse(value);
                                        break;
                                    case DataType.Varchar:
                                        info.Data[selected] = new FdbString(value);
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Invalid command");
                            }
                        }
                        break;
                }
            }
        }
    }
}