using System;
using System.Collections.Generic;
using System.Text;

namespace APIClasses
{
	public class Job
	{
		public int ID{get; set;}
		public string code{get; set;}
		public string answer{get; set;}

		public Job(string inCode, string inAnswer, int inID)
		{
			ID = inID;
			code = inCode;
			answer = inAnswer;
		}
	}
}
