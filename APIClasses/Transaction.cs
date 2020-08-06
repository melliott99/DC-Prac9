using System;
using System.Collections.Generic;
using System.Text;

namespace APIClasses
{
	public class Transaction
	{
		public uint senderID{get; set;}
		public uint receiverID{get; set;}
		public float amount{get; set;}
		public string timestamp{get; set;}
	}
}
