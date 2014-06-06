using System;
using System.IO;
using MonoTorrent.BEncoding;
using MonoTorrent.Common;

namespace MonoTorrent.Client
{
	public class FastResume
	{
		private static readonly BEncodedString VersionKey = "version";
		private static readonly BEncodedString InfoHashKey = "infohash";
		private static readonly BEncodedString BitfieldKey = "bitfield";
		private static readonly BEncodedString BitfieldLengthKey = "bitfield_length";

		private readonly BitField bitfield;
		private readonly InfoHash infoHash;

		public FastResume()
		{
		}

		public FastResume(InfoHash infoHash, BitField bitfield)
		{
			if (infoHash == null)
				throw new ArgumentNullException("infoHash");
			if (bitfield == null)
				throw new ArgumentNullException("bitfield");

			this.infoHash = infoHash;
			this.bitfield = bitfield;
		}

		public FastResume(BEncodedDictionary dict)
		{
			CheckContent(dict, VersionKey, 1);
			CheckContent(dict, InfoHashKey);
			CheckContent(dict, BitfieldKey);
			CheckContent(dict, BitfieldLengthKey);

			infoHash = new InfoHash(((BEncodedString) dict[InfoHashKey]).TextBytes);
			bitfield = new BitField((int) ((BEncodedNumber) dict[BitfieldLengthKey]).Number);
			byte[] data = ((BEncodedString) dict[BitfieldKey]).TextBytes;
			bitfield.FromArray(data, 0, data.Length);
		}

		public BitField Bitfield
		{
			get { return bitfield; }
		}

		public InfoHash Infohash
		{
			get { return infoHash; }
		}

		private void CheckContent(BEncodedDictionary dict, BEncodedString key, BEncodedNumber value)
		{
			CheckContent(dict, key);
			if (!dict[key].Equals(value))
				throw new TorrentException(string.Format("Invalid FastResume data. The value of '{0}' was '{1}' instead of '{2}'",
					key, dict[key], value));
		}

		private void CheckContent(BEncodedDictionary dict, BEncodedString key)
		{
			if (!dict.ContainsKey(key))
				throw new TorrentException(string.Format("Invalid FastResume data. Key '{0}' was not present", key));
		}

		public BEncodedDictionary Encode()
		{
			var dict = new BEncodedDictionary();
			dict.Add(VersionKey, (BEncodedNumber) 1);
			dict.Add(InfoHashKey, new BEncodedString(infoHash.Hash));
			dict.Add(BitfieldKey, new BEncodedString(bitfield.ToByteArray()));
			dict.Add(BitfieldLengthKey, (BEncodedNumber) bitfield.Length);
			return dict;
		}

		public void Encode(Stream s)
		{
			byte[] data = Encode().Encode();
			s.Write(data, 0, data.Length);
		}
	}
}