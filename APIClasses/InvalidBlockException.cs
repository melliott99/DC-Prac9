using System;
using System.Collections.Generic;
using System.Text;

namespace APIClasses
{
	public class InvalidBlockException: Exception
	{
		public InvalidBlockException()
		: base()
		{

		}

		public InvalidBlockException(string message)
		: base(message)
		{

		}


		public InvalidBlockException(string message, Exception innerException)
		: base(message, innerException)
		{
		}
	}
}
