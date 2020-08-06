using APIClasses;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionGenerator
{
	public static class Messenger
	{
		public static List<Block> blockChain =  new List<Block>();

		//Used a queue so that it is easier to remove off of 
		public static ConcurrentQueue<string[]> scriptQueue = new ConcurrentQueue<string[]>();

	}
}
