﻿// -----------------------------------------------------------------------
//  <copyright file="Structure.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Voron.Util;

namespace Voron
{
	public unsafe abstract class Structure
	{
		public abstract void Write(byte* ptr);

		public abstract int GetSize();
	}

	public unsafe class Structure<T> : Structure
	{
		internal class FixedSizeWrite
		{
			public ValueType Value;
			public FixedSizeField FieldInfo;
		}

		internal class VariableSizeWrite
		{
			public byte[] Value;
			public byte ValueSizeLength;
			public int Index;
		}

		private readonly StructureSchema<T> _schema;
		internal readonly Dictionary<T, FixedSizeWrite> _fixedSizeWrites = new Dictionary<T, FixedSizeWrite>();
		internal readonly Dictionary<T, VariableSizeWrite> _variableSizeWrites = new Dictionary<T, VariableSizeWrite>();

		public Structure(StructureSchema<T> schema)
		{
			_schema = schema;
		}

		public Structure<T> Set<TValue>(T field, TValue value)
		{
			FixedSizeField fixedSizeField;
			VariableSizeField variableSizeField = null;

			if (_schema._fixedSizeFields.TryGetValue(field, out fixedSizeField) == false && _schema._variableSizeFields.TryGetValue(field, out variableSizeField) == false)
				throw new ArgumentException("No such field in schema defined. Field name: " + field);

			var type = value.GetType();

			if (fixedSizeField != null)
			{
				if (type != fixedSizeField.Type)
					throw new InvalidDataException(string.Format("Attempt to set a field value which type is different than defined in the structure schema. Expected: {0}, got: {1}", fixedSizeField.Type, type));

				var valueTypeValue = value as ValueType;

				if (valueTypeValue == null)
					throw new NotSupportedException("Unexpected fixed size value type: " + type);

				_fixedSizeWrites.Add(field, new FixedSizeWrite { Value = valueTypeValue, FieldInfo = fixedSizeField });
			}
			else if (variableSizeField != null)
			{
				if (type != variableSizeField.Type)
					throw new InvalidDataException(string.Format("Attempt to set a field value which type is different than defined in the structure schema. Expected: {0}, got: {1}", variableSizeField.Type, type)); 

				var stringValue = value as string;
				var bytesValue = value as byte[];

				byte[] bytes;
				if (stringValue != null)
				{
					bytes = Encoding.UTF8.GetBytes(stringValue);
				}
				else if (bytesValue != null)
				{
					bytes = bytesValue;
				}
				else
					throw new NotSupportedException("Unexpected variable size value type: " + type);

				_variableSizeWrites.Add(field, new VariableSizeWrite
				{
					Value = bytes,
					ValueSizeLength = SizeOf7BitEncodedInt(bytes.Length),
					Index = variableSizeField.Index
				});
			}

			return this;
		}

		public override void Write(byte* ptr)
		{
			if (_schema.IsFixedSize == false && _variableSizeWrites.Count != 0 && _variableSizeWrites.Count != _schema._variableSizeFields.Count)
			{
				var missingFields = _schema._variableSizeFields.Select(x => x.Key).Except(_variableSizeWrites.Keys).Select(x => x.ToString());

				throw new InvalidOperationException("Your structure has variable size fields. You have to set all of them to properly write a structure and avoid overlapping fields. Missing fields: " + string.Join(", ", missingFields));
			}

			WriteFixedSizeFields(ptr);
			WriteVariableSizeFields(ptr);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteFixedSizeFields(byte* ptr)
		{
			if (_fixedSizeWrites.Count == 0)
				return;

			foreach (var fixedSizeWrite in _fixedSizeWrites.Values)
			{
				if (fixedSizeWrite.FieldInfo.Type == typeof(int))
				{
					*((int*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (int) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(long))
				{
					*((long*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (long) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(byte))
				{
					*(ptr + fixedSizeWrite.FieldInfo.Offset) = (byte) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(float))
				{
					*((float*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (float) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(double))
				{
					*((double*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (double) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(decimal))
				{
					*((decimal*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (decimal) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(short))
				{
					*((short*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (short) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(bool))
				{
					var booleanValue = (bool) fixedSizeWrite.Value;
					*(ptr + fixedSizeWrite.FieldInfo.Offset) = booleanValue ? (byte) 1 : (byte) 0;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(char))
				{
					*((char*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (char) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(uint))
				{
					*((uint*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (uint) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(ulong))
				{
					*((ulong*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (ulong) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(sbyte))
				{
					*((sbyte*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (sbyte) fixedSizeWrite.Value;
				}
				else if (fixedSizeWrite.FieldInfo.Type == typeof(ushort))
				{
					*((ushort*) (ptr + fixedSizeWrite.FieldInfo.Offset)) = (ushort) fixedSizeWrite.Value;
				}
				else
				{
					throw new NotSupportedException("Unexpected fixed size type: " + fixedSizeWrite.FieldInfo.Type);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteVariableSizeFields(byte* ptr)
		{
			if (_variableSizeWrites.Count == 0)
				return;

			ptr += _schema.FixedSize;

			foreach (var write in _variableSizeWrites.Values.OrderBy(x => x.Index))
			{
				var valueLength = write.Value.Length;

				Write7BitEncodedInt(ptr, valueLength);
				ptr += write.ValueSizeLength;

				fixed (byte* valuePtr = write.Value)
				{
					MemoryUtils.Copy(ptr, valuePtr, valueLength);
				}
				ptr += valueLength;
			}
		}

		public override int GetSize()
		{
			if (_schema.IsFixedSize)
				return _schema.FixedSize;

			return _schema.FixedSize + _variableSizeWrites.Sum(x => x.Value.Value.Length + x.Value.ValueSizeLength);
		}

		private static byte SizeOf7BitEncodedInt(int value)
		{
			byte size = 1;
			var v = (uint) value;
			while (v >= 0x80)
			{
				size++;
				v >>= 7;
			}

			return size;
		}
		private static void Write7BitEncodedInt(byte* ptr, int value)
		{
			// Write out an int 7 bits at a time.  The high bit of the byte, 
			// when on, tells reader to continue reading more bytes. 
			var v = (uint) value;   // support negative numbers
			while (v >= 0x80)
			{
				*ptr = (byte) (v | 0x80);
				ptr++;
				v >>= 7;
			}
			*ptr = (byte) (v);
		}
	}
}