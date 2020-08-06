using APIClasses;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using APIClasses;
using Microsoft.SqlServer.Server;
using IronPython.Hosting;
using IronPython.Runtime;
using IronPython;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.Security.Cryptography;
using System.ComponentModel;
using System.ServiceModel;
using System.Diagnostics;
using System.ServiceModel;
using WebServer.Models;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.CSharp.RuntimeBinder;

namespace TransactionGenerator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 
	public partial class MainWindow : Window
	{
		private List<uint> usedAccounts;
		private static Client me;
		private static RestClient RestClient = new RestClient("https://localhost:44380/");

		private List<string[]> localScripts = new List<string[]>();
		private List<Job> myJobs = new List<Job>();

		private static Log logger = Log.GetInstance();

		private List<string> ansList = new List<string>();

		private static uint blockID = 1;
		private static int jobId = 1;

		public MainWindow()
		{
			InitializeComponent();

			usedAccounts = new List<uint>();

			me = new Client();
			me.ipaddress = "localhost";//Using this ip as it works everywhere
			me.port = GeneratePort();
			me.jobcount = 0;
			PortBox.Text = "Port Number: " + me.port;//Displaying the users port on the client 


			RegisterClient();
			MinerThread();

			ServerThread();
		}


		//Registering myself
		public void RegisterClient()
		{

			RestRequest request = new RestRequest("api/client/registerclient/");
			request.AddJsonBody(me);
			IRestResponse resp = RestClient.Post(request);
			if (resp.IsSuccessful)
			{
				//good news
			}
			else
			{
				logger.LogFunc("ERROR: " + resp.Content);
				MessageBox.Show("ERROR\nCould not register you as a client for the peer to peer");
			}

		}

		/*Encoding and decoding of the scripts*/
		public string EncodeString(string str)
		{
			UTF8Encoding utf8 = new UTF8Encoding();
			byte[] textBytes = utf8.GetBytes(str);
			return Convert.ToBase64String(textBytes);
		}

		/*Encoding and decoding of the string*/
		public string DecodeString(string str)
		{
			UTF8Encoding utf8 = new UTF8Encoding();
			byte[] encodedBytes = Convert.FromBase64String(str);
			return utf8.GetString(encodedBytes);
		}


		/*Some of the most bootleg code i've ever written please excuse this*/
		public void DownloadJobTest()
		{

			List<string> ansList = new List<string>();

			if (Messenger.blockChain.Count > 1)//Check the size of the blockchain
			{
				foreach (APIClasses.Block b in Messenger.blockChain)
				{
					List<string[]> strArr = JsonConvert.DeserializeObject<List<string[]>>(b.jsonTransactions);
					if (strArr != null)
					{
						int count = 0;
						foreach (string[] s in strArr)
						{
							foreach (Job j in myJobs)
							{
								if (s[0] == j.code)
								{
									j.answer = DecodeString(s[1].ToString());//Decoding the string into english
									ansList.Add(count+1+ ": Answer is " + j.answer);//Outputting the answer on the peers client
								}
							}
							count++;	
						}
						
					}
				}
				Dispatcher.Invoke(() =>
				{
					JobBoard.ItemsSource = ansList;//Updating the GUI threads to contain the updated ans
				}); 
			}
		}

		/*Called when the users enters and submits code*/
		public void SubmitCode_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				ChannelFactory<PeerServerInterface> channelFactory;
				NetTcpBinding tcp = new NetTcpBinding();
				tcp.MaxReceivedMessageSize = 2147483647;
				string clientURL = String.Format("net.tcp://{0}:{1}/DataService", me.ipaddress, me.port);
				channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
				PeerServerInterface channel = channelFactory.CreateChannel();
				//Channel to the peer server has been created

				string scriptStr = CodeBox.Text;
				string encodedStr = EncodeString(scriptStr);
				string[] codeStr = new string[2] { encodedStr, "" };//Converting the ans to a string if it isn't already
				Job j = new Job(codeStr[0], codeStr[1], jobId);
				myJobs.Add(j);
				jobId++;//incremented for the next job this client creates

				RestRequest request = new RestRequest("api/client/getclientlist/");
				IRestResponse resp = RestClient.Get(request);

				if (resp.IsSuccessful)//Checking if the client list was available to get
				{
					List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(resp.Content);

					foreach (Client c in clients)//Give this to every client including myself
					{
						tcp = new NetTcpBinding();
						tcp.MaxReceivedMessageSize = 2147483647;
						clientURL = String.Format("net.tcp://{0}:{1}/DataService", c.ipaddress, c.port);
						channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
						channel = channelFactory.CreateChannel();
						channel.GetNewScript(codeStr);//Adding it to every clients script queue
					}
					UpdateJobList();
				}
				else
				{
					logger.LogFunc("ERROR: Could not retrieve the list of clients");
				}
			}
			catch (FormatException ex)
			{
				logger.LogFunc("ERROR: The user input was incorrect");
				MessageBox.Show("Please input python script correctly");
			}

		}


		public void UpdateJobList()
		{
			List<string> jobs = new List<string>();
			foreach (Job j in myJobs)
			{
				jobs.Add(j.ID + ": Pending");//The answer isn't in yet so we are pending on it
			}
			jobs.Sort();
			ansList = ansList.Except(jobs).ToList();
			//Removing any jobs that are in both lists
			ansList.AddRange(jobs);
			//Adding those jobs to the anslist so there aren't duplicates
			JobBoard.ItemsSource = ansList;
		}


		/*Broadcasts the script to all other peers*/
		public static void BroadcastTransaction(string[] script)
		{
			RestRequest request = new RestRequest("api/client/getclientlist/");
			IRestResponse resp = RestClient.Get(request);

			if (resp.IsSuccessful)
			{
				List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(resp.Content);

				foreach (Client c in clients)
				{
					if (c.port != me.port)//Don't broadcast it to me i already have it
					{
						ChannelFactory<PeerServerInterface> channelFactory;
						NetTcpBinding tcp = new NetTcpBinding();
						tcp.MaxReceivedMessageSize = 2147483647;
						string clientURL = String.Format("net.tcp://{0}:{1}/DataService", c.ipaddress, c.port);
						channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
						PeerServerInterface channel = channelFactory.CreateChannel();
						channel.GetNewScript(script);
					}
				}
				logger.LogFunc("SUCCESS:Broadcasted script to all clients");
			}
			else
			{
				logger.LogFunc("ERROR:Could not broadcast script to all clients");
			}

		}



		/*-------------------------------------------Server Thread----------------------------------------------------------------*/
		//Creates a connection that other peers can connect to
		public async void ServerThread()
		{
			await Task.Run(() =>
			{

				Debug.WriteLine("Connecting to the server");

				//This is the actual host service system
				ServiceHost host;

				//This represents a tcp/ip binding in the windows network stack
				NetTcpBinding tcp = new NetTcpBinding();

				//Bind server to the implementation of peerServer
				host = new ServiceHost(typeof(PeerServer));

				string setupURL = String.Format("net.tcp://{0}:{1}/DataService", me.ipaddress, me.port);

				Debug.WriteLine(setupURL);
				host.AddServiceEndpoint(typeof(PeerServerInterface), tcp, setupURL);

				//We are on
				host.Open();

				Debug.WriteLine("Connection established for: " + me.port);
				InitialiseChain();
				while (true)
				{
					//stay open
				}
			});

		}


		/*-------------------------------BlockChain Thread--------------------------------------*/

		public async void MinerThread()
		{
			await Task.Run(() =>
			{
				try
				{
					while (true)
					{
						string[] script;
						if (Messenger.scriptQueue.TryDequeue(out script))
						{
							localScripts.Add(script);
							if (localScripts.Count == 5)//When 5 scripts have been submitted thats enough for a block
							{
	

								List<string[]> scriptList = localScripts.OrderBy(orderby => script[0]).ToList();//ordering the scripts

								RunScripts();//executeing the scripts

								string jsonScripts = JsonConvert.SerializeObject(scriptList);
								
								
								string prevHash = GetPrevHash();
								APIClasses.Block newBlock = new APIClasses.Block(jsonScripts, blockID, prevHash);
								blockID++;
								ValidateBlock(newBlock);//validate the block create the has and validate that
								Messenger.blockChain.Add(newBlock);//add to this clients block chain
								GetMostPopular(newBlock);//Check my block chain against everyother clients block chain for the most popular
								DownloadJobTest();
								myJobs.Clear();
							}
						}

					}
				}
				catch (InvalidBlockException e)
				{
					logger.LogFunc("ERROR: Invalid Block");
					MessageBox.Show("Invalid Transaction");
				}
			});
		}

		public void RunScripts()
		{
			try
			{
				foreach (string[] str in localScripts)
				{
					string code = str[0];
					byte[] encodedBytes = Convert.FromBase64String(code);
					UTF8Encoding utf8 = new UTF8Encoding();
					Microsoft.Scripting.Hosting.ScriptEngine pythonEngine = IronPython.Hosting.Python.CreateEngine();
					Microsoft.Scripting.Hosting.ScriptSource pythonScript = pythonEngine.CreateScriptSourceFromString(utf8.GetString(encodedBytes));
					var answer = pythonScript.Execute();//change later to allow for more types

					byte[] answerBytes = utf8.GetBytes(answer.ToString());
					str[1] = Convert.ToBase64String(answerBytes);
				}
			}
			catch (RuntimeBinderException e)
			{
				MessageBox.Show("There was an error with the scripts you wanted me to run");
			}
			catch (UnboundNameException e)
			{
				MessageBox.Show("Please double check that input was put in correctly");
			}
		}

		public string GetPrevHash()
		{
			try
			{
				APIClasses.Block b = Messenger.blockChain.ElementAt(Messenger.blockChain.Count - 1);
				return b.hash;
			}
			catch (ArgumentOutOfRangeException ex)
			{

				if (Messenger.blockChain.Count > 0)
				{
					throw new InvalidBlockException("Argument out of range in getting previous hash", ex);
				}
				else
				{
					return "";
				}
			}

		}
		public static void GetMostPopular(APIClasses.Block block)
		{
			bool isPopular = false;
			RestRequest request = new RestRequest("api/client/getclientlist/");
			IRestResponse resp = RestClient.Get(request);

			List<Client> clients = JsonConvert.DeserializeObject<List<Client>>(resp.Content);

			Dictionary<string, List<Client>> clientMap = new Dictionary<string, List<Client>>();//<hash, <list<ports who have that hash>>

			Client mostPopular = new Client();//This is the client's whose block chain we will take
			int popularity = 0;
			string popularHash = block.hash;

			foreach (Client c in clients)
			{
				ChannelFactory<PeerServerInterface> channelFactory;
				NetTcpBinding tcp = new NetTcpBinding();
				tcp.MaxReceivedMessageSize = 2147483647;
				string clientURL = String.Format("net.tcp://{0}:{1}/DataService", c.ipaddress, c.port);
				channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
				PeerServerInterface channel = channelFactory.CreateChannel();
				APIClasses.Block b = channel.GetCurrentBlock();

				//Hash is the key and then if someone else has that key increase the value at that key
				if (clientMap.ContainsKey(b.hash))
				{
					clientMap[b.hash].Add(c);
				}
				else
				{
					List<Client> ports = new List<Client>();
					ports.Add(c);
					clientMap.Add(b.hash, ports);
				}
			}

			foreach (KeyValuePair<string, List<Client>> entry in clientMap)
			{
				if (entry.Value.Count > popularity)
				{
					mostPopular = entry.Value.ElementAt(0);
					popularHash = entry.Key;
					popularity = entry.Value.Count;
				}
			}
			if (!popularHash.Equals(block.hash))//If my hash is not the most popular
			{
				Thread.Sleep(1000);//Sleep long enough so you can get the updated version of the others
				ChannelFactory<PeerServerInterface> channelFactory;
				NetTcpBinding tcp = new NetTcpBinding();
				tcp.MaxReceivedMessageSize = 2147483647;
				string clientURL = String.Format("net.tcp://{0}:{1}/DataService", mostPopular.ipaddress, mostPopular.port);
				channelFactory = new ChannelFactory<PeerServerInterface>(tcp, clientURL);
				PeerServerInterface channel = channelFactory.CreateChannel();
				Messenger.blockChain = channel.GetBlockChain();
			}
			else
			{
				//you've got the best chain
			}
		}

		
		public static void AddBlock(APIClasses.Block newBlock)
		{
			Messenger.blockChain.Add(newBlock);
		}


		/*Called from the server thread, adds the first block to the blockchain*/
		public void InitialiseChain()
		{

			APIClasses.Block b = new APIClasses.Block("", blockID, "");
			blockID++;
			GenerateHash(b);
			Messenger.blockChain.Add(b);

			GetMostPopular(b);
			//makes sure there aren't clients who already have a block chain going

		}

		public static void ValidateBlock(APIClasses.Block newBlock)
		{
			try
			{
				CheckBlockID(newBlock.blockID);
				CheckBlockOffset(newBlock.blockOffset);
				CheckPrevHash(newBlock.prevHash);
				GenerateHash(newBlock);
				CheckHash(newBlock.hash);
			}
			catch (InvalidBlockException e)
			{
				logger.LogFunc("ERROR: Invalid Block " + e.Message);

			}
		}

		public static void CheckBlockID(uint blockID)
		{
			try
			{
				if (blockID > Messenger.blockChain.ElementAt(Messenger.blockChain.Count - 1).blockID)
				{
					//we cool
				}
				else
				{
					logger.LogFunc("ERROR: incorrect block id" + blockID);
					throw new InvalidBlockException();
				}
			}
			catch (ArgumentOutOfRangeException e)
			{
				logger.LogFunc("ERROR: blockID is out of range");
			}
		}

	

		public static void CheckAmount(float amount, uint senderID)
		{
			if (amount <= 0.0)
			{
				throw new InvalidBlockException("Invalid amount");
			}
		}

		public static void CheckBlockOffset(uint offset)
		{
			if (offset < 0)
			{
				throw new InvalidBlockException("Invalid Block Offset");
			}
		}

		public static List<APIClasses.Block> GetBlockChain()
		{
			return Messenger.blockChain;
		}

		public static void CheckPrevHash(string prevHash)
		{

			if (Messenger.blockChain.Count > 1)
			{
				if (prevHash.Equals(Messenger.blockChain.ElementAt(Messenger.blockChain.Count - 1).hash))
				{
					//we chilling
				}
				else
				{
					throw new InvalidBlockException("Invalid previous hash");
				}
			}

		}

		public static void CheckHash(string hash)
		{
			if (Messenger.blockChain.Count > 1)
			{
				string checkHash = hash.Substring(0, 5);
				if (checkHash.Equals("12345"))
				{
					//everything is good
				}
				else
				{
					throw new InvalidBlockException("This Hash is Invalid");
				}
			}
		}


		/*Generating the hash*/
		public static APIClasses.Block GenerateHash(APIClasses.Block newBlock)
		{
			string final = "";
			string newHash = "";

			bool hashed = true;

			do
			{
				//Hash string
				final = newBlock.blockID.ToString() + newBlock.jsonTransactions.ToString() + newBlock.blockOffset.ToString() + newBlock.prevHash.ToString();


				//Generating a valid Hash
				SHA256 sha256 = SHA256.Create();

				byte[] hashBytes = System.Text.Encoding.UTF8.GetBytes(final);//encoding
				byte[] finalHash = sha256.ComputeHash(hashBytes);//hashing


				newHash = ConvertHashToString(finalHash);
				string checkHash = newHash.Substring(0, 5);

				if (checkHash.Equals("12345"))//Does it start with 12345
				{
					hashed = false;
				}
				else
				{
					newBlock.blockOffset += 2;
				}

			} while (hashed);


			newBlock.hash = newHash;
			return newBlock;

		}



		/*Sourced From																				*/
		/*https://stackoverflow.com/questions/11112216/how-to-convert-a-byte-array-into-an-int-array*/
		public static string ConvertHashToString(byte[] byteArray)
		{
			String hashStr = "";
			int[] intArr = byteArray.Select(x => (int)x).ToArray();
			foreach (int num in intArr)
			{
				hashStr += num.ToString();
			}

			return hashStr;

		}





		/*-------------------------- End BlockChain stuff--------------------------------------*/



		public String GeneratePort()
		{
			Random rnd = new Random();
			string portnum = rnd.Next(8000, 8999).ToString();
			return portnum;
		}
	}
}
