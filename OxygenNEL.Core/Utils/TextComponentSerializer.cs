using System;
using System.Text;
using DotNetty.Buffers;

namespace OxygenNEL.Core.Utils;


public static class TextComponentSerializer
{
	public static IByteBuffer Serialize(TextComponent component, IByteBufferAllocator? allocator = null)
	{
		allocator ??= PooledByteBufferAllocator.Default;
		var byteBuffer = allocator.Buffer();
		try
		{
			byteBuffer.WriteByte(10);
			SerializeCompound(byteBuffer, component);
			return byteBuffer;
		}
		catch
		{
			byteBuffer.Release();
			throw;
		}
	}

	private static void SerializeCompound(IByteBuffer buffer, TextComponent component)
	{
		var flag = !string.IsNullOrEmpty(component.Text) || !string.IsNullOrEmpty(component.Translate);
		if (!string.IsNullOrEmpty(component.Text))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "text");
			WriteString(buffer, component.Text);
		}
		else if (!flag)
		{
			buffer.WriteByte(8);
			WriteString(buffer, "text");
			WriteString(buffer, "");
		}
		if (!string.IsNullOrEmpty(component.Translate))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "translate");
			WriteString(buffer, component.Translate);
		}
		if (!string.IsNullOrEmpty(component.Color))
		{
			buffer.WriteByte(8);
			WriteString(buffer, "color");
			WriteString(buffer, component.Color);
		}
		if (component.Bold)
		{
			buffer.WriteByte(1);
			WriteString(buffer, "bold");
			buffer.WriteByte(1);
		}
		if (component.Extra.Count > 0)
		{
			buffer.WriteByte(9);
			WriteString(buffer, "extra");
			buffer.WriteByte(10);
			buffer.WriteInt(component.Extra.Count);
			foreach (var item in component.Extra)
			{
				SerializeCompound(buffer, item);
			}
		}
		buffer.WriteByte(0);
	}

	public static TextComponent Deserialize(IByteBuffer buffer)
	{
		var textComponent = new TextComponent();
		var stringBuilder = new StringBuilder();
		if (buffer.ReadByte() != 10)
		{
			buffer.SetReaderIndex(buffer.ReaderIndex - 1);
			textComponent.Text = ReadUtf8String(buffer);
			return textComponent;
		}
		DeserializeCompound(buffer, textComponent, stringBuilder);
		textComponent.FullText = stringBuilder.ToString();
		return textComponent;
	}

	private static void DeserializeCompound(IByteBuffer buffer, TextComponent component, StringBuilder textBuilder)
	{
		while (true)
		{
			var b = buffer.ReadByte();
			if (b == 0)
			{
				break;
			}
			var text = ReadString(buffer);
			switch (b)
			{
			case 1:
			{
				var b3 = buffer.ReadByte();
				if (text == "bold")
				{
					component.Bold = b3 != 0;
				}
				break;
			}
			case 8:
			{
				var text2 = ReadString(buffer);
				switch (text)
				{
				case "text":
					if (string.IsNullOrEmpty(component.Text))
					{
						component.Text = text2;
					}
					textBuilder.Append(text2);
					break;
				case "translate":
					component.Translate = text2;
					textBuilder.Append(text2);
					break;
				case "color":
					component.Color = text2;
					break;
				}
				break;
			}
			case 9:
				if (text == "extra")
				{
					var b2 = buffer.ReadByte();
					var num = buffer.ReadInt();
					for (var i = 0; i < num; i++)
					{
						if (b2 == 10)
						{
							var textComponent = new TextComponent();
							DeserializeCompound(buffer, textComponent, textBuilder);
							component.Extra.Add(textComponent);
						}
						else
						{
							SkipTag(buffer, b2);
						}
					}
				}
				else
				{
					SkipList(buffer);
				}
				break;
			default:
				SkipTag(buffer, b);
				break;
			}
		}
	}

	private static void SkipList(IByteBuffer buffer)
	{
		var tagType = buffer.ReadByte();
		var num = buffer.ReadInt();
		for (var i = 0; i < num; i++)
		{
			SkipTag(buffer, tagType);
		}
	}

	private static void WriteString(IByteBuffer buffer, string value)
	{
		var bytes = Encoding.UTF8.GetBytes(value);
		buffer.WriteShort(bytes.Length);
		buffer.WriteBytes(bytes);
	}

	private static string ReadString(IByteBuffer buffer)
	{
		var array = new byte[buffer.ReadUnsignedShort()];
		buffer.ReadBytes(array);
		return Encoding.UTF8.GetString(array);
	}

	private static string ReadUtf8String(IByteBuffer buffer)
	{
		var array = new byte[ReadVarInt(buffer)];
		buffer.ReadBytes(array);
		return Encoding.UTF8.GetString(array);
	}

	private static int ReadVarInt(IByteBuffer buffer)
	{
		var num = 0;
		var num2 = 0;
		while (true)
		{
			var b = buffer.ReadByte();
			num |= (b & 0x7F) << num2;
			if ((b & 0x80) == 0)
			{
				break;
			}
			num2 += 7;
			if (num2 >= 32)
			{
				throw new Exception("VarInt too big");
			}
		}
		return num;
	}

	private static void SkipTag(IByteBuffer buffer, byte tagType)
	{
		switch (tagType)
		{
		case 1:
			buffer.SkipBytes(1);
			break;
		case 2:
			buffer.SkipBytes(2);
			break;
		case 3:
			buffer.SkipBytes(4);
			break;
		case 4:
			buffer.SkipBytes(8);
			break;
		case 5:
			buffer.SkipBytes(4);
			break;
		case 6:
			buffer.SkipBytes(8);
			break;
		case 7:
			buffer.SkipBytes(buffer.ReadInt());
			break;
		case 8:
			buffer.SkipBytes(buffer.ReadUnsignedShort());
			break;
		case 9:
		{
			var tagType2 = buffer.ReadByte();
			var num = buffer.ReadInt();
			for (var i = 0; i < num; i++)
			{
				SkipTag(buffer, tagType2);
			}
			break;
		}
		case 10:
			while (true)
			{
				var b = buffer.ReadByte();
				if (b != 0)
				{
					buffer.SkipBytes(buffer.ReadUnsignedShort());
					SkipTag(buffer, b);
					continue;
				}
				break;
			}
			break;
		case 11:
			buffer.SkipBytes(buffer.ReadInt() * 4);
			break;
		case 12:
			buffer.SkipBytes(buffer.ReadInt() * 8);
			break;
		}
	}
}
