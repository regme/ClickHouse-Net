﻿using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClickHouse.Ado.Impl.ATG.Insert;
using Buffer = System.Buffer;

namespace ClickHouse.Ado.Impl.ColumnTypes
{
    internal class SimpleColumnType<T> : ColumnType 
    {
        public SimpleColumnType()
        {
        }

        public SimpleColumnType(T[] data)
        {
            Data = data;
        }

        public T[] Data { get; private set; }
        internal override void Read(ProtocolFormatter formatter, int rows)
        {
            var itemSize = Marshal.SizeOf<T>();
            var bytes =formatter.ReadBytes(itemSize * rows);
            Data = new T[rows];
            Buffer.BlockCopy(bytes,0,Data,0, itemSize * rows);
        }

        public override int Rows => Data.Length;

        public override string AsClickHouseType()
        {
            return typeof(T).Name;
        }

        public override void Write(ProtocolFormatter formatter, int rows)
        {
            Debug.Assert(Rows == rows, "Row count mismatch!");
            var itemSize = Marshal.SizeOf<T>();
            var bytes = new byte[itemSize*rows];
            Buffer.BlockCopy(Data, 0, bytes, 0, itemSize*rows);
            formatter.WriteBytes(bytes);
        }

        public override void ValueFromConst(string value, Parser.ConstType typeHint)
        {
            if (typeHint == Parser.ConstType.String)
                Data = new[] { (T)Convert.ChangeType(ProtocolFormatter.UnescapeStringValue(value), typeof(T)) };
            else
                Data = new[] { (T)Convert.ChangeType(value, typeof(T)) };
        }

        public override void ValueFromParam(ClickHouseParameter parameter)
        {
            if (parameter.DbType == DbType.Int16 || parameter.DbType == DbType.Int32 || parameter.DbType == DbType.Int64
                || parameter.DbType == DbType.UInt16 || parameter.DbType == DbType.UInt32 || parameter.DbType == DbType.UInt64
                || parameter.DbType == DbType.Single || parameter.DbType == DbType.Decimal || parameter.DbType == DbType.Decimal)
            {
                Data = new[] {(T) Convert.ChangeType(parameter.Value, typeof(T))};
            }
            else throw new InvalidCastException($"Cannot convert parameter with type {parameter.DbType} to {typeof(T).Name}.");
        }
        public override object Value(int currentRow)
        {
            return Data[currentRow];
        }

        public override long IntValue(int currentRow)
        {
            return Convert.ToInt64(Data[currentRow]);
        }
    }
}