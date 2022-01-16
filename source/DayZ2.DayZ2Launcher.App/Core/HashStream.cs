using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

class HashStream : Stream
{
	private readonly Stream m_stream;
	private readonly HashAlgorithm m_hash;

	public HashStream(Stream stream, HashAlgorithm hash)
	{
		m_stream = stream;
		m_hash = hash;
	}

	public override bool CanRead => m_stream.CanRead;
	public override bool CanSeek => m_stream.CanSeek;
	public override bool CanWrite => m_stream.CanWrite;
	public override long Length => m_stream.Length;

	public override long Position { get => m_stream.Position; set => m_stream.Position = value; }

	public override void Flush() => m_stream.Flush();

	public override int Read(byte[] buffer, int offset, int count)
	{
		int result = m_stream.Read(buffer, offset, count);
		m_hash.TransformBlock(buffer, offset, count, buffer, offset);
		return result;
	}

	public override long Seek(long offset, SeekOrigin origin) => m_stream.Seek(offset, origin);

	public override void SetLength(long value) => m_stream.SetLength(value);

	public override void Write(byte[] buffer, int offset, int count)
	{
		m_stream.Write(buffer, offset, count);
		m_hash.TransformBlock(buffer, offset, count, buffer, offset);
	}

	public byte[] Hash
	{
		get
		{
			m_hash.TransformFinalBlock(s_empty, 0, 0);
			return m_hash.Hash;
		}
	}

	public string HashString
	{
		get
		{
			m_hash.TransformFinalBlock(s_empty, 0, 0);
			return Convert.ToBase64String(m_hash.Hash);
		}
	}

	private static readonly byte[] s_empty = Array.Empty<byte>();
}
