using APIClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TransactionGenerator
{
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = true)]
	class PeerServer : PeerServerInterface
	{
		/*Returns block chain*/
		public List<Block> GetBlockChain()
		{
			return Messenger.blockChain;			
		}

		/*Gets the last block in the block chain*/
		public Block GetCurrentBlock()
		{
			return Messenger.blockChain.ElementAt(Messenger.blockChain.Count-1);
		}

		/*Adds a script to the queue*/
		public void GetNewScript(string[] scriptStr)
		{
			Messenger.scriptQueue.Enqueue(scriptStr);
		}

	}
}
