using APIClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace TransactionGenerator
{

	[ServiceContract]
	public interface PeerServerInterface
	{

		[OperationContract]
		List<Block> GetBlockChain();

		[OperationContract]
		Block GetCurrentBlock();

		[OperationContract]
		void GetNewScript(string[] scriptStr);

	}
}
