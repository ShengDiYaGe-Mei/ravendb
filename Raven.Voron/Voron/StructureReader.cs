﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using Voron.Util;

namespace Voron
{
	public unsafe class StructureReader<T>
	{
		class ReadVariableSizeFieldInfo
		{
			public int Length;
			public int Offset;
		}

		private readonly Structure<T> _value;
		private readonly byte* _ptr;
		private readonly StructureSchema<T> _schema;
		private Dictionary<T, ReadVariableSizeFieldInfo> _variableFieldInfo = null;

		public StructureReader(byte* ptr, StructureSchema<T> schema)
		{
			_ptr = ptr;
			_schema = schema;
		}

		public StructureReader(Structure<T> value, StructureSchema<T> schema)
		{
			_value = value;
			_schema = schema;
		}

		private int FixedFieldOffset(T field)
		{
			return _schema._fixedSizeFields[field].Offset;
		}

		private ReadVariableSizeFieldInfo VariableFieldSizeInfo(T field)
		{
			if (_variableFieldInfo != null) 
				return _variableFieldInfo[field];
			
			_variableFieldInfo = new Dictionary<T, ReadVariableSizeFieldInfo>(_schema._variableSizeFields.Count);

			var variableFieldsPtr = _ptr + _schema.FixedSize;

			var offset = _schema.FixedSize;

			foreach (var fieldKey in _schema._variableSizeFields.Keys)
			{
				int valueLengthSize;
				var length = Read7BitEncodedInt(variableFieldsPtr, out valueLengthSize);

				offset += valueLengthSize;

				_variableFieldInfo.Add(fieldKey, new ReadVariableSizeFieldInfo
				{
					Length = length,
					Offset = offset
				});

				variableFieldsPtr += valueLengthSize + length;
				offset += length;
			}

			return _variableFieldInfo[field];
		}

		private int Read7BitEncodedInt(byte* ptr, out int size)
		{
			size = 0;
			// Read out an Int32 7 bits at a time.  The high bit 
			// of the byte when on means to continue reading more bytes.
			int value = 0;
			int shift = 0;
			byte b;
			do
			{
				// Check for a corrupted stream.  Read a max of 5 bytes. 
				if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
					throw new InvalidDataException("Invalid 7bit shifted value, used more than 5 bytes");

				// ReadByte handles end of stream cases for us.
				b = *ptr;
				ptr++;
				size++;
				value |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);

			return value;
		}

		public int ReadInt(T field)
		{
			if(_ptr != null)
				return *((int*)(_ptr + FixedFieldOffset(field)));

			return (int) _value._fixedSizeWrites[field].Value;
		}

		public uint ReadUInt(T field)
		{
			if (_ptr != null)
				return *((uint*) (_ptr + FixedFieldOffset(field)));

			return (uint) _value._fixedSizeWrites[field].Value;
		}

		public long ReadLong(T field)
		{
			if (_ptr != null)
				return *((long*)(_ptr + FixedFieldOffset(field)));

			return (long) _value._fixedSizeWrites[field].Value;
		}

		public ulong ReadULong(T field)
		{
			if (_ptr != null)
				return *((ulong*) (_ptr + FixedFieldOffset(field)));

			return (ulong) _value._fixedSizeWrites[field].Value;
		}

		public char ReadChar(T field)
		{
			if (_ptr != null)
				return *((char*) (_ptr + FixedFieldOffset(field)));

			return (char) _value._fixedSizeWrites[field].Value;
		}

		public byte ReadByte(T field)
		{
			if (_ptr != null)
				return *(_ptr + FixedFieldOffset(field));

			return (byte) _value._fixedSizeWrites[field].Value;
		}

		public bool ReadBool(T field)
		{
			byte value;
			if (_ptr != null)
			{
				value = (*(_ptr + FixedFieldOffset(field)));
			}
			else
			{
				value = (byte) _value._fixedSizeWrites[field].Value;
			}

			switch (value)
			{
				case 0:
					return false;
				case 1:
					return true;
				default:
					throw new InvalidDataException("Unexpected boolean value: " + value);
			}
		}

		public sbyte ReadSByte(T field)
		{
			if (_ptr != null)
				return *((sbyte*) (_ptr + FixedFieldOffset(field)));

			return (sbyte) _value._fixedSizeWrites[field].Value;
		}

		public short ReadShort(T field)
		{
			if (_ptr != null)
				return *((short*) (_ptr + FixedFieldOffset(field)));

			return (short) _value._fixedSizeWrites[field].Value;
		}

		public ushort ReadUShort(T field)
		{
			if (_ptr != null)
				return *((ushort*) (_ptr + FixedFieldOffset(field)));

			return (ushort) _value._fixedSizeWrites[field].Value;
		}

		public float ReadFloat(T field)
		{
			if (_ptr != null)
				return *((float*) (_ptr + FixedFieldOffset(field)));

			return (float) _value._fixedSizeWrites[field].Value;
		}

		public double ReadDouble(T field)
		{
			if (_ptr != null)
				return *((double*) (_ptr + FixedFieldOffset(field)));

			return (double) _value._fixedSizeWrites[field].Value;
		}

		public decimal ReadDecimal(T field)
		{
			if (_ptr != null)
				return *((decimal*) (_ptr + FixedFieldOffset(field)));

			return (decimal) _value._fixedSizeWrites[field].Value;
		}

		public string ReadString(T field)
		{
			if (_ptr != null)
			{
				var filedInfo = VariableFieldSizeInfo(field);

				return new string((sbyte*)(_ptr + filedInfo.Offset), 0, filedInfo.Length, Encoding.UTF8);
			}

			return Encoding.UTF8.GetString(_value._variableSizeWrites[field].Value);
		}

		public byte[] ReadBytes(T field)
		{
			if (_ptr != null)
			{
				var filedInfo = VariableFieldSizeInfo(field);

				var result = new byte[filedInfo.Length];

				fixed (byte* rPtr = result)
				{
					MemoryUtils.Copy(rPtr, _ptr + filedInfo.Offset, filedInfo.Length);
				}

				return result;
			}

			return _value._variableSizeWrites[field].Value;
		}
	}
}