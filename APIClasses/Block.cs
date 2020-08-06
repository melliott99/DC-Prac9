using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace APIClasses
{
	public class Block
	{
		public uint blockID{get; set;}
		
		public uint blockOffset{get; set;}

		public string prevHash{get; set;}

		public string hash{get; set;}

		public string jsonTransactions{get; set;}

		//Creating first new block
		public Block()
		{
			blockID = 0;
			blockOffset = 1;
			hash = "";//Upon creation there isn't a hash
		}


		public Block(string scripts, uint newBlockID, string inPrevHash)
		{
			blockID = newBlockID;
			jsonTransactions = scripts;
			blockOffset = 1;
			prevHash = inPrevHash;
		}
	}
}
